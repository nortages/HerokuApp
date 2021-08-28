using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using Websocket.Client;

namespace TwitchBot.Main.DonationAlerts
{
    public class DonationAlertsClient
    {
        private const string DonAlertsWebSocketUri = "wss://centrifugo.donationalerts.com/connection/websocket";
        
        private readonly string _accessToken;
        private readonly string _connectionToken;
        private readonly string _userId;
        private WebsocketClient _webSocket;
        private static ILogger _logger;
        private bool _isConnectionPending;
        private int _connectedChannelsNum;
        private readonly JsonSerializer _serializer;
        private readonly List<string> _channelsToConnect = new();

        public event EventHandler OnConnected;
        public event EventHandler<OnDonationAlertArgs> OnDonationAlert;
        public event EventHandler<DonationGoalUpdate> OnDonationGoalUpdateReceived;
        
        public DonationAlertsClient(string accessToken)
        {
            _accessToken = accessToken;
            var userInfo = GetUserInfo(_accessToken);
            _connectionToken = userInfo.Value<string>("socket_connection_token");
            _userId = userInfo.Value<string>("id");

            _serializer = new JsonSerializer {DateFormatString = "yyyy-MM-ddTHH:mm:ss"};
        }

        public static void SetLogger(ILogger logger)
        {
            _logger = logger;
        }

        private void Log(LogLevel level, string message)
        {
            _logger.Log(level,"[{Name}] {Message}", GetType().Name, message);
        }

        public void Connect()
        {
            var url = new Uri(DonAlertsWebSocketUri);
            _webSocket = new WebsocketClient(url)
            {
                ReconnectTimeout = null,
                ErrorReconnectTimeout = TimeSpan.FromSeconds(30)
            };

            _webSocket.MessageReceived.Subscribe(msg =>
            {
                WebSocketOnMessage(msg.Text);
            });

            _webSocket.ReconnectionHappened.Subscribe(info =>
            {
                _logger.Log(LogLevel.Information, $"Reconnection happened, type: {info.Type}");
                WebSocketOnOpen(null, null);
            });
            
            _webSocket.DisconnectionHappened.Subscribe(info =>
            {
                _logger.Log(LogLevel.Information, $"Disconnection happened, type: {info.Type}");
            });
            
            OnConnected += (_, _) => Log(LogLevel.Information, "Successfully connected to websocket");
            
            _webSocket.Start();
        }

        private void WebSocketOnOpen(object sender, EventArgs e)
        {
            _isConnectionPending = true;
            var dataToSend = ConnectionStepOne();
            _webSocket.Send(dataToSend);
        }

        private void WebSocketOnMessage(string data)
        {
            var parsedData = JObject.Parse(data);
            
            if (_isConnectionPending)
            {
                _logger.Log(LogLevel.Information, $"Connection message received: {parsedData}");
                ProceedConnection(parsedData);
                return;
            }

            var channelToken = parsedData.SelectToken("$.result.channel");
            if (channelToken == null) return;

            var channel = channelToken.Value<string>();
            var eventDataToken = parsedData.SelectToken("$.result.data.data");
            if (eventDataToken == null) return;


            if (channel == $"$alerts:donation_{_userId}")
            {
                var donObject = eventDataToken.ToObject<OnDonationAlertArgs>(_serializer);
                OnDonationAlert?.Invoke(this, donObject);
            }
            else if (channel == $"$goals:goal_{_userId}")
            {
                var donObject = eventDataToken.ToObject<DonationGoalUpdate>(_serializer);
                OnDonationGoalUpdateReceived?.Invoke(this, donObject);
            }
        }

        private void ProceedConnection(JToken data)
        {
            var clientId = data.SelectToken("$.result.client");
            if (clientId != null)
            {
                ConnectionStepTwo(clientId.Value<string>());
                return;
            }
            
            var client = data.SelectToken("$.result.data.info.client");
            if (client == null) return;
            
            _connectedChannelsNum++;
            if (_connectedChannelsNum != _channelsToConnect.Count) return;

            _connectedChannelsNum = 0;
            _isConnectionPending = false;
            OnConnected?.Invoke(this, new EventArgs());
        }

        public void ListenToDonationAlerts()
        {
            _channelsToConnect.Add($"$alerts:donation_{_userId}");
        }

        public void ListenToDonationGoalsUpdates()
        {
            _channelsToConnect.Add($"$goals:goal_{_userId}");
        }

        private string ConnectionStepOne()
        {
            var firstJson = new JObject {
                { "params", new JObject { { "token", _connectionToken } } },
                { "id", 1 }
            };
            var jsonString = JsonConvert.SerializeObject(firstJson);
            return jsonString;
        }

        private void ConnectionStepTwo(string clientId)
        {
            var client = new RestClient("https://www.donationalerts.com/api/v1/centrifuge/subscribe");
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {_accessToken}");
            request.AddHeader("Content-Type", "application/json");
            var jsonBody = new JObject {
                { "channels", JArray.FromObject(_channelsToConnect) },
                { "client", clientId }
            };
            request.AddJsonBody(JsonConvert.SerializeObject(jsonBody));
            var content = client.Execute(request, Method.POST).Content;
            var requestAnswer = JObject.Parse(content);
            
            var channelsInfo = requestAnswer.Value<JArray>("channels");
            if (channelsInfo == null)
                throw new NullReferenceException($"[{nameof(DonationAlertsClient)}] [{MethodBase.GetCurrentMethod()?.Name}] Channels array is null.");
            
            foreach (var channelInfo in channelsInfo)
            {
                var channelName = channelInfo.Value<string>("channel");
                var connectionToken = channelInfo.Value<string>("token");
                var secondJson = new JObject {
                    { "params", new JObject {
                        { "channel", channelName },
                        { "token", connectionToken }
                    }},
                    { "method", 1 },
                    { "id", 2 }
                };
                var dataToSend = JsonConvert.SerializeObject(secondJson); 
                _webSocket.Send(dataToSend);
            }
        }

        private static JObject GetUserInfo(string accessToken)
        {
            var client = new RestClient("https://www.donationalerts.com/api/v1/user/oauth");
            var request = new RestRequest();
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            var content = client.Execute(request, Method.GET).Content;
            var requestAnswer = JObject.Parse(content);
            return requestAnswer.Value<JObject>("data");
        }

        public void Close()
        {
            // _webSocket?.Close(CloseStatusCode.Normal, "Program closing.");
            _webSocket.Dispose();
        }
    }
}
