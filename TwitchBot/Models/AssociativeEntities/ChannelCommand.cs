using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using TwitchBot.Main;

#nullable disable

namespace TwitchBot.Models.AssociativeEntities
{
    [Table("channel_command")]
    public partial class ChannelCommand
    {
        [Key] [Column("id")]
        public int Id { get; set; }
        
        [Column("command_id")] 
        public int CommandId { get; set; }

        [Column("channel_id")]
        public int ChannelInfoId { get; set; }

        [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        
        [Column("last_usage")]
        public DateTime? LastUsage { get; set; }

        [ForeignKey(nameof(ChannelInfoId))] 
        public virtual ChannelInfo ChannelInfo { get; set; }

        [ForeignKey(nameof(CommandId))]
        public virtual Command Command { get; set; }
        
        public virtual ICollection<UserChannelCommand> UserChannelCommands { get; set; }
    }

    public partial class ChannelCommand
    {
        // Checks global cooldown.
        public bool IsAvailable()
        {
            var globalCooldown = Command.GlobalCooldown ?? ChannelInfo.GlobalCooldown;

            if (LastUsage != null &&
                DateTime.Now - LastUsage < TimeSpan.FromSeconds(globalCooldown))
                return false;
        
            LastUsage = DateTime.Now;
        
            return true;
        }
    }
}