using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchBot.Main.Interfaces;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.PubSub.Events;

namespace TwitchBot.Main.MiniGames
{
    public class TimeoutRewards : IMiniGame
    {
        public readonly List<string> WhoCanSendFromTimeout = new();
        public (bool flag, int num) TimeoutUserBelowData = (false, 0);
        
        public void TimeoutUserBelowRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            TimeoutUserBelowData.flag = true;
            TimeoutUserBelowData.num++;
        }

        public void KiraOnMessageReceived(object s, OnMessageReceivedArgs e, CallbackArgs args)
        {
            if (!TimeoutUserBelowData.flag || e.ChatMessage.IsModerator ||
                e.ChatMessage.IsBroadcaster) return;

            var timeout = TimeSpan.FromMinutes(10 * TimeoutUserBelowData.num);
            TimeoutUser(args, e.ChatMessage.Username, timeout,
                "Попал(-а) под награду за баллы канала - \"Таймач человеку снизу\"");
            TimeoutUserBelowData = default;
        }

        public void KiraOnWhisperReceived(object s, OnWhisperReceivedArgs e, CallbackArgs args)
        {
            var whisperMessage = e.WhisperMessage;
            var senderId = whisperMessage.UserId;
            var isSubscriber = args.ChannelBot.ChannelTwitchHelpers.IsSubscribeToChannel(
                args.ChannelInfo.ChannelUserId,
                senderId);
            if (!isSubscriber ||
                !WhoCanSendFromTimeout.Contains(whisperMessage.Username, StringComparer.OrdinalIgnoreCase)
            ) return;

            BotService.BotTwitchClient.SendMessage(args.ChannelInfo.ChannelUsername,
                $"{whisperMessage.Username} передаёт: {whisperMessage.Message}");
            WhoCanSendFromTimeout.RemoveAll(n =>
                n.Equals(whisperMessage.Username, BotService.StringComparison));
        }

        public void TimeoutMyselfRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            var timeoutTime = TimeSpan.FromMinutes(10);
            TimeoutUser(args, e.DisplayName, timeoutTime, "Награда за баллы канала - \"Таймач самому себе\"");
        }

        private void TimeoutUser(CallbackArgs args, string username, TimeSpan timeoutTime, string timeoutMessage)
        {
            var channelBotInfo = args.ChannelInfo;
            BotService.BotTwitchClient.TimeoutUser(channelBotInfo.ChannelUsername, username, timeoutTime,
                timeoutMessage);
            var timeoutMinutes = timeoutTime.TotalMinutes;
            args.Logger.LogInformation("{Username} was timedout on {TimeoutMinutes} minutes", username, timeoutMinutes);
            WhoCanSendFromTimeout.Add(username);
            Task.Delay(timeoutTime).ContinueWith(_ => WhoCanSendFromTimeout.RemoveAll(n => n == username));
        }
    }
}