using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TwitchBot.Main.Enums;
using TwitchBot.Models.AssociativeEntities;

#nullable disable

namespace TwitchBot.Models
{
    [Table("channel")]
    public class ChannelInfo
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
        public int? ChannelCredentialsId { get; set; }
        
        [Required] [Column("user_cooldown")] 
        public int UserCooldown { get; set; }
        
        [Required] [Column("global_cooldown")] 
        public int GlobalCooldown { get; set; }
        
        [Required] [Column("lang")] 
        public Lang Lang { get; set; }
        
        [Required] [Column("is_test_mode")] 
        public bool IsTestMode { get; set; }
        
        [Required] [Column("channel_username")] 
        public string ChannelUsername { get; set; }

        [ForeignKey(nameof(BotCredentialsId))] 
        public virtual Credentials BotCredentials { get; set; }
        
        [ForeignKey(nameof(ChannelCredentialsId))]
        public virtual Credentials ChannelCredentials { get; set; }

        [InverseProperty(nameof(ChannelService.ChannelInfo))]
        public virtual ICollection<ChannelService> ChannelServices { get; set; }
        
        [InverseProperty(nameof(ChannelServiceEventCallback.ChannelInfo))]
        public virtual ICollection<ChannelServiceEventCallback> ChannelServiceEventCallbacks { get; set; }
        
        [InverseProperty(nameof(ChannelCommand.ChannelInfo))]
        public virtual ICollection<ChannelCommand> ChannelCommands { get; set; }
        
        [InverseProperty(nameof(ChannelMessageCommand.ChannelInfo))]
        public virtual ICollection<ChannelMessageCommand> ChannelMessageCommands { get; set; }
        
        [InverseProperty(nameof(ChannelRewardRedemption.ChannelInfo))]
        public virtual ICollection<ChannelRewardRedemption> ChannelRewardRedemptions { get; set; }
        
        [InverseProperty(nameof(ChannelMiniGame.ChannelInfo))]
        public virtual ICollection<ChannelMiniGame> ChannelMiniGames { get; set; }
    }
}