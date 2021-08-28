using System.ComponentModel;
using Newtonsoft.Json;

namespace TwitchBot.Main
{
    [JsonObject]
    public class RewardRedemptionInfo : BaseCallbackInfo
    {
        public string Title { get; set; }
        
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)] public override bool HasCallback { get; set; }
    }
}
