using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models.AssociativeEntities
{
    [Table("channel_mini_game")]
    public class ChannelMiniGame
    {
        [Key] [Column("id")] public int Id { get; set; }

        [Column("channel_id")] public int ChannelId { get; set; }

        [Column("mini_game_id")] public int MiniGameId { get; set; }

        [Column("is_enabled")] public bool IsEnabled { get; set; }

        [ForeignKey(nameof(ChannelId))] public virtual ChannelInfo ChannelInfo { get; set; }

        [ForeignKey(nameof(MiniGameId))] public virtual MiniGame MiniGame { get; set; }
    }
}