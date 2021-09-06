using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("service_callback")]
    public partial class ServiceCallback
    {
        [Key] [Column("id")]
        public int Id { get; set; }
        [Column("callback_id")]
        public string CallbackId { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }

        [ForeignKey(nameof(CallbackId))]
        public virtual CallbackInfo CallbackInfo { get; set; }
    }
}
