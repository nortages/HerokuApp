using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fasterflect;
using Newtonsoft.Json;
using TwitchBot.Main.DonationAlerts;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Models;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.PubSub.Events;
// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

namespace TwitchBot.Main.Callbacks
{
    public class EventCallbacks
    {
        private static bool _allowDoubleBan;
        
        public static void KiraOnMessageReceived(object s, OnMessageReceivedArgs e, CallbackArgs args)
        {
            if (!RewardCallbacks.TimeoutUserBelowData.flag || e.ChatMessage.IsModerator ||
                e.ChatMessage.IsBroadcaster) return;
            
            var timeout = TimeSpan.FromMinutes(10 * RewardCallbacks.TimeoutUserBelowData.num);
            RewardCallbacks.KiraTimeoutUser(args, e.ChatMessage.Username, timeout,
                "Попал(-а) под награду за баллы канала - \"Таймач человеку снизу\"");
            RewardCallbacks.TimeoutUserBelowData = default;
        }

        public static void KiraOnWhisperReceived(object s, OnWhisperReceivedArgs e, CallbackArgs args)
        {
            var whisperMessage = e.WhisperMessage;
            var senderId = whisperMessage.UserId;
            var isSubscriber = args.ChannelBot.ChannelTwitchHelpers.IsSubscribeToChannel(
                args.ChannelBotInfo.ChannelUserId,
                senderId);
            if (!isSubscriber ||
                !RewardCallbacks.WhoCanSendFromTimeout.Contains(whisperMessage.Username, StringComparer.OrdinalIgnoreCase)
            ) return;
            
            MainBotService.BotTwitchClient.SendMessage(args.ChannelBotInfo.ChannelUsername,
                $"{whisperMessage.Username} передаёт: {whisperMessage.Message}");
            RewardCallbacks.WhoCanSendFromTimeout.RemoveAll(n =>
                n.Equals(whisperMessage.Username, MainBotService.StringComparison));
        }

        public static void KiraOnCommunitySubscription(object s, OnCommunitySubscriptionArgs e, CallbackArgs args)
        {
            // var _massGiftCount = e.GiftedSubscription.MsgParamMassGiftCount;
            var answer = e.GiftedSubscription.MsgParamMassGiftCount == 1
                ? $"{e.GiftedSubscription.DisplayName}, спасибо за подарочную подписку! PrideFlower"
                : $"{e.GiftedSubscription.DisplayName}, спасибо за подарочные подписки! peepoLove peepoLove peepoLove";
            MainBotService.BotTwitchClient.SendMessage(e.Channel, answer);
        }

        public static void KiraOnGiftedSubscription(object s, OnGiftedSubscriptionArgs e, CallbackArgs args)
        {
            if (!string.Equals(e.GiftedSubscription.MsgParamRecipientDisplayName, MainBotService.BotUsername,
                MainBotService.StringComparison)) return;
            
            var answer = $"спасибо большое за подписку kupaLove kupaLove kupaLove";
            MainBotService.BotTwitchClient.SendMessage(e.Channel, $"{e.GiftedSubscription.DisplayName}, {answer}");
        }

        public static void KiraOnReSubscriber(object s, OnReSubscriberArgs e, CallbackArgs args)
        {
            var channelBot = args.ChannelBot;
            MainBotService.BotTwitchClient.SendMessage(e.Channel,
                $"{e.ReSubscriber.DisplayName}, спасибо за продление подписки! Poooound");
        }

        public static void KiraOnNewSubscriber(object s, OnNewSubscriberArgs e, CallbackArgs args)
        { 
            var channelBot = args.ChannelBot;

            //var passportEmote =
            //    TwitchHelpers.IsSubscribeToChannel(kiraChannelBot.UserId, MainBotService.BotId,
            //        kiraChannelBot.HelperBot.AccessToken)
            //        ? "kupaPasport"
            //        : "";
            //kiraChannelBot.twitchClient.SendMessage(e.Channel,
            //    $"{e.Subscriber.DisplayName}, спасибо за подписку! bleedPurple Давайте сюда Ваш паспорт FBCatch {passportEmote}");
            MainBotService.BotTwitchClient.SendMessageWithDelay(e.Channel, $"!саб @{e.Subscriber.DisplayName}", TimeSpan.FromSeconds(1));
        }
        
