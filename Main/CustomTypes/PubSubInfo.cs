using System.Collections.Generic;
using Newtonsoft.Json;

namespace HerokuApp
{
    public class PubSubInfo
    {
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("OnRewardRedeemed")]
        public List<RewardInfo> RewardsInfo { get; set; }
    }
}
