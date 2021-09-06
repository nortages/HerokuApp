using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

#nullable disable

namespace TwitchBot.Models
{
    [Table("twitch_client")]
    public partial class TwitchClientInfo
    {
        [Key] [Column("channel_id")]
        public int ChannelId { get; set; }
        [Required] [Column("is_enabled")]
        public bool IsEnabled { get; set; }
        [Column("on_message_received")]
        public int? OnMessageReceivedServiceCallbackId { get; set; }
        [Column("on_user_timedout")]
        public int? OnUserTimedoutServiceCallbackId { get; set; }
        [Column("on_new_subscriber")]
        public int? OnNewSubscriberServiceCallbackId { get; set; }
        [Column("on_gifted_subscription")]
        public int? OnGiftedSubscriptionServiceCallbackId { get; set; }
        [Column("on_whisper_received")]
        public int? OnWhisperReceivedServiceCallbackId { get; set; }
        [Column("on_resubscriber")]
        public int? OnReSubscriberServiceCallbackId { get; set; }
        [Column("on_chat_command_received")]
        public int? OnChatCommandReceivedServiceCallbackId { get; set; }

        [ForeignKey(nameof(ChannelId))]
        public virtual ChannelBotInfo ChannelBotInfo { get; set; }
        [ForeignKey(nameof(OnChatCommandReceivedServiceCallbackId))]
        public virtual ServiceCallback OnChatCommandReceivedServiceCallback { get; set; }
        [ForeignKey(nameof(OnGiftedSubscriptionServiceCallbackId))]
        public virtual ServiceCallback OnGiftedSubscriptionServiceCallback { get; set; }
        [ForeignKey(nameof(OnMessageReceivedServiceCallbackId))]
        public virtual ServiceCallback OnMessageReceivedServiceCallback { get; set; }
        [ForeignKey(nameof(OnNewSubscriberServiceCallbackId))]
        public virtual ServiceCallback OnNewSubscriberServiceCallback { get; set; }
        [ForeignKey(nameof(OnReSubscriberServiceCallbackId))]
        public virtual ServiceCallback OnReSubscriberServiceCallback { get; set; }
        [ForeignKey(nameof(OnUserTimedoutServiceCallbackId))]
        public virtual ServiceCallback OnUserTimedoutServiceCallback { get; set; }
        [ForeignKey(nameof(OnWhisperReceivedServiceCallbackId))]
        public virtual ServiceCallback OnWhisperReceivedServiceCallback { get; set; }
    }
}
