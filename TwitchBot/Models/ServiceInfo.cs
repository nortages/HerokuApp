using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models
{
    [Table("service")]
    public class ServiceInfo
    {
        [Key] [Column("id")] 
        public int Id { get; set; }

        [Required] [Column("name")] 
        public string Name { get; set; }
        
        [Column("description")] 
        public string Description { get; set; }
        
        [Required] [Column("type_path")] 
        public string TypePath { get; set; }

        [InverseProperty(nameof(ServiceEvent.ServiceInfo))]
        public virtual ICollection<ServiceEvent> ServiceEvents { get; set; }
    }
}