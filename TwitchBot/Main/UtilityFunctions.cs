using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using TwitchBot.Main.ExtensionsMethods;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;

namespace TwitchBot.Main
{
    public static class UtilityFunctions
    {
        private const BindingFlags BindingFlags = System.Reflection.BindingFlags.InvokeMethod | 
                                                  System.Reflection.BindingFlags.IgnoreCase | 
                                                  System.Reflection.BindingFlags.Static | 
                                                  System.Reflection.BindingFlags.Public;

        public static bool IsMethodTask(string methodName)
        {
            var methodInfo = GetCallbackMethodInfo(methodName);
            return methodInfo.ReturnType == typeof(Task);
        }

        private static MethodInfo GetCallbackMethodInfo(string methodName)
        {
            var methodInfo = typeof(Callbacks).GetMethod(methodName, BindingFlags);
            if (methodInfo == null)
                throw new ArgumentException($"Method info is not found. Method name: {methodName}");
            return methodInfo;
        }
    
        public static object CallMethodByName(Type type, string methodName, params object[] args)
        {            
            var methodInfo = type.GetMethod(methodName, BindingFlags);
            return methodInfo?.Invoke(null, args);
            // var action = (Func<>)Delegate.CreateDelegate
            //     (typeof(Action), methodInfo);
            // action();
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

        public static void TimeoutCommandUser(ChatCommand command, CallbackArgs args, TimeSpan timeoutTime, string username = null, string reason = "")
        {
            if (command.ChatMessage.IsBroadcaster ||
                command.ChatMessage.IsMe) return;

            var usernameToBan = string.IsNullOrEmpty(username) ? command.ChatMessage.Username : username;
            var channel = command.ChatMessage.Channel;

            if (command.ChatMessage.IsModerator)
            {
                if (args.Bot.StreamerBot?.TwitchClient == null) return;

                args.Bot.StreamerBot.TwitchClient.TimeoutModer(channel, usernameToBan, timeoutTime, reason);
            }
            else
            {
                args.Bot.TwitchClient.TimeoutUser(channel, usernameToBan, timeoutTime, reason);
            }
        }
    }
}
