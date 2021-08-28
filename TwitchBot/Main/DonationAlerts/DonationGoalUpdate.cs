using System;
using Newtonsoft.Json;

namespace TwitchBot.Main.DonationAlerts
{
    public class DonationGoalUpdate
    {
        public int Id;
        [JsonProperty("is_active")] public int IsActive;
        public string Title;
        public string Currency;
        [JsonProperty("start_amount")] public double StartAmount;
        [JsonProperty("raised_amount")] public double RaisedAmount;
        [JsonProperty("goal_amount")] public double GoalAmount;
        [JsonProperty("started_at")] public DateTime StartedAt;
        [JsonProperty("expires_at")] public DateTime? ExpiresAt;
    }
}