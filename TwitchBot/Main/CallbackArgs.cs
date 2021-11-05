using Microsoft.Extensions.Logging;
using TwitchBot.Models;
using TwitchBot.Models.AssociativeEntities;

namespace TwitchBot.Main
{
    public class CallbackArgs
    {
        public ILogger Logger { get; set; }
        public ChannelBot ChannelBot { get; set; }
        public ChannelInfo ChannelInfo { get; set; }
        public NortagesTwitchBotDbContext DbContext { get; set; }
    }
    
    public class OptionCallbackArgs : CallbackArgs
    {
        public Option Option { get; set; }
        public bool? IsMentionRequired { get; set; }
        public object CallMethodTarget { get; set; }
    }
    
    public class CommandCallbackArgs : OptionCallbackArgs
    {
        public Command Command { get; set; }
        public UserChannelCommand UserChannelCommand { get; set; }
    }
    
    public class MessageCommandCallbackArgs : OptionCallbackArgs
    {
        public MessageCommand MessageCommand { get; set; }
    }
}