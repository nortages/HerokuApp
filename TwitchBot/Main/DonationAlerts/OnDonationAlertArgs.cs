using System;
using Newtonsoft.Json;
using TwitchBot.Main.Converters;

namespace TwitchBot.Main.DonationAlerts
{
    [JsonObject]
    public class OnDonationAlertArgs : EventArgs
    {
        public int Id;
        public double Amount;
        public string Currency;
        public string Username;
        public string Message;
        public string ChannelUsername;
        
        [JsonProperty("created_at")]
        public DateTime? CreatedAt;
        
        [JsonConverter(typeof(BoolConverter))] [JsonProperty("is_shown")]
        public bool IsShown;

        [JsonProperty("message_type")]
        public DonationMessageType MessageType;
        
        [JsonProperty("shown_at")]
        public DateTime? ShownAt;
    }
}