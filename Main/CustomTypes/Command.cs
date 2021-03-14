using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using NewtonsoftJsonIgnore = Newtonsoft.Json.JsonIgnoreAttribute;

namespace HerokuApp
{
    public class Command : Option
    {
        [JsonProperty("names")]
        public List<string> Names { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("user_cooldown")]
        public int? UserCooldown { get; set; }

        [JsonProperty("global_cooldown")]
        public int? GlobalCooldown { get; set; }

        [JsonProperty("save_usage", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(false)]
        private bool IsSaveUsage { get; set; }

        [JsonProperty("is_mention_required", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(true)]
        public bool IsMentionRequired { get; set; }

        [NewtonsoftJsonIgnore]
        public Dictionary<string, ProbabilityOptionInfo> lastOption = new Dictionary<string, ProbabilityOptionInfo>();

        [NewtonsoftJsonIgnore]
        public DateTime lastUsage;

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (!IsEnabled) return;

            if (IsSaveUsage)
            {
                var usageProperty = Config.GetJPropertyFromCommandsInfo($"commands_usage.{Id}");
                usageFrequency = usageProperty.Value.ToObject<Dictionary<string, int>>();
                Config.OnConfigSaves += delegate
                {
                    usageProperty.Value = JObject.FromObject(usageFrequency);
                }; 
            }
        }

        public void IncreaseUsageFrequency(string username)
        {
            if (!usageFrequency.ContainsKey(username))
            {
                usageFrequency.Add(username, default);
            }
            usageFrequency[username]++;
        }
    }
}
