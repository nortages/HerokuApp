using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using TwitchLib.Client.Events;

using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;
using NewtonsoftJsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;
using HerokuApp.Main.Interfaces;

namespace HerokuApp
{
    public class Option : ISomeInterface
    {
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [NewtonsoftJsonIgnore]
        public Option Parent { get; set; }

        [JsonProperty("callback_name")]
        [NewtonsoftJsonConverter(typeof(MethodNameToCallbackConverter<OnChatCommandReceivedArgs>))]
        public Callback<OnChatCommandReceivedArgs> Callback { get; set; }

        [JsonProperty("answer")]
        public Dictionary<string, string> Answer { get; set; }

        [JsonProperty("options")]
        public List<ProbabilityOptionInfo> Options { get; set; }

        [NewtonsoftJsonIgnore]
        public Dictionary<string, int> usageFrequency = new Dictionary<string, int>();

        [NewtonsoftJsonIgnore]
        public List<double> convertedProbabilities;

        public string GetUnformattedAnswer(string lang)
        {
            if (!Answer.ContainsKey(lang)) lang = "ru";
            var answer = Answer[lang];
            return answer;
        }

        public string GetAnswer(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            string answer = null;
            if (Callback != null)
            {
                answer = Callback(args);
            }
            else
            {
                if (Answer != null)
                {
                    answer = GetUnformattedAnswer(args.lang);
                }
                else
                {
                    answer = GetAnswerFromOptions(args);
                }
            }
            return answer;
        }

        public string GetAnswerFromOptions(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            if (Options == null) return null;

            var username = args.e.Command.ChatMessage.Username;
            var randDouble = Config.rand.NextDouble();

            var numberOfNesting = 0;
            Option opt = this;
            while (true)
            {
                if (opt.Parent != null)
                {
                    opt = opt.Parent;
                    numberOfNesting++;
                }
                else
                {
                    break;
                }
            }
            if (args.isTestMode &&
                args.e.Command.ArgumentsAsList.Count >= numberOfNesting + 1)
            {
                var argument = args.e.Command.ArgumentsAsList[numberOfNesting];
                if (double.TryParse(argument, NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    randDouble = value;
                }
            }

            string answer;
            try
            {
                var option = GetRandProbabilityOption(randDouble);
                args.sender = option;
                answer = option.GetAnswer(args);
                if (this is Command command) command.lastOption[username] = option;
            }
            catch (ArgumentException ex)
            {
                answer = ex.Message;
            }

            return answer;
        }

        public ProbabilityOptionInfo GetRandProbabilityOption(double randDouble)
        {
            if (randDouble > 1 || randDouble < 0)
            {
                throw new ArgumentException("Значение должно быть между 0 и 1");
            }

            ProbabilityOptionInfo result = null;
            convertedProbabilities ??= GetConvertedProbabilities(Options);
            var converted = convertedProbabilities;
            for (int i = 0; i < converted.Count; i++)
            {
                if (!(converted[i] >= randDouble)) continue;
                result = Options[i];
                break;
            }
            result ??= Options.Last();
            return result;
        }

        private List<double> GetConvertedProbabilities(List<ProbabilityOptionInfo> options)
        {
            if (options.First().Probability == null)
            {
                options.ForEach(n => n.Probability = 1.0 / options.Count);
            }

            var sums = new List<double>();
            foreach (var option in options)
            {
                var sum = sums.LastOrDefault();
                var newSum = sum + (double)option.Probability;
                sums.Add(newSum);
            }

            return sums;
        }

    }
}
