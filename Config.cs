using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using TwitchLib.Client.Models;

namespace HerokuApp
{
    static class Config
    {
        static readonly RestClient client;
        static readonly RestClient commandsClient;
        static readonly RestClient th3gloablistClient;
        static readonly RestClient hearthstoneClient;

        static void GetJsonStorageInfo(out string url, out string configId, out string apiKey, out string securityKey)
        {
            string pathToConfig = "configs.json";
            string JSONbinURLKey = "JSONbinURL";
            string securityKeyKey = "securityKey";
            string apiKeyKey = "JSONstorageAPIkey";
            string configIdKey = "configId";

            if (File.Exists(pathToConfig))
            {
                using StreamReader r = new StreamReader(pathToConfig);
                string json = r.ReadToEnd();
                var items = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                configId = items[configIdKey];
                apiKey = items[apiKeyKey];
                url = items[JSONbinURLKey];
                securityKey = items[securityKeyKey];
            }
            else
            {
                configId = Environment.GetEnvironmentVariable(configIdKey);
                apiKey = Environment.GetEnvironmentVariable(apiKeyKey);
                url = Environment.GetEnvironmentVariable(JSONbinURLKey);
                securityKey = Environment.GetEnvironmentVariable(securityKeyKey);
            }
        }

        static string GetRequest(RestClient restClient)
        {
            var request = new RestRequest();
            var response = restClient.Get(request);
            return response.Content;
        }

        static void PatchRequest(string patchJsonString, RestClient restClient)
        {
            var request = new RestRequest();
            request.AddParameter("application/merge-patch+json", patchJsonString, ParameterType.RequestBody);
            restClient.Patch(request);
        }

        public static void SaveChatMessages()
        {
            Console.WriteLine("Start saving messages...");
            var patchDict = new Dictionary<string, Queue<ChatMessage>>
            {
                { "chatMessages", TwitchChatBot.chatMessages }
            };
            var patchJsonString = JsonConvert.SerializeObject(patchDict);
            Console.WriteLine("Send them to storage...");
            PatchRequest(patchJsonString, commandsClient);
            Console.WriteLine("Saved!");
        }

        public static void LoadChatMessages()
        {
            var jsonString = GetRequest(commandsClient);
            var data = JsonConvert.DeserializeObject<JObject>(jsonString);
            var messages = data.Value<JArray>("chatMessages");
            Console.WriteLine("Saved messages: " + messages.Count);
            if (messages.Count != 0)
            {
                TwitchChatBot.chatMessages = messages.ToObject<Queue<ChatMessage>>();
            }
        }

        public static void LoadTh3globalistData()
        {
            var jsonString = GetRequest(th3gloablistClient);
            Th3globalistData = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, double>>>(jsonString);
        }

        public static void SaveManuls(int manulsNum)
        {
            var patchDict = new Dictionary<string, int>
            {
                { "manuls", manulsNum }
            };
            var patchJsonString = JsonConvert.SerializeObject(patchDict);
            PatchRequest(patchJsonString, commandsClient);
        }

        public static int GetManuls()
        {   
            var jsonString = GetRequest(commandsClient);
            var data = JsonConvert.DeserializeObject<JObject>(jsonString);
            return data.Value<int>("manuls");
        }

        public static JArray GetManulsEasterEggs()
        {
            var jsonString = GetRequest(commandsClient);
            var data = JsonConvert.DeserializeObject<JObject>(jsonString);
            return data.Value<JArray>("manulsEasterEggs");
        }

        public static JObject GetRadishCommandUsage()
        {
            var jsonString = GetRequest(commandsClient);
            var data = JsonConvert.DeserializeObject<JObject>(jsonString);
            return data.Value<JObject>("radishUsage");
        }

