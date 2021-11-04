using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TwitchBot.Main;
using TwitchBot.Models.AssociativeEntities;
using TwitchLib.Client.Events;

#nullable disable

namespace TwitchBot.Models
{
    [Table("command")]
    public partial class Command
    {
        [Key] [Column("id")] 
        public int Id { get; set; }

        [Column("option_id")] 
        public int OptionId { get; set; }

        [Required] [Column("names")] 
        public string[] Names { get; set; }

        [Column("description")] 
        public string Description { get; set; }

        [Column("user_cooldown")] 
        public int? UserCooldown { get; set; }

        [Column("global_cooldown")] 
        public int? GlobalCooldown { get; set; }
        
        [Column("mini_game_id")] 
        public int? MiniGameId { get; set; }

        [Required] [Column("is_private")]
        public bool IsPrivate { get; set; }

        [ForeignKey(nameof(OptionId))]
        public virtual Option Option { get; set; }
        
        [ForeignKey(nameof(MiniGameId))]
        public virtual MiniGame MiniGame { get; set; }

        [InverseProperty(nameof(ChannelCommand.Command))]
        public virtual ICollection<ChannelCommand> ChannelCommands { get; set; }
    }

    public partial class Command
    {
        public string GetAnswer(OnChatCommandReceivedArgs e,
            CallbackArgs args)
        {
            var channelBot = args.ChannelBot;
            var channelBotInfo = args.ChannelInfo;
            var isMentionRequired = Option.IsMentionRequired;
            var username = e.Command.ChatMessage.Username;
            var displayName = e.Command.ChatMessage.DisplayName;

            args.IsMentionRequired = isMentionRequired;
            args.Command = this;
            var answer = Option.GetAnswer(this, e, args);
            if (answer is null)
                return null;

            if (args.IsMentionRequired is true)
                answer = $"@{displayName}, {answer}";

            const string randChatterVariable = "${random.chatter}";
            if (answer.Contains(randChatterVariable))
            {
                var randChatter = BotService.BotTwitchHelpers.GetRandChatter(e.Command.ChatMessage.Channel);
                answer = answer.Replace(randChatterVariable, randChatter == null ? displayName : randChatter.Username);
            }
            answer = answer.Replace("${sender}", displayName);

            return answer;
        }
    }
}