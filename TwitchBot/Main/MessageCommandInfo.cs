using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using TwitchBot.Main.Converters;
using NewtonsoftJsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace TwitchBot.Main
{
    [JsonObject]
    public class MessageCommandInfo : BaseCallbackInfo
    {
        public bool IsMentionRequired { get; set; }
        public double? Probability { get; set; }
        public string Answer { get; set; }
        public Dictionary<string, string> MultiLangAnswer { get; set; }

        [NewtonsoftJsonConverter(typeof(StringToRegexConverter))]
        public Regex Regex { get; set; }
    }
}
