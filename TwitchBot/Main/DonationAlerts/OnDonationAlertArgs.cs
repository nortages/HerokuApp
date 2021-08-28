using System;
using Newtonsoft.Json;
using TwitchBot.Main.Converters;

namespace TwitchBot.Main.DonationAlerts
{
    [JsonObject]
    public class OnDonationAlertArgs : EventArgs
    {
        public int Id;
        public string Username;
        [JsonProperty("message_type")] public DonationMessageType MessageType;
        public string Message;
        public double Amount;
        public string Currency;
        [JsonConverter(typeof(BoolConverter))]
        [JsonProperty("is_shown")] public bool IsShown;
        [JsonProperty("created_at")] public DateTime? CreatedAt;
        [JsonProperty("shown_at")] public DateTime? ShownAt;
    }
}