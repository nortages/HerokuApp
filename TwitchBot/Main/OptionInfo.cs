using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using TwitchBot.Main.ExtensionsMethods;
using TwitchLib.Client.Events;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;
using JsonNetConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace TwitchBot.Main
{
    [JsonObject]
    public class OptionInfo : BaseCallbackInfo
    {
        public double? Probability { get; set; }
        
        public string Answer { get; set; }
        public Dictionary<string, string> MultiLangAnswer { get; set; }
        public bool? IsMentionRequired { get; set; }
        public List<OptionInfo> Options { get; set; }
        
        [NewtonsoftJsonIgnore] public OptionInfo Parent { get; set; }
        [NewtonsoftJsonIgnore] public Dictionary<string, int> UsageFrequency { get; set; } = new();

        public string GetMultiLangAnswer(string lang)
        {
            if (!MultiLangAnswer.ContainsKey(lang)) lang = "ru";
            var answer = MultiLangAnswer[lang];
            return answer;
        }

        public string GetAnswer(object o, EventArgs e, CallbackArgs args)
        {
            string answer;
            if (Callback != null)
            {
                answer = Callback(o, e, args);
            }
            else
            {
                if (!string.IsNullOrEmpty(Answer))
                {
                    answer = Answer;
                }
                else if (MultiLangAnswer != null)
                {
                    answer = GetMultiLangAnswer(args.Lang);
                }
                else
                {
                    answer = GetAnswerFromOptions(o, e, args);
                }
                
                const string optionVariable = "${option}";
                if (answer.Contains(optionVariable))
                {
                    answer = answer.Replace(optionVariable, GetAnswerFromOptions(this, e, args));
                }
            }
            args.IsMentionRequired = IsMentionRequired ?? args.IsMentionRequired;
            
            return answer;
        }

        public string GetAnswerFromOptions(object o, EventArgs e, CallbackArgs args)
        {
            if (Options == null) return null;
            var command = (e as OnChatCommandReceivedArgs)?.Command;
            if (command == null)
                throw new ArgumentException();

            var username = command.ChatMessage.Username;
            var randDouble = Program.Rand.NextDouble();

            if (args.IsTestMode)
            {
                var numberOfNesting = GetNumberOfNesting(this);
                if (command.ArgumentsAsList.Count >= numberOfNesting + 1)
                {
                    randDouble = GetValueFromArguments(command.ArgumentsAsList, numberOfNesting, randDouble);
                }
            }

            string answer;
            try
            {
                var option = Options.GetProbableOption(randDouble);
                answer = option.GetAnswer(option, e, args);
                if (this is CommandInfo commandInfo) commandInfo.LastOption[username] = option;
            }
            catch (ArgumentException ex)
            {
                answer = ex.Message;
            }

            return answer;
        }

        private int GetNumberOfNesting(OptionInfo optionInfo)
        {
            var currentOption = optionInfo;
            var numberOfNesting = 0;
            while (true)
            {
                if (currentOption.Parent == null) break;
                currentOption = currentOption.Parent;
                numberOfNesting++;
            }

            return numberOfNesting;
        }

        private static double GetValueFromArguments(List<string> commandArguments, int numberOfNesting, double randDouble)
        {
            var argument = commandArguments[numberOfNesting];
            return double.Parse(argument, NumberStyles.Any, CultureInfo.InvariantCulture);
        }
    }
}
