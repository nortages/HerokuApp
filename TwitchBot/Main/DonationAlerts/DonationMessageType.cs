using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace TwitchBot.Main.DonationAlerts
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DonationMessageType { Text, Audio };
}