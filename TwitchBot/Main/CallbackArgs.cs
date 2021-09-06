using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchBot.Models;

namespace TwitchBot.Main
{
    public class CallbackArgs
    {
        public ChannelBot ChannelBot { get; set; }
        public ChannelBotInfo ChannelBotInfo { get; set; }
        public NortagesTwitchBotContext DbContext { get; set; }
        public Option Option { get; set; }
        public bool? IsMentionRequired { get; set; }
    }
}