using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models
{
    [Table("service_event_callback")]
    public class ServiceEventCallback
    {
        [Key] [Column("id")] 
        public int Id { get; set; }
        
        [Column("service_event_id")] 
        public int ServiceEventId { get; set; }
        
        [Column("callback_id")] 
        public string CallbackId { get; set; }
        
        [Column("mini_game_id")] 
        public int? MiniGameId { get; set; }
        
        [ForeignKey(nameof(ServiceEventId))]    
        public virtual ServiceEvent ServiceEvent { get; set; }
        
        [ForeignKey(nameof(CallbackId))] 
        public virtual CallbackInfo CallbackInfo { get; set; }
        
        [ForeignKey(nameof(MiniGameId))]
        public virtual MiniGame MiniGame { get; set; }
    }
}