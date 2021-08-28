using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchLib.Client;
using TwitchLib.Client.Extensions;
using TwitchLib.PubSub;

namespace TwitchBot.Main.ExtensionsMethods
{
    public static class TwitchClientExtensions
    {
        public static void TimeoutModer(this TwitchClient twitchClient, string channel, string username, TimeSpan timeoutTime, string reason = "")
        {
            twitchClient.TimeoutUser(channel, username, timeoutTime, reason);
            Task.Delay(timeoutTime.Add(TimeSpan.FromSeconds(1))).ContinueWith(t => twitchClient.SendMessage(channel, $"/mod {username}"));
        }

        public static void SendMessageWithDelay(this TwitchClient client, string channel, string message, TimeSpan delay)
        {
            Task.Delay(delay).ContinueWith(t => client.SendMessage(channel, message));
        }
    }
}
