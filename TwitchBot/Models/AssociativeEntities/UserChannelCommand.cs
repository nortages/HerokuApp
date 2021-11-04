using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models.AssociativeEntities
{
    [Table("user_channel_command")]
    public partial class UserChannelCommand
    {
        [Key] [Column("id")] 
        public int Id { get; set; }
        
        [Column("user_id")] 
        public string UserId { get; set; }
        
        [Column("channel_command_id")] 
        public int ChannelCommandId { get; set; }
        
        [Column("amount")] 
        public int Amount { get; set; }
        
        [Column("last_option_id")] 
        public int? LastOptionId { get; set; }
        
        [Column("last_usage")]
        public DateTime? LastUsage { get; set; }

        [ForeignKey(nameof(ChannelCommandId))]
        public virtual ChannelCommand ChannelCommand { get; set; }
        
        [ForeignKey(nameof(LastOptionId))]  
        public virtual Option LastOption { get; set; }

        [ForeignKey(nameof(UserId))] 
        public virtual User User { get; set; }
    }

    public partial class UserChannelCommand
    {
        // Checks user cooldown.
        public bool IsAvailable()
        {
            var userCooldown = ChannelCommand.Command.UserCooldown ?? ChannelCommand.ChannelInfo.UserCooldown;
            if (DateTime.Now - LastUsage < TimeSpan.FromSeconds(userCooldown))
                return false;
            return true;
        }
    }
}