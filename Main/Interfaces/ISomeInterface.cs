using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HerokuApp.Main.Interfaces
{
    interface ISomeInterface
    {
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

    }
}