        public static void KiraOnUserTimedout(object s, OnUserTimedoutArgs e, CallbackArgs args)
        {
            var twoUsers = new List<Tuple<string, string>>
            {
                new("th3globalist", "74415861"),
                new("rinael_lapki", "116926816")
            };

            var username = e.UserTimeout.Username;
            if (twoUsers.All(n => n.Item1 != username)) return;
            if (!_allowDoubleBan) return;
            
            var wasBannedIndex = twoUsers.FindIndex(n => n.Item1 == username);
            var (usernameToBan, userId) = twoUsers[twoUsers.Count - 1 - wasBannedIndex];

            var channelBot = args.ChannelBot;
            var channelBotInfo = args.ChannelBotInfo;
            var isTimedout = channelBot.ChannelTwitchHelpers.IsUserTimedOut(channelBotInfo.ChannelUserId, userId).Result;
            if (isTimedout) return;

            var reason = $"Вместе со второй личностью {username}";
            var timeoutTime = TimeSpan.FromSeconds(e.UserTimeout.TimeoutDuration);

            var isModerator = channelBot.ChannelTwitchHelpers.IsUserModerator(channelBotInfo.ChannelUserId, userId);
            if (isModerator)
            {
                if (args.ChannelBot.ChannelTwitchClient is {} channelTwitchClient)
                    channelTwitchClient.TimeoutModer(channelBotInfo.ChannelUsername, usernameToBan, timeoutTime, reason);
            }
            else
            {
                MainBotService.BotTwitchClient.TimeoutUser(channelBotInfo.ChannelUsername, usernameToBan, timeoutTime, reason);
            }

            _allowDoubleBan = false;
            Task.Delay(TimeSpan.FromSeconds(15)).ContinueWith(_ => _allowDoubleBan = true);
        }
        
        public static void KiraOnDonationReceived(object s, OnDonationAlertArgs e, CallbackArgs args)
        {
            Console.WriteLine("A new donation!");
            Console.WriteLine(JsonConvert.SerializeObject(e));
            
            if (!e.IsShown) return;
            
            var username = string.IsNullOrEmpty(e.Username) ? "Аноним" : e.Username;
            var answer = $"/me {username} закинул(-а) {e.Amount} шекелей";
            answer += e.MessageType switch
            {
                DonationMessageType.Text when string.IsNullOrEmpty(e.Message) => "!",
                DonationMessageType.Text => $" со словами: {e.Message}",
                DonationMessageType.Audio => ", записав аудио сообщение!",
                _ => throw new ArgumentOutOfRangeException()
            };

            MainBotService.BotTwitchClient.SendMessage(args.ChannelBotInfo.ChannelUsername, answer);
        }
        
        public static void GeneralOnChatCommandReceivedCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var channel = e.Command.ChatMessage.Channel;
            var commandString = e.Command.CommandText;
            var channelBotInfo = args.ChannelBotInfo;
            
            string answer;
            var channelCommand = channelBotInfo.ChannelCommands.SingleOrDefault(c => c.Command.Names.Contains(commandString));
            if (channelCommand is not {IsEnabled: true, Command: {IsPrivate: false, Option: {IsEnabled: true }} command})
                return;
            
            if (e.Command.CommandIdentifier == '?')
            {
                var description = command.Description;
                var descriptionResult = (string.IsNullOrEmpty(description)
                    ? " не имеет описания"
                    : $"{char.ToLower(description[0]) + description[1..]}");
                answer = $"@{e.Command.ChatMessage.DisplayName} эта команда {descriptionResult}";
            }
            else
            {
                answer = command.GetAnswer(e, args);
            }
            if (string.IsNullOrEmpty(answer)) return;
            MainBotService.BotTwitchClient.SendMessage(channel, answer);
        }
        
        public static void GeneralOnWhisperCommandReceivedCallback(object s, OnWhisperCommandReceivedArgs e, CallbackArgs args)
        {
            if (!e.Command.WhisperMessage.IsMe())
                return;
            
            var privateCommand = args.DbContext.Commands
                .Where(c => c.IsPrivate)
                .SingleOrDefault(c => c.Names.Contains(e.Command.CommandText));
            if (privateCommand == null)
                return;
            
            typeof(CommandCallbacks).CallMethod(privateCommand.Option.CallbackId, privateCommand, e, args);
        }
        
        public static void GeneralOnMessageReceivedCallback(object s, OnMessageReceivedArgs e, CallbackArgs args)
        {
            var channel = e.ChatMessage.Channel;
            var message = e.ChatMessage.Message;
            var channelBot = args.ChannelBot;
            var channelBotInfo = args.ChannelBotInfo;

            var channelMessageCommand = channelBotInfo.ChannelMessageCommands
                .SingleOrDefault(c => Regex.Match(message, c.MessageCommand.Regex, MainBotService.RegexOptions).Success);
            if (channelMessageCommand is not {IsEnabled: true, MessageCommand: {Option: {IsEnabled: true }} messageCommand})
                return;
            
            var answer = messageCommand.GetAnswer(e, args);
            if (string.IsNullOrEmpty(answer)) return;
            
            if (messageCommand.Option.IsMentionRequired is true) 
                answer = $"@{e.ChatMessage.DisplayName}, {answer}";
            MainBotService.BotTwitchClient.SendMessage(channel, answer);
        }

        public static void GeneralOnRewardRedeemedCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            if (e.Status != "UNFULFILLED") return;

            Console.WriteLine("Someone redeemed for a reward!");
            Console.WriteLine($"Username: {e.DisplayName},\n Title: {e.RewardTitle}");

            var redemption = args.ChannelBotInfo.RewardRedemptions.SingleOrDefault(r => r.Title == e.RewardTitle);
            if (redemption == null) return;

            typeof(RewardCallbacks).CallMethod(redemption.CallbackId, s, e, args);
        }   
    }
}