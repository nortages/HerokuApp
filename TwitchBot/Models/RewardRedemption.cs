using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace TwitchBot.Models
{
    [Table("reward_redemption")]
    public partial class RewardRedemption
    {
        [Key] [Column("id")]
        public int Id { get; set; }
        [Required] [Column("title")]
        public string Title { get; set; }
        [Required] [Column("callback_id")]
        public string CallbackId { get; set; }
        [Column("channel_id")]
        public int ChannelId { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        
        [ForeignKey(nameof(CallbackId))]
        public virtual CallbackInfo CallbackInfo { get; set; }
        [ForeignKey(nameof(ChannelId))]
        public virtual ChannelBotInfo ChannelBotInfo { get; set; }
    }
}
