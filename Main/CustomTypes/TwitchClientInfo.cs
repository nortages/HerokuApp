using Newtonsoft.Json;
using TwitchLib.Client.Events;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;
using NewtonsoftJsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace HerokuApp
{
    public class TwitchClientInfo
    {
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        //[JsonProperty("events")]
        //public Dictionary<string, string> Events { get; set; }

        [JsonProperty("OnWhisperReceived")]
        [NewtonsoftJsonConverter(typeof(MethodNameToCallbackConverter<OnWhisperReceivedArgs>))]
        public Callback<OnWhisperReceivedArgs> OnWhisperReceivedCallback { get; set; }

        [JsonProperty("OnUserTimedout")]
        [NewtonsoftJsonConverter(typeof(MethodNameToCallbackConverter<OnUserTimedoutArgs>))]
        public Callback<OnUserTimedoutArgs> OnUserTimedoutCallback { get; set; }

        [JsonProperty("AdditionalOnMessageReceived")]
        [NewtonsoftJsonConverter(typeof(MethodNameToCallbackConverter<OnMessageReceivedArgs>))]
        public Callback<OnMessageReceivedArgs> AdditionalOnMessageReceivedCallback { get; set; }
    }
}
