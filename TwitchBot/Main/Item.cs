using Newtonsoft.Json;

namespace TwitchBot.Main
{
    [JsonObject]
    public class Item
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }
    }
}