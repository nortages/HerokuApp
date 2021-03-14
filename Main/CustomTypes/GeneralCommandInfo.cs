using Newtonsoft.Json;

namespace HerokuApp
{
    public class GeneralCommandInfo
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }
    }
}
