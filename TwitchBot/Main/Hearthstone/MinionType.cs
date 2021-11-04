using Newtonsoft.Json;
using TwitchBot.Main.Converters;

namespace TwitchBot.Main.Hearthstone
{
    [JsonConverter(typeof(IntToEnumConverter))]
    public enum MinionType
    {
        None = -1,
        Without = 0,
        Murloc = 14,
        Demon = 15,
        Mech = 17,
        Elemental = 18,
        Beast = 20,
        Totem = 21,
        Pirate = 23,
        Dragon = 24,
        All = 26
    }
}