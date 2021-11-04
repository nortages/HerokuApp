using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models.AssociativeEntities
{
    [Table("channel_service")]
    public class ChannelService
    {
        [Key] [Column("id")] 
        public int Id { get; set; }

        [Column("channel_id")] 
        public int ChannelId { get; set; }

        [Column("service_id")] 
        public int ServiceId { get; set; }
        
        [Column("credentials_id")] 
        public int CredentialsId { get; set; }

        [ForeignKey(nameof(ChannelId))] 
        public virtual ChannelInfo ChannelInfo { get; set; }

        [ForeignKey(nameof(ServiceId))]
        public virtual ServiceInfo Service { get; set; }
        
        [ForeignKey(nameof(CredentialsId))]
        public virtual Credentials Credentials { get; set; }
    }
}