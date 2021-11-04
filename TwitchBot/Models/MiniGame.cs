using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TwitchBot.Models.AssociativeEntities;

namespace TwitchBot.Models
{
    [Table("mini_game")]
    public class MiniGame
    {
        [Key] [Column("id")] 
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("is_enabled")] 
        public bool IsEnabled { get; set; }

        public virtual ICollection<ServiceEventCallback> ServiceEventCallbacks { get; set; }
        public virtual ICollection<Command> MiniGameCommands { get; set; }
        public virtual ICollection<RewardRedemption> MiniGameRewardRedemptions { get; set; }
    }
}