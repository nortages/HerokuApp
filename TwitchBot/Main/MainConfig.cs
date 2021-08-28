using System.Collections.Generic;
using Newtonsoft.Json;

namespace TwitchBot.Main
{
    [JsonObject]
    public class MainConfig
    {
        public List<Bot> BotsInfo { get; set; }
        public List<CommandInfo> GeneralCommands { get; set; }
        public List<CommandInfo> ServiceCommands { get; set; }
        public List<MessageCommandInfo> GeneralMessageCommands { get; set; }
    }
}
