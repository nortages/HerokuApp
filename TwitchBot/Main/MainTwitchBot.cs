using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchBot.Main.Hearthstone;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;

namespace TwitchBot.Main
{
    public class MainTwitchBot
    {
        private static IConfiguration _configuration;
        private static ILogger _logger;
        private static ILoggerProvider _loggerProvider;
        public static List<Bot> ChannelsBots { get; } = new();

        public MainTwitchBot(IConfiguration configuration, ILoggerProvider loggerProvider)
        {
            _configuration = configuration;
            _loggerProvider = loggerProvider;
        }

        public void Connect()
        {
            Program.OnProcessExit += ManageProcessExit;
            _logger = _loggerProvider.CreateLogger(GetType().Name);
            Config.Build(_configuration);
            ChannelsBots.AddRange(GetChannelsBots());
            HearthstoneApiClient.GetBattlegroundsMinions();
        }

        private static void SetParent(OptionInfo optionInfo)
        {
            if (optionInfo.Options == null) return;

            foreach (var nestedOption in optionInfo.Options)
            {
                nestedOption.Parent = optionInfo;
                SetParent(nestedOption);
            }
        }

        private static IEnumerable<Bot> GetChannelsBots()
        {
            var bots = new List<Bot>();
            const string path = "./Main/Profiles";
            var profiles = Directory.GetDirectories(path);

            var hasTestProfile = false;
            if (profiles.Any(p => p.Contains("_test")))
            {
                hasTestProfile = true;
                profiles = profiles.Where(p => !p.Contains("_test")).ToArray();
            }
            
            foreach (var profileDirPath in profiles)
            {
                try
                {
                    var bot = GetBot(profileDirPath);
                    var commandsJObject = JObject.Parse(File.ReadAllText($"{profileDirPath}/commands.json"));
                    GetCommands(commandsJObject, bot);
                    bots.Add(bot);
                    if (bot.IsEnabled) bot.Connect();
                }
                catch (JsonReaderException e)
                {
                    _logger.Log(LogLevel.Error, e.Message);
                }
                catch (FileNotFoundException e)
                {
                    Console.Error.WriteLine(e);
                }
            }

            if (hasTestProfile)
            {
                var testBot = GetBot(path + "/_test");
                foreach (var bot in bots)
                {
                    var botCommands = bot.Commands.Where(c => testBot.Commands.All(n => n.Id != c.Id));
                    testBot.Commands.AddRange(botCommands);
                }

                bots.Add(testBot);
                if (testBot.IsEnabled) testBot.Connect();
            }
            
            return bots;
        }

        private static Bot GetBot(string profileDirPath)
        {
            var botInfoJObject = JObject.Parse(File.ReadAllText($"{profileDirPath}/bot_info.json"));
            var bot = botInfoJObject.ToObject<Bot>();
            if (bot == null)
                throw new ArgumentException("Can't cast bot's JObject to type Bot");
            bot.CreateLogger(_loggerProvider);
                
            return bot;
        }

        private static void GetCommands(JToken commandsJObject, Bot bot)
        {
            var commands = commandsJObject.Value<JArray>("Commands");
            if (commands != null)
            {
                foreach (var token in commands)
                {
                    var command = token.ToObject<CommandInfo>();
                    SetParent(command);
                    bot.Commands.Add(command);
                }
            }

            var fileContents = File.ReadAllText("./Main/Profiles/general_commands.json");
            var generalCommandsJObject = JObject.Parse(fileContents);

            var generalCommandsInfo = commandsJObject.Value<JArray>("GeneralCommandsInfo");
            bot.Commands.AddRange(GetGeneralItems<CommandInfo>(generalCommandsInfo, generalCommandsJObject, "GeneralCommands"));
           
            var generalMessageCommandsInfo = commandsJObject.Value<JArray>("GeneralMessageCommandsInfo");
            bot.MessageCommands.AddRange(GetGeneralItems<MessageCommandInfo>(generalMessageCommandsInfo, generalCommandsJObject, "GeneralMessageCommands"));

        }

        private static List<T> GetGeneralItems<T>(JArray generalItemsInfo, JToken generalItemsJObject, string key)
        {
            if (generalItemsJObject == null)
                throw new ArgumentException();
            
            if (generalItemsInfo == null)
                throw new ArgumentException();
            
            var generalItems = new List<T>();
            foreach (var token in generalItemsInfo)
            {
                var items = generalItemsJObject.Value<JArray>(key);
                if (items == null)
                    throw new InvalidOperationException("Items not found");

                var generalMessageCommandInfo = token.ToObject<Item>();
                if (generalMessageCommandInfo == null)
                    throw new ArgumentException();
                var genMesCommand =
                    items.Single(n => n.Value<string>("Id") == generalMessageCommandInfo.Id);
                generalItems.Add(genMesCommand.ToObject<T>());
            }

            return generalItems;
        }
        
        private static void ManageProcessExit(object _, EventArgs e)
        {
            foreach (var bot in ChannelsBots.Where(bot => bot.IsEnabled))
            {
                bot.Disconnect();
            }
        }
        
        public static void TestCommands()
        {
            var client = new TwitchClient(new FDGTClient());
            client.Initialize(
                new ConnectionCredentials(
                    Config.BotUsername,
                    Config.GetTwitchAccessToken()
                ), "fdgt"
            );
 
            client.Connect();
            // client.SendMessage("#channel", "bits");
            client.OnChatCommandReceived += (sender, args) =>
            {
                Console.WriteLine("");
            };
        }
    }
}