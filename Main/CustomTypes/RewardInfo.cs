using Newtonsoft.Json;
using TwitchLib.PubSub.Events;
using NewtonsoftJsonConverter = Newtonsoft.Json.JsonConverter;
using NewtonsoftJsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace HerokuApp
{
    public class RewardInfo
    {
        [JsonProperty("id")]
        public string Name { get; set; }

        [JsonProperty("callback")]
        [NewtonsoftJsonConverter(typeof(MethodNameToCallbackConverter<OnRewardRedeemedArgs>))]
        public Callback<OnRewardRedeemedArgs> Callback { get; set; }
    }
}
