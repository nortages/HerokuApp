using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitchBot.Main
{
    [JsonObject]
    public class CallbackArgs
    {
        public Bot Bot { get; set; }
        public bool IsTestMode { get; set; }
        public string Lang { get; set; }
        public bool IsMentionRequired { get; set; }
        public JObject AdditionalData { get; set; }
    }
}