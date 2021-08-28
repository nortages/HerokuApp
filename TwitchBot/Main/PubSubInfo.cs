using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchBot.Main
{
    [JsonObject]
    public class PubSubInfo
    {
        public bool IsEnabled { get; set; }
        public List<RewardRedemptionInfo> OnRewardRedeemedCallbacks { get; set; }
    }
}
