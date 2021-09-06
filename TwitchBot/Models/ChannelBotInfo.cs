using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using Fasterflect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwitchBot.Main;
using TwitchBot.Main.DonationAlerts;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchBot.Main.DonationAlerts;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

#nullable disable

namespace TwitchBot.Models
{
    [Table("channel")]
    public class ChannelBotInfo
    {
        [Key] [Column("id")]
        public int Id { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        [Required] [Column("channel_user_id")]
        public string ChannelUserId { get; set; }
        [Required] [Column("bot_credentials_id")]
        public int BotCredentialsId { get; set; }
        [Column("streamer_credentials_id")]
        public int? StreamerCredentialsId { get; set; }
        [Required] [Column("user_cooldown")]
        public int UserCooldown { get; set; }
        [Required] [Column("global_cooldown")]
        public int GlobalCooldown { get; set; }
        [Required] [Column("lang")]
        public Lang Lang { get; set; }
        [Required] [Column("is_test_mode")]
        public bool IsTestMode { get; set; }
        [Column("channel_username")]
        public string ChannelUsername { get; set; }

        [ForeignKey(nameof(BotCredentialsId))]
        public virtual Credentials BotCredentials { get; set; }
        [ForeignKey(nameof(StreamerCredentialsId))]
        public virtual Credentials ChannelCredentials { get; set; }
        public virtual DonationAlertInfo DonationAlertsInfo { get; set; }
        public virtual PubsubInfo PubSubInfo { get; set; }
        public virtual TwitchClientInfo TwitchClientInfo { get; set; }
        [InverseProperty(nameof(ChannelCommand.ChannelBotInfo))]
        public virtual ICollection<ChannelCommand> ChannelCommands { get; set; }
        [InverseProperty(nameof(ChannelMessageCommand.ChannelBotInfo))]
        public virtual ICollection<ChannelMessageCommand> ChannelMessageCommands { get; set; }
        [InverseProperty(nameof(RewardRedemption.ChannelBotInfo))]
        public virtual ICollection<RewardRedemption> RewardRedemptions { get; set; }
    }
}