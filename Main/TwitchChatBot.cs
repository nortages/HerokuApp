using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using HerokuApp.Main.CustomTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Serialization;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace HerokuApp.Main
{
    public static partial class NortagesTwitchBot
    {
        const int SAVEDCHATMESSAGESNUM = 100;
        public static Queue<ChatMessage> chatMessages = new Queue<ChatMessage>(SAVEDCHATMESSAGESNUM);

        public static void Connect()
        {
            Console.WriteLine("Connect method is invoked");

            var mainConfig = Config.MainConfig;
            foreach (var item in mainConfig.BotsInfo)
            {
                foreach (var command in item.Commands)
                {
                    SetParent(command);
                }
                foreach (var commandInfo in item.GeneralCommandsInfo)
                {
                    if (!commandInfo.IsEnabled) continue;

                    var genCommand = Config.MainConfig.GeneralCommands.Single(n => n.Id == commandInfo.Id);
                    item.Commands.Add(genCommand);
                }
                foreach (var messageCommandInfo in item.GeneralMessageCommandsInfo)
                {
                    if (!messageCommandInfo.IsEnabled) continue;

                    var genMesCommand = Config.MainConfig.GeneralMessageCommands.Single(n => n.Id == messageCommandInfo.Id);
                    item.MessageCommands.Add(genMesCommand);
                }
            }
            
            Program.OnProcessExit += ManageProcessExit;
            HearthstoneApiClient.GetBattlegroundsMinions();
        }

        private static void SetParent(Option option)
        {
            if (option.Options == null) return;

            foreach (var nestedOption in option.Options)
            {
                nestedOption.Parent = option;
                SetParent(nestedOption);
            }
        }

        private static void ManageProcessExit(object _, EventArgs e)
        {            
            foreach (var bot in Config.MainConfig.BotsInfo)
            {
                bot.Disconnect();
            }
        }

        static public void TestCommands()
        {
            var client = new TwitchClient(new FDGTClient());
            client.Initialize(
                new ConnectionCredentials(
                    Config.BotUsername,
                    Config.GetTwitchAccessToken()
                ), "fdgt"
            );
 
            client.Connect();
            client.SendMessage("#channel", "bits");
            
            var pubsub = new TwitchPubSub();
            
        }

        public static Dictionary<ProbabilityOptionInfo, int> CheckCommandOptionsFrequency(Command command, int sampleNum)
        {
            var optionsFrequency = new Dictionary<ProbabilityOptionInfo, int>();
            for (int i = 0; i < sampleNum; i++)
            {
                var option = command.GetRandProbabilityOption(Config.rand.NextDouble());
                if (!optionsFrequency.ContainsKey(option))
                {
                    optionsFrequency.Add(option, 0);
                }
                optionsFrequency[option]++;
            }
            return optionsFrequency.OrderByDescending(kv => kv.Key.Probability).ToDictionary(kv => kv.Key, kv => kv.Value);
        }
    }
}