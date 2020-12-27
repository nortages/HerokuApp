using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Interfaces;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Core.Models.Undocumented.RecentMessages;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.Entitlements.GetCodeStatus;
using TwitchLib.Api.Helix.Models.Subscriptions;
using TwitchLib.Api.Helix.Models.Users;
using TwitchLib.Api.ThirdParty.UsernameChange;
using TwitchLib.Api.V5.Models.Channels;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace HerokuApp
{
    public static class TwitchHelpers
    {
        private static readonly TwitchAPI twitchAPI = new TwitchAPI();

        static TwitchHelpers()
        {
            // TwitchAPI
            twitchAPI.Settings.ClientId = Config.BotClientId;
            twitchAPI.Settings.AccessToken = Config.BotAccessToken;
            twitchAPI.Settings.Secret = "Twitch"; // Need to not hard code this 
        }

        public static void SubscribeToStreamEvents(string url, string channelId, TimeSpan duration)
        {
            twitchAPI.Helix.Webhooks.StreamUpDownAsync(url, TwitchLib.Api.Core.Enums.WebhookCallMode.Subscribe, channelId, duration);
        }

        public static void RenewTwitchToken(string refreshToken, out string newAccessToken)
        {
            var fullUrl = $"https://twitchtokengenerator.com/api/refresh/{refreshToken}";
            var client = new RestClient(fullUrl);
            var request = new RestRequest();
            var response = client.Get(request).Content;
            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, string>>(response);
            newAccessToken = responseDict["token"];
        }

        public static bool IsSubscribeToChannel(string broadcasterId, string userId, string accessToken = null)
        {
            if (accessToken == null) accessToken = twitchAPI.Settings.AccessToken;
            var url = @"https://api.twitch.tv/helix/subscriptions";

            var client = new RestClient(url);
            var request = new RestRequest();
            request.AddParameter("broadcaster_id", broadcasterId);
            request.AddParameter("user_id", userId);
            request.AddHeader("Authorization", $"Bearer {accessToken}");
            request.AddHeader("Client-Id", Config.BotClientId);
            var response = client.Get(request).Content;

            dynamic parsedData = JObject.Parse(response);
            JArray jArray = parsedData.data;
            return jArray.Count != 0;
        }

        public static void SendMessageWithDelay(this TwitchClient client, JoinedChannel channel, string message, TimeSpan delay)
        {
            Task.Delay(delay).ContinueWith(t => client.SendMessage(channel, message));
        }

        public static void SendMessageWithDelay(this TwitchClient client, string channel, string message, TimeSpan delay)
        {
            Task.Delay(delay).ContinueWith(t => client.SendMessage(channel, message));
        }

        public static bool GetOnlineStatus(string channelId)
        {
            return twitchAPI.V5.Streams.BroadcasterOnlineAsync(channelId).Result;
        }

        public static Subscription[] GetSubscribers(string channelId)
        {
            return twitchAPI.Helix.Subscriptions.GetBroadcasterSubscriptions(channelId, Config.BotClientId).Result.Data;
        }

        public static TimeSpan? GetUpTime()
        {
            string userId = GetIdByUsername(Config.ChannelName);

            if (userId == null || string.IsNullOrEmpty(userId))
                return null;
            return twitchAPI.V5.Streams.GetUptimeAsync(userId).Result;
        }

        public static string GetIdByUsername(string userName)
        {
            List<string> list = new List<string>() { userName };
            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].Id;
        }

        public static string GetUsernameById(string id)
        {
            List<string> list = new List<string>() { id };
            User[] users = twitchAPI.Helix.Users.GetUsersAsync(list, null).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].DisplayName;
        }

        public static User GetUser(string userName)
        {
            if (userName == string.Empty)
                return null;

            List<string> list = new List<string>() { userName };
            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0];
        }

        public static User[] GetUsersAsync(List<string> userNames)
        {
            if (userNames.Count == 0)
                return null;

            User[] users = twitchAPI.Helix.Users.GetUsersAsync(null, userNames).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users;
        }

        public static Channel GetChannel(string userName)
        {
            string userId = GetIdByUsername(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                Channel channel = twitchAPI.V5.Channels.GetChannelByIDAsync(userId).Result;
                if (channel != null)
                    return channel;
            }

            return null;
        }

        public static User[] GetChanneSubscribers(string userName)
        {
            string userId = GetIdByUsername(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                var subs = twitchAPI.Helix.Subscriptions.GetBroadcasterSubscriptions(userId).Result;
                var userNames = new List<string>();
                foreach (var sub in subs.Data)
                {
                    userNames.Add(sub.UserName);
                }
                return GetUsersAsync(userNames);
            }

            return null;
        }

        public static List<UsernameChangeListing> GetUsernameChangesAsync(string userName)
        {
            return twitchAPI.ThirdParty.UsernameChange.GetUsernameChangesAsync(userName).Result;
        }

        public static List<ChatterFormatted> GetChatters(string channelName)
        {
            return twitchAPI.Undocumented.GetChattersAsync(channelName).Result;
        }
    }
}