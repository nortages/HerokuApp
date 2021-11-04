using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TwitchBot.Models.AssociativeEntities
{
    [Table("channel_service_event_callback")]
    public class ChannelServiceEventCallback
    {
        [Key] [Column("id")] 
        public int Id { get; set; }

        [Column("channel_id")] 
        public int ChannelId { get; set; }

        [Column("service_event_callback_id")] 
        public int ServiceEventCallbackId { get; set; }

        [Column("is_enabled")] 
        public bool IsEnabled { get; set; }

        [ForeignKey(nameof(ChannelId))] 
        public virtual ChannelInfo ChannelInfo { get; set; }

        [ForeignKey(nameof(ServiceEventCallbackId))]
        public virtual ServiceEventCallback ServiceEventCallback { get; set; }
    }
}