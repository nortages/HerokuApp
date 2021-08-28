using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace TwitchBot.Main
{
    internal static class Config
    {
        public static readonly bool IsDeployed = Environment.GetEnvironmentVariable("DEPLOYED") != null;
        public const StringComparison StringComparison = System.StringComparison.OrdinalIgnoreCase;

        public static string OwnerUsername { get; private set; }
        public static string BotUsername { get; private set; }
        public static string BotUserId { get; private set; }
        public static string BotClientId { get; private set; }

        public static JObject CommandsData { get; private set; }
        public static JObject ConfigData { get; private set; }

        private static RestClient _jsonStorageClient;
        private static RestRequest _commandsInfoRequest;

        public static event EventHandler OnConfigSaves;
        
        static Config()
        {
            if (IsDeployed)
            {
                Program.OnProcessExit += (_, e) => SaveCommandsInfo();

                var timer = new System.Timers.Timer(TimeSpan.FromMinutes(15).TotalMilliseconds) {AutoReset = true};
                timer.Elapsed += (e, args) => SaveCommandsInfo();
                timer.Start(); 
            }
            
            // TODO: make separate method to update access tokens
            // var streamEventsWebhookExpirationDate = config["streamEventsWebhook"].Value<DateTime>();
            // if ((streamEventsWebhookExpirationDate - DateTime.Now).TotalDays < 2)
            // {
            //     var callbackUrl = "https://asp-docker.herokuapp.com/twitch-webhooks/streams/";
            //     var duration = TimeSpan.FromSeconds(864000);
            //     TwitchHelpers.SubscribeToStreamEvents(callbackUrl, "466176261", duration);
            //     var nestedDict = new JObject
            //     {
            //         { "StreamEventsWebhook", DateTime.Now.AddDays(duration.TotalDays) }
            //     };
            //     var newConfig = new JObject
            //     {
            //         { "ExpirationDates", nestedDict }
            //     };
            //     var patchJsonString = JsonConvert.SerializeObject(newConfig);
            //     PatchRequest(patchJsonString, client);
            // }
        }

        public static void Build(IConfiguration configuration)
        {
            var (apiKey, securityKey) = GetJsonStorageSecrets();
            _jsonStorageClient = GetJsonStorageClient(configuration["JsonStorage:BaseUrl"], apiKey, securityKey);
            (OwnerUsername, BotUsername, BotUserId) = GetBotInfo(configuration);
            ConfigData = GetJsonStorageBin(configuration["JsonStorage:Bins:Config"]);
            CommandsData = GetJsonStorageBin(configuration["JsonStorage:Bins:CommandsData"]);
            BotClientId = ConfigData.Value<string>("bot_client_id");
        }

        public static string GetTwitchAccessToken() => GetTwitchAccessToken(BotUsername);

        public static string GetTwitchAccessToken(string username)
        {
            return (string) ConfigData.SelectToken($"$.twitch_tokens.{username.ToLower()}.access_token");
        }
 
        public static void SaveCommandsInfo()
        {
            if (CommandsData == null) return;
            
            Console.WriteLine("Saving config...");
            OnConfigSaves?.Invoke(null, null!);
            _commandsInfoRequest.AddOrUpdateParameter(
                "application/json",
                CommandsData.ToString(Formatting.None),
                ParameterType.RequestBody);
            _jsonStorageClient.Execute(_commandsInfoRequest, Method.PUT);
        }

        private static (string apiKey, string securityKey) GetJsonStorageSecrets()
        {
            string apiKey, securityKey;
            
            if (IsDeployed)
            {
                apiKey = Environment.GetEnvironmentVariable("json_storage_api_key");
                securityKey = Environment.GetEnvironmentVariable("json_storage_security_key");
            }
            else
            {
                var jsonStorageSecrets = JObject.Parse(File.ReadAllText("./Main/json_storage_secrets.json"));
                apiKey = jsonStorageSecrets.Value<string>("api_key");
                securityKey = jsonStorageSecrets.Value<string>("security_key");
            }
            
            return (apiKey, securityKey);
        }
        
        private static (string ownerUsername, string botUsername, string botClientId) GetBotInfo(
            IConfiguration configuration)
        {
            var ownerUsername = configuration["BotInfo:OwnerUsername"];
            var botUsername = configuration["BotInfo:BotUsername"];
            var botClientId = configuration["BotInfo:BotUserId"];
            
            return (ownerUsername, botUsername, botClientId);
        }

        private static JObject GetJsonStorageBin(string binId)
        {
            _commandsInfoRequest = new RestRequest("bin/" + binId);
            var response = _jsonStorageClient.Execute(_commandsInfoRequest, Method.GET).Content;
            return JObject.Parse(response);
        }

        private static RestClient GetJsonStorageClient(string baseUrl, string apiKey, string securityKey)
        {
            var jsonStorageClient = new RestClient(baseUrl);
            jsonStorageClient.AddDefaultHeader("Api-key", apiKey);
            jsonStorageClient.AddDefaultHeader("Security-key", securityKey);
            return jsonStorageClient;
        }
    }
}
