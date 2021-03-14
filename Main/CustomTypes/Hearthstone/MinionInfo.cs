using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace HerokuApp.Main.CustomTypes
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
        All = 26,
    }

    public enum MinionKeyword
    {
        None,
        Taunt = 1,
        DivineShield = 3,
        Battlecry = 8,
        Windfury = 11,
        Deathrattle = 12,
        Poisonous = 32,
        Overkill = 61,
        Reborn = 78,
    }

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

    public class MinionInfo
    {
        public int Id { get; set; }

        [JsonProperty("rarityId")] public MinionRarity Rarity { get; set; }
        [JsonProperty("minionTypeId")] public MinionType MinionType { get; set; }
        
        public int Health { get; set; }
        public int Attack { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public List<MinionKeyword> KeywordIds { get; set; }
        public BattlegroundsInfo Battlegrounds { get; set; }

        public bool HasTaunt { get; set; }
        public bool HasReborn { get; set; }
        public bool HasDivineShield { get; set; }
        public bool HasWindfury { get; set; }

        [JsonIgnore] public AdditionalInfo AdditionalInfo { get; set; }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (KeywordIds == null) return;
            // TODO: Can be put into json.

            var tauntExceptions = new List<int> { 48100, 61028, 63623, 763, 1003, 43022, 63622, 63619, 48100 };
            HasTaunt = KeywordIds.Contains(MinionKeyword.Taunt) && !tauntExceptions.Contains(Id);

            var rebornExceptions = new List<int> { };
            HasReborn = KeywordIds.Contains(MinionKeyword.Reborn) && !rebornExceptions.Contains(Id);

            var divineShieldExceptions = new List<int> { 61072, 38740, 60629 };
            HasDivineShield = KeywordIds.Contains(MinionKeyword.DivineShield) && !divineShieldExceptions.Contains(Id);

            HasWindfury = KeywordIds.Contains(MinionKeyword.Windfury);
        }

        public override string ToString()
        {
            return $"{Name} {Attack}-{Health}";
        }

        public class BattlegroundsInfo
        {
            public int Tier { get; set; }
        }
    }    
}
