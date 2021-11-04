using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models.AssociativeEntities
{
    [Table("channel_reward_redemption")]
    public class ChannelRewardRedemption
    {
        [Key] [Column("id")] public int Id { get; set; }

        [Column("is_enabled")] public bool IsEnabled { get; set; }

        [Required]
        [Column("reward_redemption_id")]
        public int RewardRedemptionId { get; set; }

        [Required] [Column("channel_id")] public int ChannelId { get; set; }

        [ForeignKey(nameof(ChannelId))] public virtual ChannelInfo ChannelInfo { get; set; }

        [ForeignKey(nameof(RewardRedemptionId))]
        public virtual RewardRedemption RewardRedemption { get; set; }
    }
}