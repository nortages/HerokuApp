using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("pubsub")]
    public partial class PubsubInfo
    {
        [Key] [Column("channel_id")]
        public int ChannelId { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        [Column("on_reward_redeemed")]
        public int? OnRewardRedeemedServiceCallbackId { get; set; }

        [ForeignKey(nameof(ChannelId))]
        public virtual ChannelBotInfo ChannelBotInfo { get; set; }
        [ForeignKey(nameof(OnRewardRedeemedServiceCallbackId))]
        public virtual ServiceCallback OnRewardRedeemedServiceCallback { get; set; }
    }
}
