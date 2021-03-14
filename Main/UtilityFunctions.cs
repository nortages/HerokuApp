using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HerokuApp.Main;
using HerokuApp.Main.Extensions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;

namespace HerokuApp
{
    public static class UtilityFunctions
    {
        const BindingFlags bindingFlags = BindingFlags.InvokeMethod | BindingFlags.IgnoreCase | BindingFlags.Static | BindingFlags.Public;

        public static T CallMethodByName<T>(string methodName, params object[] args)
        {            
            var methodInfo = typeof(NortagesTwitchBot).GetMethod(methodName, bindingFlags);
            return (T)methodInfo?.Invoke(null, args);
        }

        public static void AddEventToTarget(object target, string eventName, string eventHandlerName)
        {   
            var eventInfo = target.GetType().GetEvent(eventName);
            var eventType = eventInfo.EventHandlerType;
            var methodInfo = typeof(NortagesTwitchBot).GetMethod(eventHandlerName, bindingFlags);
            Delegate d = Delegate.CreateDelegate(eventType, target, methodInfo);
            //Action<object, object> handler = HandleRfqSendComment;
            //var newArgs = new CallbackArgs
            //EventHandler d = delegate (object s, EventArgs args) => CallMethodByName<string>(eventHandlerName, args);
            //EventHandler handler = (sender, args) => CallMethodByName<string>(eventHandlerName, args);
            //var newHandler = Convert.ChangeType(handler, eventType);
            eventInfo.AddEventHandler(target, ConvertDelegate(d, eventType));
        }

        public static Delegate ConvertDelegate(Delegate originalDelegate, Type targetDelegateType)
        {
            return Delegate.CreateDelegate(
                targetDelegateType,
                originalDelegate.Target,
                originalDelegate.Method);
        }

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
            var ending = "";
            switch (num % 10)
            {
                case 1:
                    ending = endings[0];
                    break;
                case 2:
                case 3:
                case 4:
                    ending = endings[1];
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 0:
                    ending = endings[2];
                    break;
            }
            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                case 14:
                    ending = endings[2];
                    break;
            }

            return ending;
        }

        public static void ls()
        {
            var folders = Directory.GetDirectories(Environment.CurrentDirectory);
            var files = Directory.GetFiles(Environment.CurrentDirectory);
            var combined = folders.Concat(files).ToArray();
            foreach (var file in combined)
            {
                Console.WriteLine(file);
            }
        }

        public static void TimeoutCommandUser(CallbackArgs<OnChatCommandReceivedArgs> args, TimeSpan timeoutTime, string reason = "")
        {
            if (args.e.Command.ChatMessage.IsBroadcaster ||
                args.e.Command.ChatMessage.IsMe) return;

            var username = args.e.Command.ChatMessage.Username;
            var channel = args.e.Command.ChatMessage.Channel;

            if (args.e.Command.ChatMessage.IsModerator)
            {
                if (args.bot.HelperBot == null ||
                    args.bot.HelperBot.twitchClient == null) return;

                args.bot.HelperBot.twitchClient.TimeoutModer(channel, username, timeoutTime, reason);
                Task.Delay(timeoutTime.Add(TimeSpan.FromSeconds(1))).ContinueWith(t => args.bot.HelperBot.twitchClient.SendMessage(channel, $"/mod {username}"));
            }
            else
            {
                args.bot.twitchClient.TimeoutUser(channel, username, timeoutTime, reason);
            }

        }
    }
}
