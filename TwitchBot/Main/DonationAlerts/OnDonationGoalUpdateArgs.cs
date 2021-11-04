using System;
using Newtonsoft.Json;

namespace TwitchBot.Main.DonationAlerts
{
    public class OnDonationGoalUpdateArgs
    {
        public int Id;
        public string ChannelUsername;
        public string Currency;
        public string Title;
        
        [JsonProperty("expires_at")] 
        public DateTime? ExpiresAt;
        
        [JsonProperty("goal_amount")] 
        public double GoalAmount;
        
        [JsonProperty("is_active")] 
        public int IsActive;
        
        [JsonProperty("raised_amount")] 
        public double RaisedAmount;
        
        [JsonProperty("start_amount")] 
        public double StartAmount;
        
        [JsonProperty("started_at")] 
        public DateTime StartedAt;
    }
}