using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;
using TwitchLib.Client.Events;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;
using NewtonsoftJsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;
using System.Text.RegularExpressions;

namespace HerokuApp
{
    public class MessageCommand
    {
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("is_mention_required", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        public bool IsMentionRequired { get; set; }

        [JsonProperty("regex")]
        [NewtonsoftJsonConverter(typeof(StringToRegexConverter))]
        public Regex Regex { get; set; }

        [JsonProperty("callback_name")]
        [NewtonsoftJsonConverter(typeof(MethodNameToCallbackConverter<OnChatCommandReceivedArgs>))]
        public Callback<OnChatCommandReceivedArgs> Callback { get; set; }

        [JsonProperty("answer")]
        public Dictionary<string, string> Answer { get; set; }
    }
}