        public static void SaveRadishCommandUsage(KeyValuePair<string, int> chatterUsage)
        {
            var nestedDict = new JObject
            {
                { chatterUsage.Key, chatterUsage.Value }
            };
            var patchDict = new JObject
            {
                { "radishUsage", nestedDict }
            };
            var patchJsonString = JsonConvert.SerializeObject(patchDict);
            PatchRequest(patchJsonString, commandsClient);
        }

        public static JArray GetPirates()
        {
            var jsonString = GetRequest(hearthstoneClient);
            if (jsonString == "")
            {
                RenewBattleNetToken();
                jsonString = GetRequest(hearthstoneClient);
            }
            var data = JsonConvert.DeserializeObject<JObject>(jsonString);
            return data.Value<JArray>("cards");
        }

        public static string RenewToken(string url, string battleNetClientId, string battleNetClientSecret)
        {
            var client = new RestClient(url);
            client.Timeout = -1;
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddParameter("client_id", battleNetClientId);
            request.AddParameter("client_secret", battleNetClientSecret);
            request.AddParameter("grant_type", "client_credentials");
            IRestResponse response = client.Execute(request);
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.Content);
            return responseDict["access_token"];
        }

        public static void SaveManulsEasterEggs(JArray easterEggs)
        {
            var patchDict = new Dictionary<string, JArray>
            {
                { "manulsEasterEggs", easterEggs }
            };
            var patchJsonString = JsonConvert.SerializeObject(patchDict);
            PatchRequest(patchJsonString, commandsClient);
        }

        static Config()
        {
            GetJsonStorageInfo(out string apiUrl, out string configId, out string apiKey, out string securityKey);
            client = new RestClient($"{apiUrl}{configId}");
            client.AddDefaultHeader("Api-key", apiKey);
            client.AddDefaultHeader("Security-key", securityKey);

            var chatMessagesId = "06054dfa9012";
            commandsClient = new RestClient($"{apiUrl}{chatMessagesId}");
            commandsClient.AddDefaultHeader("Api-key", apiKey);
            commandsClient.AddDefaultHeader("Security-key", securityKey);

            var th3globalistDataId = "4cba63f40ca3";
            th3gloablistClient = new RestClient($"{apiUrl}{th3globalistDataId}");
            th3gloablistClient.AddDefaultHeader("Api-key", apiKey);
            th3gloablistClient.AddDefaultHeader("Security-key", "th3globalist");

            var jsonString = GetRequest(client);
            var config = JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(jsonString);
            var expirationDates = JsonConvert.DeserializeObject<Dictionary<string, string>>(config["ExpirationDates"].ToString());

            ChannelName = config["ChannelName"];
            ChannelAccessToken = config["ChannelAccessToken"];
            ChannelRefreshToken = config["ChannelRefreshToken"];
            ChannelUserID = config["ChannelUserId"];

            var value = expirationDates["ChannelAccessToken"];
            var ChannelTokenExpirationDate = value == "" ? DateTime.Now : DateTime.Parse(value);
            if ((ChannelTokenExpirationDate - DateTime.Now).TotalDays < 2)
            {
                TwitchHelpers.RenewTwitchToken(ChannelRefreshToken, out string newAccessToken);
                ChannelAccessToken = newAccessToken;
                var nestedDict = new JObject
                {
                    { "ChannelAccessToken", DateTime.Now.AddDays(60) }
                };
                var newConfig = new JObject
                {
                    { "ChannelAccessToken", newAccessToken },
                    { "ExpirationDates", nestedDict }
                };
                var patchJsonString = JsonConvert.SerializeObject(newConfig);
                PatchRequest(patchJsonString, client);
            }

            value = expirationDates["StreamEventsWebhook"];
            var StreamEventsWebhookExpirationDate = value == "" ? DateTime.Now : DateTime.Parse(value);
            if ((StreamEventsWebhookExpirationDate - DateTime.Now).TotalDays < 2)
            {
                var callbackUrl = "https://asp-docker.herokuapp.com/twitch-webhooks/streams/";
                var duration = TimeSpan.FromSeconds(864000);
                TwitchHelpers.SubscribeToStreamEvents(callbackUrl, "466176261", duration);
                var nestedDict = new JObject
                {
                    { "StreamEventsWebhook", DateTime.Now.AddDays(duration.TotalDays) }
                };
                var newConfig = new JObject
                {
                    { "ExpirationDates", nestedDict }
                };
                var patchJsonString = JsonConvert.SerializeObject(newConfig);
                PatchRequest(patchJsonString, client);
            }

            BotUsername = config["BotUsername"];
            BotAccessToken = config["BotAccessToken"];
            BotTwitchPassword = config["BotTwitchPassword"];
            BotRefreshToken = config["BotRefreshToken"];

            value = expirationDates["BotAccessToken"];
            var BotTokenExpirationDate = value == "" ? DateTime.Now : DateTime.Parse(value);
            if ((BotTokenExpirationDate - DateTime.Now).TotalDays < 2)
            {
                TwitchHelpers.RenewTwitchToken(BotRefreshToken, out string newAccessToken);
                BotAccessToken = newAccessToken;
                var nestedDict = new JObject
                {
                    { "BotAccessToken", DateTime.Now.AddDays(60) }
                };
                var newConfig = new JObject
                {
                    { "BotAccessToken", newAccessToken },
                    { "ExpirationDates", nestedDict }
                };
                var patchJsonString = JsonConvert.SerializeObject(newConfig);
                PatchRequest(patchJsonString, client);
            }

            BattleNetClientId = config["BattleNetClientId"];
            BattleNetClientSecret = config["BattleNetClientSecret"];
            BattleNetAccessToken = config["BattleNetAccessToken"];
            value = expirationDates["BattleNetAccessToken"];
            var BNetTokenExpirationDate = value == "" ? DateTime.Now : DateTime.Parse(value);
            if ((BNetTokenExpirationDate - DateTime.Now).TotalDays < 2)
            {
                RenewBattleNetToken();
            }

            hearthstoneClient = new RestClient($"https://us.api.blizzard.com/hearthstone/cards?locale=ru_RU&gameMode=battlegrounds&minionType=pirate&access_token={BattleNetAccessToken}");

            GmailEmail = config["GmailEmail"];
            GmailPassword = config["GmailPassword"];

            BotClientId = config["BotClientId"];
            JsonBinSecret = config["JsonBinSecret"];
            GoogleCredentials = config["GoogleCredentials"].ToString();
        }

