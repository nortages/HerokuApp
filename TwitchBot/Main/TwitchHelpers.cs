using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Helix.Models.Videos.GetVideos;
using TwitchLib.Api.ThirdParty.UsernameChange;
using TwitchLib.Api.V5.Models.Channels;

namespace TwitchBot.Main
{
    public static class TwitchHelpers
    {
        private static readonly TwitchAPI TwitchApi = new();

        static TwitchHelpers()
        {
            // TwitchAPI
            TwitchApi.Settings.ClientId = Config.BotClientId;
            TwitchApi.Settings.AccessToken = Config.GetTwitchAccessToken();
            TwitchApi.Settings.Secret = "Twitch"; // Need to not hard code this
        }
        
        public static void SubscribeToStreamEvents(string url, string channelId, TimeSpan duration)
        {
            TwitchApi.Helix.Webhooks.StreamUpDownAsync(url, TwitchLib.Api.Core.Enums.WebhookCallMode.Subscribe, channelId, duration);
        }

        public static bool IsSubscribeToChannel(string broadcasterId, string userId, string accessToken)
        {
            return TwitchApi.Helix.Subscriptions.GetUserSubscriptionsAsync(broadcasterId, new List<string> { userId }, accessToken)
                .Result.Data.Length != 0;
        }

        public static async Task<bool> IsUserTimedOut(string broadcasterId, string userId, string accessToken = null)
        {
            try
            {
                var result = await TwitchApi.Helix.Moderation.GetBannedUsersAsync(broadcasterId, new List<string> { userId }, accessToken: accessToken);
                return result.Data.Length != 0;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static bool IsUserModerator(string broadcasterId, string userId, string accessToken = null)
        {
            var result = TwitchApi.Helix.Moderation.GetModeratorsAsync(broadcasterId, new List<string> { userId }, accessToken: accessToken).Result;
            return result != null && result.Data != null && result.Data.Length != 0;
        }

        public static void FulfillRedemption(string broadcasterId, string rewardId, string redemptionId)
        {
            var request = new UpdateCustomRewardRedemptionStatusRequest
            {
                Status = TwitchLib.Api.Core.Enums.CustomRewardRedemptionStatus.FULFILLED
            };
            TwitchApi.Helix.ChannelPoints.UpdateCustomRewardRedemptionStatus(broadcasterId, rewardId, new List<string> { redemptionId}, request);
        }

        public static bool GetOnlineStatus(string channelId)
        {
            return TwitchApi.V5.Streams.BroadcasterOnlineAsync(channelId).Result;
        }

        public static bool IsStreamUp(string channelId)
        {
            var streams = TwitchApi.Helix.Streams.GetStreamsAsync(userIds: new List<string> { channelId }).Result.Streams;
            return streams.Length != 0;
        }

        public static TimeSpan? GetLastVideoDate(string channelId)
        {
            var result = TwitchApi.Helix.Videos.GetVideoAsync(userId: channelId, first: 1).Result;
            var lastVideo = result.Videos.FirstOrDefault();
            if (lastVideo != null)
            {
                var durationStr = lastVideo.Duration;
                TimeSpan duration = GetTimeSpanFromTwitchDuration(durationStr);
                return DateTime.Now - DateTime.Parse(lastVideo.CreatedAt).Add(duration);
            }
            else
            {
                return null;
            }
        }

        public static TimeSpan GetTimeSpanFromTwitchDuration(string durationStr)
        {
            var regex = new Regex(@"(\d+h)?(\d+m)?(\d+s)?");
            var match = regex.Match(durationStr);
            var reversedGroups = match.Groups.Values.Reverse().ToList();
            var values = new int[] { 0, 0, 0 };
            for (int i = 0; i < 3; i++)
            {
                if (string.IsNullOrEmpty(reversedGroups[i].Value)) continue;
                values[values.Length - 1 - i] = int.Parse(string.Join("", reversedGroups[i].Value.SkipLast(1)));
            }
            var duration = new TimeSpan(values[0], values[1], values[2]);
            return duration;
        }

        public static Video[] GetRecentVideos(string channelId, int num)
        {
            return TwitchApi.Helix.Videos.GetVideoAsync(userId: channelId, first: num).Result.Videos;
        }

        public static TimeSpan? GetUpTime(string channelName)
        {
            string userId = GetIdByUsername(channelName);

            if (userId == null || string.IsNullOrEmpty(userId))
                return null;
            return TwitchApi.V5.Streams.GetUptimeAsync(userId).Result;
        }

        public static string GetIdByUsername(string userName)
        {
            List<string> list = new List<string>() { userName };
            User[] users = TwitchApi.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].Id;
        }

        public static string GetUsernameById(string id)
        {
            List<string> list = new List<string>() { id };
            User[] users = TwitchApi.Helix.Users.GetUsersAsync(list, null).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].DisplayName;
        }

        public static User GetUser(string userName)
        {
            if (userName == string.Empty)
                return null;

            List<string> list = new List<string>() { userName };
            User[] users = TwitchApi.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0];
        }

        public static User[] GetUsersAsync(List<string> userNames)
        {
            if (userNames.Count == 0)
                return null;

            User[] users = TwitchApi.Helix.Users.GetUsersAsync(null, userNames).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users;
        }

        public static Channel GetChannel(string userName)
        {
            string userId = GetIdByUsername(userName);

            if (!string.IsNullOrEmpty(userId))
            {
                Channel channel = TwitchApi.V5.Channels.GetChannelByIDAsync(userId).Result;
                if (channel != null)
                    return channel;
            }

            return null;
        }

        public static User[] GetChannelSubscribers(string userName)
        {
            var userId = GetIdByUsername(userName);
            if (string.IsNullOrEmpty(userId)) return null;

            var subs = TwitchApi.Helix.Subscriptions.GetBroadcasterSubscriptions(userId).Result;
            var userNames = subs.Data.Select(sub => sub.UserName).ToList();
            return GetUsersAsync(userNames);

        }

        public static List<UsernameChangeListing> GetUsernameChangesAsync(string userName)
        {
            return TwitchApi.ThirdParty.UsernameChange.GetUsernameChangesAsync(userName).Result;
        }

        public static List<ChatterFormatted> GetChatters(string channelName)
        {
            return TwitchApi.Undocumented.GetChattersAsync(channelName).Result;
        }

        public static ChatterFormatted GetRandChatter(string channelName)
        {
            var chatters = GetChatters(channelName);
            return chatters[Program.Rand.Next(0, chatters.Count)];
        }
    }
}