using Newtonsoft.Json;

namespace HerokuApp
{
    public class ProbabilityOptionInfo : Option
    {
        [JsonProperty("probability")]
        public double? Probability { get; set; }
    }
}
