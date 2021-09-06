using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("channel_message_command")]
    public partial class ChannelMessageCommand
    {
        [Key] [Column("message_command_id")]
        public int MessageCommandId { get; set; }
        [Key] [Column("channel_id")]
        public int ChannelId { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        [ForeignKey(nameof(ChannelId))]
        public virtual ChannelBotInfo ChannelBotInfo { get; set; }
        [ForeignKey(nameof(MessageCommandId))]
        public virtual MessageCommand MessageCommand { get; set; }
    }
}
