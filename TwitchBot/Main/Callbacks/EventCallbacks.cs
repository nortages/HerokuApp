using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Fasterflect;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchBot.Main.DonationAlerts;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Models;
using TwitchBot.Models.AssociativeEntities;
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

        public static void KiraOnCommunitySubscription(object s, OnCommunitySubscriptionArgs e, CallbackArgs args)
        {
            // var _massGiftCount = e.GiftedSubscription.MsgParamMassGiftCount;
            var answer = e.GiftedSubscription.MsgParamMassGiftCount == 1
                ? $"{e.GiftedSubscription.DisplayName}, спасибо за подарочную подписку! PrideFlower"
                : $"{e.GiftedSubscription.DisplayName}, спасибо за подарочные подписки! peepoLove peepoLove peepoLove";
            BotService.BotTwitchClient.SendMessage(e.Channel, answer);
        }

        public static void KiraOnGiftedSubscription(object s, OnGiftedSubscriptionArgs e, CallbackArgs args)
        {
            if (!string.Equals(e.GiftedSubscription.MsgParamRecipientDisplayName, BotService.BotUsername,
                BotService.StringComparison)) return;

            var answer = "спасибо большое за подписку kupaLove kupaLove kupaLove";
            BotService.BotTwitchClient.SendMessage(e.Channel, $"{e.GiftedSubscription.DisplayName}, {answer}");
        }

        public static void KiraOnReSubscriber(object s, OnReSubscriberArgs e, CallbackArgs args)
        {
            BotService.BotTwitchClient.SendMessage(e.Channel,
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
            BotService.BotTwitchClient.SendMessageWithDelay(e.Channel, $"!саб @{e.Subscriber.DisplayName}",
                TimeSpan.FromSeconds(1));
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
            var channelBotInfo = args.ChannelInfo;
            var isTimedout = channelBot.ChannelTwitchHelpers.IsUserTimedOut(channelBotInfo.ChannelUserId, userId)
                .Result;
            if (isTimedout) return;

            var reason = $"Вместе со второй личностью {username}";
            var timeoutTime = TimeSpan.FromSeconds(e.UserTimeout.TimeoutDuration);

            var isModerator = channelBot.ChannelTwitchHelpers.IsUserModerator(channelBotInfo.ChannelUserId, userId);
            if (isModerator)
            {
                if (args.ChannelBot.ChannelTwitchClient is { } channelTwitchClient)
                    channelTwitchClient.TimeoutModer(channelBotInfo.ChannelUsername, usernameToBan, timeoutTime,
                        reason);
            }
            else
            {
                BotService.BotTwitchClient.TimeoutUser(channelBotInfo.ChannelUsername, usernameToBan, timeoutTime,
                    reason);
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

            BotService.BotTwitchClient.SendMessage(args.ChannelInfo.ChannelUsername, answer);
        }

        public static void GeneralOnChatCommandReceivedCallback(object s, OnChatCommandReceivedArgs e,
            CallbackArgs args)
        {
            var channel = e.Command.ChatMessage.Channel;
            var commandString = e.Command.CommandText;
            var channelBotInfo = args.ChannelInfo;
            var userId = e.Command.ChatMessage.UserId;
            var username = e.Command.ChatMessage.Username;
            
            var user = args.DbContext.Users.SingleOrDefault(u => u.Id == userId);
            if (user is null)
                args.DbContext.Users.Add(new User {Id = userId, Name = username});
            
            var channelCommand = args.ChannelInfo.ChannelCommands.SingleOrDefault(c => c.Command.Names.Contains(commandString));
            if (channelCommand is not {Command: {IsPrivate: false}})
                return;
            var command = channelCommand.Command;
            
            if (!channelBotInfo.IsTestMode && !channelCommand.IsAvailable())
                return;
            
            string answer;
            if (e.Command.CommandIdentifier == '?')
            {
                var description = command.Description;
                var descriptionResult = string.IsNullOrEmpty(description)
                    ? " не имеет описания"
                    : $"{char.ToLower(description[0]) + description[1..]}";
                answer = $"@{e.Command.ChatMessage.DisplayName} эта команда {descriptionResult}";
            }
            else
            {
                var userChannelCommand = args.DbContext.UserChannelCommands.SingleOrDefault(u => u.UserId == userId && u.ChannelCommandId == channelCommand.Id);
                // var userChannelCommand = channelCommand.UserChannelCommands.SingleOrDefault(u => u.UserId == userId);
                if (userChannelCommand is null)
                {
                    userChannelCommand = new UserChannelCommand
                    {
                        UserId = e.Command.ChatMessage.UserId,
                        ChannelCommandId = channelCommand.Id,
                        Amount = 0,
                    };                }
                else
                {
                    if (!channelBotInfo.IsTestMode && !userChannelCommand.IsAvailable())
                        return;    
                }
            
                object callMethodTarget;
                if (command.MiniGame is null)
                {
                    callMethodTarget = typeof(CommandCallbacks);
                }
                else
                {
                    if (!command.MiniGame.IsEnabled)
                        return;
                    var miniGameInstance = args.ChannelBot.MiniGameNameToInstance[command.MiniGame.Id];
                    callMethodTarget = miniGameInstance;
                }

                var commandCallbackArgs = new CommandCallbackArgs
                {
                    Logger = args.Logger,
                    ChannelBot = args.ChannelBot,
                    ChannelInfo = args.ChannelInfo,
                    DbContext = args.DbContext,
                    
                    CallMethodTarget = callMethodTarget, 
                    UserChannelCommand = userChannelCommand,
                    Command = command,
                };
                
                answer = command.GetAnswer(e, commandCallbackArgs);
                
                userChannelCommand.LastUsage = DateTime.Now.ToUniversalTime();
                userChannelCommand.Amount++;
                channelCommand.UserChannelCommands.Add(userChannelCommand);
                args.DbContext.Update(userChannelCommand);
            }

            if (string.IsNullOrEmpty(answer))
                return;
            BotService.BotTwitchClient.SendMessage(channel, answer);
        }

        public static void GeneralOnWhisperCommandReceivedCallback(object s, OnWhisperCommandReceivedArgs e,
            CallbackArgs args)
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
            var channelBotInfo = args.ChannelInfo;

            var channelMessageCommand = channelBotInfo.ChannelMessageCommands
                .SingleOrDefault(c => Regex.Match(message, c.MessageCommand.Regex, BotService.RegexOptions).Success);
            if (channelMessageCommand is not
                {IsEnabled: true, MessageCommand: {Option: {IsEnabled: true}} messageCommand})
                return;

            var messageCommandCallbackArgs = new MessageCommandCallbackArgs
            {
                Logger = args.Logger,
                ChannelBot = args.ChannelBot,
                ChannelInfo = args.ChannelInfo,
                DbContext = args.DbContext,
                
                MessageCommand = messageCommand,
            };
            
            var answer = messageCommand.GetAnswer(e, messageCommandCallbackArgs);
            if (string.IsNullOrEmpty(answer)) return;

            if (messageCommand.Option.IsMentionRequired is true)
                answer = $"@{e.ChatMessage.DisplayName}, {answer}";
            BotService.BotTwitchClient.SendMessage(channel, answer);
        }

        public static void GeneralOnRewardRedeemedCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            if (e.Status != "UNFULFILLED") return;

            args.Logger.LogInformation(JsonConvert.SerializeObject(e));

            RewardRedemption rewardRedemption;
            object callMethodTarget;
            
            var channelRedemption =
                args.ChannelInfo.ChannelRewardRedemptions.SingleOrDefault(r =>
                    r.RewardRedemption.Title == e.RewardTitle);
            if (channelRedemption is not {IsEnabled: true})
                return;
            rewardRedemption = channelRedemption.RewardRedemption;
            
            if (rewardRedemption.MiniGame is null)
            {
                callMethodTarget = typeof(RewardCallbacks);
            }
            else
            {
                if (!rewardRedemption.MiniGame.IsEnabled)
                    return;
                var miniGameInstance = args.ChannelBot.MiniGameNameToInstance[rewardRedemption.MiniGame.Id];
                callMethodTarget = miniGameInstance;
            }

            callMethodTarget.CallMethod(rewardRedemption.CallbackId, s, e, args);
        }
    }
}