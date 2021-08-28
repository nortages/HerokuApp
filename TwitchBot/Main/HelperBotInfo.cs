using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TwitchBot.Main
{
    [JsonObject]
    public class HelperBotInfo
    {
        public bool IsEnabled { get; set; }
        public bool IsHelperBot { get; set; }
        public string ChannelName { get; set; }
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public TwitchClientInfo TwitchClientInfo { get; set; }
        
        public Bot ConvertToBot()
        {
            var infoStr = JsonConvert.SerializeObject(this);
            var helperBot = JObject.Parse(infoStr).ToObject<Bot>();
            if (helperBot == null)
                throw new InvalidOperationException();
            helperBot.ChannelName = ChannelName;
            return helperBot;
        }
    }
}