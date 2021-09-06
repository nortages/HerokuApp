using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("donation_alerts")]
    public partial class DonationAlertInfo
    {
        [Key] [Column("channel_id")]
        public int ChannelBotId { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        [Required] [Column("access_token")]
        public string AccessToken { get; set; }
        [Column("on_donation_received")]
        public int? OnDonationReceivedServiceCallbackId { get; set; }
        [Column("on_donation_goal_update_received")]
        public int? OnDonationGoalUpdateReceivedServiceCallbackId { get; set; }

        [ForeignKey(nameof(ChannelBotId))]
        public virtual ChannelBotInfo ChannelBotInfo { get; set; }
        [ForeignKey(nameof(OnDonationReceivedServiceCallbackId))]
        public virtual ServiceCallback OnDonationReceivedServiceCallback { get; set; }
        [ForeignKey(nameof(OnDonationGoalUpdateReceivedServiceCallbackId))]
        public virtual ServiceCallback OnDonationGoalUpdateReceivedServiceCallback { get; set; }
    }
}
