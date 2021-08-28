using Newtonsoft.Json;
using TwitchBot.Main.Converters;

namespace TwitchBot.Main.Hearthstone
{
    [JsonConverter(typeof(IntToEnumConverter))]
    public enum MinionRarity
    {
        None = -1,
        Without = 0,
        Common = 1,
        Free = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5
    }
}