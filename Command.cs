using System;
using System.Collections.Generic;
using TwitchLib.Client.Events;
using TwitchLib.PubSub.Events;

namespace HerokuApp
{
    public class Command : Option
    {
        public List<string> names = new List<string>();
        public Dictionary<string, ProbabilityOption> lastOption = new Dictionary<string, ProbabilityOption>();
    }

    public class Bot
    {
        public string ChannelName;
        public List<Command> commands;
        public Action<object, OnNewSubscriberArgs> OnNewSubscriber;
        public Action<object, OnReSubscriberArgs> OnReSubscriber;
        public Action<object, OnGiftedSubscriptionArgs> OnGiftedSubscription;
        public Action<object, OnCommunitySubscriptionArgs> OnCommunitySubscription;
        public Action<object, OnWhisperReceivedArgs> OnWhisperReceived;
        public Action<object, OnStreamUpArgs> OnStreamUp;
        public Action<object, OnRewardRedeemedArgs> OnRewardRedeemed;
        public Dictionary<string, (Command, DateTime)> lastCommand = new Dictionary<string, (Command, DateTime)>();
        public TimeSpan cooldown;
    }
}
