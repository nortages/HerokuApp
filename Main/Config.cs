using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using TwitchLib.Client.Models;

namespace HerokuApp
{
    internal static class Config
    {
        public static readonly Random rand = new Random();
        public static readonly StringComparison stringComparison = StringComparison.OrdinalIgnoreCase;
        public static bool isDeployed = Environment.GetEnvironmentVariable("DEPLOYED") != null;

        public static string BotUsername => "NortagesBot";
        public static string OwnerUsername => "Segatron_Lapki";
        public static string BotClientId => "gp762nuuoqcoxypju8c569th9wz7q5";
        public static string BotId => "541161434";
        private static RestClient jsonStorageClient;
        private static RestRequest commandsInfoRequest;

        public static MainConfig MainConfig { get; set; }

        static readonly JObject commandsInfo;
        public static readonly JObject config;

        public static void SaveChatMessages()
        {
            Console.WriteLine("Start saving messages...");
            var patchDict = new Dictionary<string, Queue<ChatMessage>>
            {
                { "chatMessages", Main.NortagesTwitchBot.chatMessages }
            };
            var patchJsonString = JsonConvert.SerializeObject(patchDict);
            Console.WriteLine("Send them to storage...");
            //PatchRequest(patchJsonString, commandsClient);
            Console.WriteLine("Saved!");
        }

        public static void LoadChatMessages()
        {
            var messages = commandsInfo.Value<JArray>("chatMessages");
            Console.WriteLine("Saved messages: " + messages.Count);
            if (messages.Count != 0)
            {
                Main.NortagesTwitchBot.chatMessages = messages.ToObject<Queue<ChatMessage>>();
            }
        }

        public static T GetFromCommandsInfo<T>(string key) => commandsInfo[key].Value<T>();

        public static JProperty GetJPropertyFromCommandsInfo(string path)
        {
            // TODO: Add if not exist.
            JObject rootObject;
            string key;
            var splittedPath = path.Split('.');
            if (splittedPath.Count() == 1)
            {
                rootObject = commandsInfo;
                key = path;
            }
            else
            {
                key = splittedPath.Last();
                var neededPath = "$." + string.Join('.', splittedPath.TakeWhile(n => !n.Equals(key)));
                rootObject = commandsInfo.SelectToken(neededPath).Value<JObject>();
            }
            return rootObject.Properties().First(pr => string.Equals(pr.Name, key, StringComparison.InvariantCultureIgnoreCase));
        }

        public static string GetTwitchAccessToken() => GetTwitchAccessToken(BotUsername);

        public static string GetTwitchAccessToken(string username)
        {
            var request = new RestRequest("bin/" + GetConfigVariable("config_bin_id"), Method.GET);
            var response = JObject.Parse(jsonStorageClient.Execute(request).Content);
            return (string) response.SelectToken($"$.tokens.{username}.accessToken");
        }

        public static string GetConfigVariable(string key) => config.Value<string>(key);

        public static event EventHandler OnConfigSaves;

        public static void SaveCommandsInfo()
        {
            Console.WriteLine("Saving config...");
            OnConfigSaves?.Invoke(null, null);
            commandsInfoRequest.AddJsonBody(JsonConvert.SerializeObject(commandsInfo));
            jsonStorageClient.Execute(commandsInfoRequest, Method.PUT);
        }

        static Config()
        {
            config = JObject.Parse(File.ReadAllText("configs.json"));

            jsonStorageClient = new RestClient(GetConfigVariable("jsonstorage_base_url"));
            jsonStorageClient.AddDefaultHeader("Api-key", GetConfigVariable("jsonstorage_api_key"));
            jsonStorageClient.AddDefaultHeader("Security-key", GetConfigVariable("jsonstorage_security_key"));

            commandsInfoRequest = new RestRequest("bin/" + GetConfigVariable("commands_info_bin_id"));
            var response = jsonStorageClient.Execute(commandsInfoRequest, Method.GET).Content;
            commandsInfo = JObject.Parse(response);

            if (isDeployed)
            {
                Program.OnProcessExit += (_, e) => SaveCommandsInfo();

                System.Timers.Timer timer = new System.Timers.Timer(TimeSpan.FromMinutes(15).TotalMilliseconds);
                timer.AutoReset = true;
                timer.Elapsed += delegate { SaveCommandsInfo(); };
                timer.Start(); 
            }

            var path = "./Main/CommandsFiles/MainConfig.json";
            var content = File.ReadAllText(path);
            MainConfig = JsonConvert.DeserializeObject<MainConfig>(content, new JsonSerializerSettings { PreserveReferencesHandling = PreserveReferencesHandling.Objects });

            //var chatMessagesId = "06054dfa9012";
            //commandsClient = new RestClient($"{apiUrl}{chatMessagesId}");
            //commandsClient.AddDefaultHeader("Api-key", apiKey);
            //commandsClient.AddDefaultHeader("Security-key", securityKey);
            //commandsInfo = JsonConvert.DeserializeObject<JObject>(GetRequest(commandsClient));

            //var streamEventsWebhookExpirationDate = config["streamEventsWebhook"].Value<DateTime>();
            //if ((streamEventsWebhookExpirationDate - DateTime.Now).TotalDays < 2)
            //{
            //    var callbackUrl = "https://asp-docker.herokuapp.com/twitch-webhooks/streams/";
            //    var duration = TimeSpan.FromSeconds(864000);
            //    TwitchHelpers.SubscribeToStreamEvents(callbackUrl, "466176261", duration);
            //    var nestedDict = new JObject
            //    {
            //        { "StreamEventsWebhook", DateTime.Now.AddDays(duration.TotalDays) }
            //    };
            //    var newConfig = new JObject
            //    {
            //        { "ExpirationDates", nestedDict }
            //    };
            //    var patchJsonString = JsonConvert.SerializeObject(newConfig);
            //    PatchRequest(patchJsonString, client);
            //}
        }
    }
}
