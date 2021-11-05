using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TwitchBot.Main;
using TwitchBot.Models.AssociativeEntities;
using TwitchLib.Client.Events;

#nullable disable

namespace TwitchBot.Models
{
    [Table("message_command")]
    public partial class MessageCommand
    {
        [Key] [Column("option_id")]
        public int OptionId { get; set; }

        [Required] [Column("regex")] 
        public string Regex { get; set; }

        [ForeignKey(nameof(OptionId))]
        public virtual Option Option { get; set; }

        public virtual ICollection<ChannelMessageCommand> ChannelMessageCommands { get; set; }
    }

    public partial class MessageCommand
    {
        public string GetAnswer(
            OnMessageReceivedArgs e,
            MessageCommandCallbackArgs args)
        {
            var channelBot = args.ChannelBot;
            var isMentionRequired = Option.IsMentionRequired;

            args.IsMentionRequired = isMentionRequired;
            var answer = Option.GetAnswer(this, e, args);
            if (isMentionRequired is true) answer = $"@{e.ChatMessage.DisplayName}, {answer}";

            const string randChatterVariable = "${random.chatter}";
            if (answer.Contains(randChatterVariable))
                answer = answer.Replace(randChatterVariable,
                    channelBot.ChannelTwitchHelpers.GetRandChatter(e.ChatMessage.Channel).Username);
            answer = answer.Replace("${sender}", e.ChatMessage.DisplayName);

            return answer;
        }
    }
}