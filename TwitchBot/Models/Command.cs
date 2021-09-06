using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TwitchBot.Main;
using TwitchLib.Client.Events;

#nullable disable

namespace TwitchBot.Models
{
    [Table("command")]
    public partial class Command
    {
        [Key] [Column("option_id")]
        public int OptionId { get; set; }
        [Required] [Column("names")]
        public string[] Names { get; set; }
        [Column("description")]
        public string Description { get; set; }
        [Column("user_cooldown")]
        public int? UserCooldown { get; set; }
        [Column("global_cooldown")]
        public int? GlobalCooldown { get; set; }
        [Required] [Column("is_private")]
        public bool IsPrivate { get; set; }

        [ForeignKey(nameof(OptionId))]
        public virtual Option Option { get; set; }
        public virtual ChannelCommand ChannelCommand { get; set; }
    }
    
    public partial class Command
    {
        public string GetAnswer(
            OnChatCommandReceivedArgs e,
            CallbackArgs args)
        {
            var channelBot = args.ChannelBot;
            var channelBotInfo = args.ChannelBotInfo;
            var isMentionRequired = Option.IsMentionRequired;
            var username = e.Command.ChatMessage.Username;

            if (!channelBotInfo.IsTestMode)
            {
                if (!ChannelCommand.IsCommandAvailable(username)) return null;
                Option.IncreaseUsageFrequency(username);
                ChannelCommand.UpdateLastUsage(username);
            }

            args.IsMentionRequired = isMentionRequired;
            var answer = Option.GetAnswer(this, e, args);
            if (args.IsMentionRequired is true) answer = $"@{e.Command.ChatMessage.DisplayName}, {answer}";

            const string randChatterVariable = "${random.chatter}";
            if (answer.Contains(randChatterVariable))
            {
                answer = answer.Replace(randChatterVariable, channelBot.ChannelTwitchHelpers.GetRandChatter(e.Command.ChatMessage.Channel).Username);
            }
            answer = answer.Replace("${sender}", e.Command.ChatMessage.DisplayName);
            
            return answer;
        }
    }
}
