using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models
{
    [Table("service_event")]
    public class ServiceEvent
    {
        [Key] [Column("id")] 
        public int Id { get; set; }

        [Required] [Column("name")] 
        public string Name { get; set; }
        
        [Column("service_id")] 
        public int ServiceId { get; set; }  
        
        [ForeignKey(nameof(ServiceId))] 
        public virtual ServiceInfo ServiceInfo { get; set; }
    }
}