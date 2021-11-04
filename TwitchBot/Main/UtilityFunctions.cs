using System;
using System.Collections.Generic;
using TwitchBot.Main.ExtensionsMethods;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;

namespace TwitchBot.Main
{
    public static class UtilityFunctions
    {
        public static string FormatTimespan(TimeSpan timespan)
        {
            var resultParts = new List<string>();

            var days = timespan.Days;
            if (days != 0)
            {
                var daysWord = "" + GetRussianWordEnding(days, new[] {"день", "дня", "дней"});
                var daysPart = $"{days} {daysWord}";
                resultParts.Add(daysPart);
            }

            var hours = timespan.Hours;
            if (hours != 0)
            {
                var hourWord = "час" + GetRussianWordEnding(hours, new[] {"", "а", "ов"});
                var hourPart = $"{hours} {hourWord}";
                resultParts.Add(hourPart);
            }

            var minutes = timespan.Minutes;
            if (minutes != 0)
            {
                var minutesWord = "минут" + GetRussianWordEnding(minutes, new[] {"у", "ы", ""});
                var minutesPart = $"{minutes} {minutesWord}";
                resultParts.Add(minutesPart);
            }

            // Inserts the 'и' conjunction before the last part if there is several parts.
            if (resultParts.Count > 1)
                resultParts.Insert(resultParts.Count - 1, "и");

            return string.Join(" ", resultParts);
        }

        public static string GetRussianWordEnding(int num, string[] endings)
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

        public static void TimeoutCommandUser(ChatCommand command, CallbackArgs args, TimeSpan timeoutTime,
            string username = null, string reason = "")
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
                BotService.BotTwitchClient.TimeoutUser(channel, usernameToBan, timeoutTime, reason);
            }
        }
    }
}