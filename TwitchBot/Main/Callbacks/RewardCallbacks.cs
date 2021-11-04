using System;
using System.Threading.Tasks;
using TwitchBot.Models;
using TwitchLib.PubSub.Events;

// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

namespace TwitchBot.Main.Callbacks
{
    public class RewardCallbacks
    {
        public static void UkrainianStreamRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            //TwitchHelpers.FulFillRedemption(args.e.ChannelId, args.e.RewardId.ToString(), args.e.RedemptionId.ToString());
            var rewardTimespan = TimeSpan.FromMinutes(15);
            args.ChannelInfo.Lang = Lang.ua;
            Task.Delay(rewardTimespan).ContinueWith(_ => args.ChannelInfo.Lang = Lang.ru);
        }
    }
}