using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TwitchBot.Models;
using TwitchLib.Client.Extensions;
using TwitchLib.PubSub.Events;
// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

namespace TwitchBot.Main.Callbacks
{
    public class RewardCallbacks
    {
        public static (bool flag, int num) TimeoutUserBelowData = (false, 0);
        public static readonly List<string> WhoCanSendFromTimeout = new();
        
        public static void TimeoutUserBelowRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            TimeoutUserBelowData.flag = true;
            TimeoutUserBelowData.num++;
        }

        public static void TimeoutMyselfRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            var timeoutTime = TimeSpan.FromMinutes(10);
            KiraTimeoutUser(args, e.DisplayName, timeoutTime, "Награда за баллы канала - \"Таймач самому себе\"");
        }

        public static void KiraTimeoutUser(CallbackArgs args, string username, TimeSpan timeoutTime, string timeoutMessage)
        {
            var channelBot = args.ChannelBot;
            var channelBotInfo = args.ChannelBotInfo;
            MainBotService.BotTwitchClient.TimeoutUser(channelBotInfo.ChannelUsername, username, timeoutTime, timeoutMessage);
            var timeoutMinutes = timeoutTime.TotalMinutes;
            channelBot.Logger.LogInformation("{Username} was timedout on {TimeoutMinutes} minutes", username, timeoutMinutes);
            WhoCanSendFromTimeout.Add(username);
            Task.Delay(timeoutTime).ContinueWith(_ => WhoCanSendFromTimeout.RemoveAll(n => n == username));
        }

        public static void UkrainianStreamRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            //TwitchHelpers.FulFillRedemption(args.e.ChannelId, args.e.RewardId.ToString(), args.e.RedemptionId.ToString());
            var rewardTimespan = TimeSpan.FromMinutes(15);
            args.ChannelBotInfo.Lang = Lang.ua;
            Task.Delay(rewardTimespan).ContinueWith(_ => args.ChannelBotInfo.Lang = Lang.ru);
        }
    }
}