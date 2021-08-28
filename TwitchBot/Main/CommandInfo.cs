using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchBot.Main.ExtensionsMethods;
using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;
using JsonNetConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace TwitchBot.Main
{
    public class EventCallbackInfo : BaseCallbackInfo
    {
        public string EventName { get; set; }
        public string ListenMethodName { get; set; }
        
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)] public override bool HasCallback { get; set; }
    }
    
    public class CommandInfo : OptionInfo
    {
        public List<string> Names { get; set; }
        public string Description { get; set; }
        public int? UserCooldown { get; set; }
        public int? GlobalCooldown { get; set; }
        [JsonProperty] private bool IsSaveUsage { get; set; }

        [NewtonsoftJsonIgnore] public readonly Dictionary<string, OptionInfo> LastOption = new();
        [NewtonsoftJsonIgnore] public DateTime LastUsage;
        [NewtonsoftJsonIgnore] public readonly Dictionary<string, DateTime> UsersToLastUsageDatetime = new();
        
        [OnDeserialized]
        internal new void OnDeserializedMethod(StreamingContext context)
        {
            base.OnDeserializedMethod(context);
            
            if (!IsEnabled) return;
            if (!IsSaveUsage) return;
            var usageFrequencyJProperty = Config.CommandsData.SelectToken($"$.commands_usage.{Id}").GetJProperty();

            var value = usageFrequencyJProperty.Value;
            if (value.Type != JTokenType.Object) return;
            UsageFrequency = (value as JObject)?.ToObject<Dictionary<string, int>>();
            Config.OnConfigSaves += delegate
            {
                if (UsageFrequency == null) return;
                Console.WriteLine($"Saving {Id} usage");
                usageFrequencyJProperty.Value = JObject.FromObject(UsageFrequency);
            };
        }

        public void IncreaseUsageFrequency(string username)
        {
            if (!UsageFrequency.ContainsKey(username))
            {
                UsageFrequency.Add(username, default);
            }
            UsageFrequency[username]++;
        }
        
        public void UpdateLastUsage(string username)
        {
            UsersToLastUsageDatetime[username] = DateTime.Now;
        }
    }
}