        static void RenewBattleNetToken()
        {
            var newAccessToken = RenewToken("https://us.battle.net/oauth/token", BattleNetClientId, BattleNetClientSecret);
            BattleNetAccessToken = newAccessToken;
            var nestedDict = new JObject
            {
                { "BattleNetAccessToken", DateTime.Now.AddDays(1) }
            };
            var newConfig = new JObject
            {
                { "BattleNetAccessToken", newAccessToken },
                { "ExpirationDates", nestedDict }
            };
            var patchJsonString = JsonConvert.SerializeObject(newConfig);
            PatchRequest(patchJsonString, client);
        }

        public static string ChannelName { get; private set; }
        public static string ChannelAccessToken { get; private set; }
        public static string ChannelRefreshToken { get; private set; }
        public static string ChannelUserID { get; private set; }

        public static string BotUsername { get; private set; }
        public static string BotAccessToken { get; private set; }
        public static string BotRefreshToken { get; private set; }
        public static string BotClientId { get; private set; }
        public static string BotTwitchPassword { get; private set; }

        public static string GmailEmail { get; private set; }
        public static string GmailPassword { get; private set; }

        public static string JsonBinSecret { get; private set; }
        public static string GoogleCredentials { get; private set; }
        public static string BattleNetClientId { get; private set; }
        public static string BattleNetClientSecret { get; private set; }
        public static string BattleNetAccessToken { get; private set; }
        public static Dictionary<string, Dictionary<string, double>> Th3globalistData { get; private set; }
    }
}
