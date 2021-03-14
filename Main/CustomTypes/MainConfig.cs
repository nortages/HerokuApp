using System.Collections.Generic;
using Newtonsoft.Json;

namespace HerokuApp
{
    public class MainConfig
    {
        [JsonProperty("bots")]
        public List<Bot> BotsInfo { get; set; }

        [JsonProperty("general_commands")]
        public List<Command> GeneralCommands { get; set; }

        [JsonProperty("service_commands")]
        public List<Command> ServiceCommands { get; set; }

        [JsonProperty("general_message_commands")]
        public List<MessageCommand> GeneralMessageCommands { get; set; }
    }
}
