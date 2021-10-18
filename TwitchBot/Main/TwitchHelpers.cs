using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Core.Models.Undocumented.Chatters;
using TwitchLib.Api.Helix.Models.ChannelPoints.UpdateCustomRewardRedemptionStatus;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using TwitchLib.Api.Helix.Models.Videos.GetVideos;
using TwitchLib.Api.Services;
using TwitchLib.Api.ThirdParty.UsernameChange;
using TwitchLib.Api.V5.Models.Channels;

namespace TwitchBot.Main
{
    public class TwitchHelpers
    {
        public TwitchAPI TwitchApi { get; }

        public TwitchHelpers(string clientId, string accessToken)
        {
            TwitchApi = new TwitchAPI();
            TwitchApi.Settings.ClientId = clientId;
            TwitchApi.Settings.AccessToken = accessToken;
            TwitchApi.Settings.Secret = "Twitch"; // Need to not hard code this
        }
        
        public void SubscribeToStreamEvents(string url, string channelId, TimeSpan duration)
        {
            TwitchApi.Helix.Webhooks.StreamUpDownAsync(url, WebhookCallMode.Subscribe, channelId, duration);
        }

        public bool IsSubscribeToChannel(string broadcasterId, string userId)
        {
            return TwitchApi.Helix.Subscriptions.GetUserSubscriptionsAsync(broadcasterId, new List<string> { userId })
                .Result.Data.Length != 0;
        }

        public async Task<bool> IsUserTimedOut(string broadcasterId, string userId, string accessToken = null)
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

        public bool IsUserModerator(string broadcasterId, string userId, string accessToken = null)
        {
            var result = TwitchApi.Helix.Moderation.GetModeratorsAsync(broadcasterId, new List<string> { userId }, accessToken: accessToken).Result;
            return result is {Data: {Length: > 0}};
        }

        public void FulfillRedemption(string broadcasterId, string rewardId, string redemptionId)
        {
            var request = new UpdateCustomRewardRedemptionStatusRequest
            {
                Status = CustomRewardRedemptionStatus.FULFILLED
            };
            TwitchApi.Helix.ChannelPoints.UpdateCustomRewardRedemptionStatus(broadcasterId, rewardId, new List<string> { redemptionId}, request);
        }

        public bool GetOnlineStatus(string channelId)
        {
            return TwitchApi.V5.Streams.BroadcasterOnlineAsync(channelId).Result;
        }

        public async Task<bool> IsStreamUp(string channelId)
        {
            var response = await TwitchApi.Helix.Streams.GetStreamsAsync(userIds: new List<string> { channelId }, first: 1);
            var streams = response.Streams;
            return streams.Length != 0;
        }

        public TimeSpan? GetElapsedTimeFromLastStream(string channelId)
        {
            var result = TwitchApi.Helix.Videos.GetVideoAsync(userId: channelId, first: 1).Result;
            var lastVideo = result.Videos.FirstOrDefault();
            if (lastVideo == null) return null;
            
            var durationStr = lastVideo.Duration;
            var duration = GetTimeSpanFromTwitchDuration(durationStr);
            return DateTime.Now - DateTime.Parse(lastVideo.CreatedAt).Add(duration);

        }

        public TimeSpan GetTimeSpanFromTwitchDuration(string durationStr)
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

        public Video[] GetRecentVideos(string channelId, int num)
        {
            return TwitchApi.Helix.Videos.GetVideoAsync(userId: channelId, first: num).Result.Videos;
        }

        public TimeSpan? GetUpTime(string channelName)
        {
                var userId = GetIdByUsername(channelName);

            if (userId == null || string.IsNullOrEmpty(userId))
                return null;
            return TwitchApi.V5.Streams.GetUptimeAsync(userId).Result;
        }

        public string GetIdByUsername(string userName)
        {
            var list = new List<string> { userName };
            var users = TwitchApi.Helix.Users.GetUsersAsync(null, list).Result.Users;
            return users is {Length: > 0} ? users[0].Id : null;
        }

        public string GetUsernameById(string id)
        {
            var list = new List<string> { id };
            var users = TwitchApi.Helix.Users.GetUsersAsync(list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0].DisplayName;
        }

        public User GetUser(string userName)
        {
            if (userName == string.Empty)
                return null;

            List<string> list = new List<string>() { userName };
            User[] users = TwitchApi.Helix.Users.GetUsersAsync(null, list).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users[0];
        }

        public User[] GetUsersAsync(List<string> userNames)
        {
            if (userNames.Count == 0)
                return null;

            User[] users = TwitchApi.Helix.Users.GetUsersAsync(null, userNames).Result.Users;

            if (users == null || users.Length == 0)
                return null;

            return users;
        }

        public Channel GetChannel(string userName)
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

        public User[] GetChannelSubscribers(string userName)
        {
            var userId = GetIdByUsername(userName);
            if (string.IsNullOrEmpty(userId)) return null;

            var subs = TwitchApi.Helix.Subscriptions.GetBroadcasterSubscriptions(userId).Result;
            var userNames = subs.Data.Select(sub => sub.UserName).ToList();
            return GetUsersAsync(userNames);

        }

        public List<UsernameChangeListing> GetUsernameChangesAsync(string userName)
        {
            return TwitchApi.ThirdParty.UsernameChange.GetUsernameChangesAsync(userName).Result;
        }

        public List<ChatterFormatted> GetChatters(string channelName)
        {
            return TwitchApi.Undocumented.GetChattersAsync(channelName).Result;
        }

        public ChatterFormatted GetRandChatter(string channelName)
        {
            var chatters = GetChatters(channelName);
            return chatters[Program.Rand.Next(0, chatters.Count)];
        }
    }
}