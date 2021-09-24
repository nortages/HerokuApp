using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwitchBot.Main.Callbacks;
using TwitchBot.Main.ExtensionsMethods;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;

namespace TwitchBot.Main
{
    public static class UtilityFunctions
    {

        public static string FormatTimespan(TimeSpan timespan)
        {
            var options = new (string word, string[] endings, int howMuch, string timespanAttribute, int value)[] {
                ("", new string[] { "день", "дня", "дней" }, 24, "TotalDays", 0),
                ("час", new string[] { "", "а", "ов" }, 60, "TotalHours", 0),
                ("минут", new string[] { "у", "ы", "" }, 60, "TotalMinutes", 0),
            };
            var resultParts = new List<string>();
            for (int i = 0; i < options.Length; i++)
            {
                var totalValue = (int)Math.Floor((double)typeof(TimeSpan).GetProperty(options[i].timespanAttribute).GetValue(timespan));
                for (int j = 0; j < i; j++)
                {
                    var valueToMult = options[j].value;
                    for (int k = j; k < i; k++)
                    {
                        valueToMult *= options[k].howMuch;
                    }
                    totalValue -= valueToMult;
                }
                options[i].value = totalValue;
                if (totalValue != 0)
                {
                    var completeWord = options[i].word + GetWordEnding(totalValue, options[i].endings);
                    var part = $"{totalValue} {completeWord}";
                    resultParts.Add(part);
                }
            }
            if (resultParts.Count > 1) resultParts.Insert(resultParts.Count - 1, "и");
            return string.Join(" ", resultParts);
        }

        public static string GetWordEnding(int num, string[] endings)
        {
            var ending = (num % 100) switch
            {
                11 or 12 or 13 or 14 => endings[2],
                _ => (num % 10) switch
                {
                    1 => endings[0],
                    2 or 3 or 4 => endings[1],
                    5 or 6 or 7 or 8 or 9 or 0 => endings[2],
                    _ => ""
                }
            };
            return ending;
        }

        public static void TimeoutCommandUser(ChatCommand command, CallbackArgs args, TimeSpan timeoutTime, string username = null, string reason = "")
        {
            if (command.ChatMessage.IsBroadcaster ||
                command.ChatMessage.IsMe) return;

            var usernameToBan = string.IsNullOrEmpty(username) ? command.ChatMessage.Username : username;
            var channel = command.ChatMessage.Channel;

            if (command.ChatMessage.IsModerator)
            {
                if (args.ChannelBot.ChannelTwitchClient == null) return;

                args.ChannelBot.ChannelTwitchClient.TimeoutModer(channel, usernameToBan, timeoutTime, reason);
            }
            else
            {
                MainBotService.BotTwitchClient.TimeoutUser(channel, usernameToBan, timeoutTime, reason);
            }
        }
    }
}
