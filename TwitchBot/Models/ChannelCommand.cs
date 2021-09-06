using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("channel_command")]
    public partial class ChannelCommand
    {
        [Key] [Column("command_id")]
        public int CommandId { get; set; }
        [Key] [Column("channel_id")]
        public int ChannelId { get; set; }
        [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        [ForeignKey(nameof(ChannelId))]
        public virtual ChannelBotInfo ChannelBotInfo { get; set; }
        [ForeignKey(nameof(CommandId))]
        public virtual Command Command { get; set; }
    }
    
    public partial class ChannelCommand
    {
        [NotMapped]
        public DateTime LastUsage;
        [NotMapped]
        public readonly Dictionary<string, Option> LastOption = new();
        [NotMapped]
        public readonly Dictionary<string, DateTime> UsersToLastUsageDatetime = new();
        
        public void UpdateLastUsage(string username)
        {
            UsersToLastUsageDatetime[username] = DateTime.Now;
        }
        
        public bool IsCommandAvailable(string username)
        {
            var userCooldown = Command.UserCooldown ?? ChannelBotInfo.UserCooldown;
            var globalCooldown = Command.GlobalCooldown ?? ChannelBotInfo.GlobalCooldown;

            // Checks user cooldown.
            if (UsersToLastUsageDatetime.ContainsKey(username) &&
                DateTime.Now - UsersToLastUsageDatetime[username] < TimeSpan.FromSeconds(userCooldown))
            {
                return false;
            }

            // Checks global cooldown.
            if (LastUsage != default &&
                DateTime.Now - LastUsage < TimeSpan.FromSeconds(globalCooldown))
            {
                return false;
            }

            LastUsage = DateTime.Now;

            return true;
        }
    }
}
