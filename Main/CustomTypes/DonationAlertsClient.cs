using System;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HerokuApp.Main.Extensions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace HerokuApp.Main.CustomTypes
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DonationMessageType { None, Text, Audio };

    public class DonationObject
    {
        public int Id;
        public string Username;
        [JsonProperty("message_type")] public DonationMessageType MessageType;
        public string Message;
        public double Amount;
        public string Currency;
        [JsonConverter(typeof(BoolConverter))]
        [JsonProperty("is_shown")] public bool IsShown;
        [JsonConverter(typeof(CustomStringToDateTimeConverter))]
        [JsonProperty("created_at")] public DateTime CreatedAt;
        [JsonConverter(typeof(CustomStringToDateTimeConverter))]
        [JsonProperty("shown_at")] public DateTime? ShownAt;
    }

    internal class CustomStringToDateTimeConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return false;
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, Newtonsoft.Json.JsonSerializer serializer)
        {
            if (reader.Value == null) return null;
            return DateTime.Parse(reader.Value.ToString());
        }

        public override void WriteJson(JsonWriter writer, object value, Newtonsoft.Json.JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }

    public class DonationAlertsClient
    {
        private const string donAlertaWebSocketURI = "wss://centrifugo.donationalerts.com/connection/websocket";
        private readonly string _accessToken;
        private readonly string _channelName;

        public event EventHandler<DonationObject> OnDonationReceived;

        public DonationAlertsClient(string accessToken, string channelName)
        {
            _accessToken = accessToken;
            _channelName = channelName;
        }

        public async Task StartAsync()
        {
            var url = new Uri(donAlertaWebSocketURI);

            var socket = new ClientWebSocket();
            await socket.ConnectAsync(url, CancellationToken.None);
            AppDomain.CurrentDomain.ProcessExit += async (o, e) => await ClosingHandler(socket);
            var userInfo = GetUserInfo(_accessToken);
            var connectionToken = userInfo.Value<string>("socket_connection_token");
            string jsonString1 = StepOne(connectionToken);
            await socket.SendAsync(jsonString1);
            var response1 = await socket.ReceiveAsync();

            var channelId = userInfo.Value<string>("id");
            var jsonString2 = StepTwo(response1, channelId, _accessToken);
            await socket.SendAsync(jsonString2);

            Console.WriteLine("Successfully connected to DonationAlerts websocket");

            var channelName = _channelName;
            while (true)
            {
                var response = await socket.ReceiveAsync();
                if (response == null)
                {
                    Console.WriteLine("Donation Alerts socket closed");
                }
                
                var responseJObject = JObject.Parse(response);
                Console.WriteLine("A new message in the DonAlerts socket!");
                //Console.WriteLine(responseJObject);

                var donateJObject = responseJObject.SelectToken("$.result.data.data");
                if (donateJObject == null) continue;

                Console.WriteLine("A new donation!");
                Console.WriteLine(donateJObject);

                var donObject = donateJObject.ToObject<DonationObject>();
                OnDonationReceived?.Invoke(this, donObject);
            }
        }

        private string StepOne(string connectionToken)
        {
            var firstJSON = new JObject {
                { "params", new JObject { { "token", connectionToken } } },
                { "id", 1 }
            };
            var jsonString = JsonConvert.SerializeObject(firstJSON);
            return jsonString;
        }

        private string StepTwo(string response, string channelId, string accessToken)
        {
            var formattedChannelId = $"$alerts:donation_{channelId}";
            var answer = JObject.Parse(response);
            var clientId = answer.SelectToken("$.result.client");

            var client = new RestClient("https://www.donationalerts.com/api/v1/centrifuge/subscribe");
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Content-Type", "application/json");
            var jsonBody = new JObject {
                { "channels", new JArray { formattedChannelId } },
                { "client", clientId }
            };
            request.AddJsonBody(JsonConvert.SerializeObject(jsonBody));
            var content = client.Execute(request, Method.POST).Content;
            var requestAnswer = JObject.Parse(content);
            var connectionToken = requestAnswer.SelectToken("$.channels[0].token");

            var secondJSON = new JObject {
                { "params", new JObject {
                    { "channel", formattedChannelId },
                    { "token", connectionToken }
                }},
                { "method", 1 },
                { "id", 2 }
            };
            var jsonString = JsonConvert.SerializeObject(secondJSON);
            return jsonString;
        }

        private JObject GetUserInfo(string accessToken)
        {
            var client = new RestClient("https://www.donationalerts.com/api/v1/user/oauth");
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {accessToken}");

            var content = client.Execute(request, Method.GET).Content;
            var requestAnswer = JObject.Parse(content);
            return requestAnswer.Value<JObject>("data");
        }

        private static async Task ClosingHandler(ClientWebSocket socket)
        {
            Console.WriteLine("DonationAlerts socket is closing...");
            await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Program reloading", CancellationToken.None);
            Console.WriteLine($"DonationAlerts socket status: {socket.State}");
        }
    }
}
