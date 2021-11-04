using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("reward_redemption")]
    public class RewardRedemption
    {
        [Key] [Column("id")]
        public int Id { get; set; }

        [Required] [Column("title")]
        public string Title { get; set; }

        [Required] [Column("callback_id")]
        public string CallbackId { get; set; }
        
        [Column("mini_game_id")] 
        public int? MiniGameId { get; set; }
        
        [ForeignKey(nameof(MiniGameId))]
        public virtual MiniGame MiniGame { get; set; }

        [ForeignKey(nameof(CallbackId))]
        public virtual CallbackInfo CallbackInfo { get; set; }
    }
}