using System.Collections.Generic;
using Newtonsoft.Json;
using TwitchBot.Main.Converters;
using TwitchLib.Client.Events;
using NewtonsoftJsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace TwitchBot.Main
{
    [JsonObject]
    public class TwitchClientInfo
    {
        public bool IsEnabled;

        public List<EventCallbackInfo> EventsCallbacks = new();

        // [NewtonsoftJsonConverter(typeof(EventNameToCallbackConverter<OnWhisperReceivedArgs>))]
        // public BaseCallbackInfo OnWhisperReceivedCallback;
        //
        // [NewtonsoftJsonConverter(typeof(EventNameToCallbackConverter<OnUserTimedoutArgs>))]
        // public EventCallback<OnUserTimedoutArgs> OnUserTimedoutCallback;
        //
        // [NewtonsoftJsonConverter(typeof(EventNameToCallbackConverter<OnMessageReceivedArgs>))]
        // public EventCallback<OnMessageReceivedArgs> AdditionalOnMessageReceivedCallback;
        //
        // [NewtonsoftJsonConverter(typeof(EventNameToCallbackConverter<OnNewSubscriberArgs>))]
        // public EventCallback<OnNewSubscriberArgs> OnNewSubscriberCallback;
        //
        // [NewtonsoftJsonConverter(typeof(EventNameToCallbackConverter<OnGiftedSubscriptionArgs>))]
        // public EventCallback<OnGiftedSubscriptionArgs> OnGiftedSubscriptionCallback;
    }
}
