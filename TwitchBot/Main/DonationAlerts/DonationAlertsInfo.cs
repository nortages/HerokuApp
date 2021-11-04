using System.Collections.Generic;
using Newtonsoft.Json;
using NewtonsoftJsonConverterAttribute = Newtonsoft.Json.JsonConverterAttribute;

namespace TwitchBot.Main.DonationAlerts
{
    [JsonObject]
    public class DonationAlertsInfo
    {
        public bool IsEnabled;
        public string AccessToken;
        public List<EventCallbackInfo> EventsCallbacks = new();
        
        // [NewtonsoftJsonConverter(typeof(EventNameToCallbackConverter<OnDonationAlertArgs>))]
        // public EventCallback<OnDonationAlertArgs> OnDonationAlertCallback;
    }
}