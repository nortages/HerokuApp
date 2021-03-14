using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HerokuApp.Main.CustomTypes;
using HerokuApp.Main.Extensions;
using Newtonsoft.Json.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.PubSub.Events;
using HerokuApp.Main.CustomTypes.Hearthstone;

namespace HerokuApp.Main
{
    public partial class NortagesTwitchBot
    {
        static int massGifts = 0;
        static (bool flag, int num) timeoutUserBelowData = (false, 0);
        static readonly List<string> whoCanSendFromTimeout = new List<string>();
        static readonly List<DuelOffer> duelOffersPendingAcceptance = new List<DuelOffer>();

        static public Dictionary<string, List<Tuple<Board, Board>>> bgDuelsRecords = new Dictionary<string, List<Tuple<Board, Board>>>();
        
        #region Commands callbacks
        public static string GetCommandsCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var opt = (Option)args.sender;
            var baseUrl = Config.isDeployed ? "https://asp-docker.herokuapp.com" : "http://localhost:5000";
            return string.Format(opt.GetUnformattedAnswer(args.lang), baseUrl, args.bot.ChannelName.ToLower());
        }

        public static string WaitingStreamCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            string channelName;
            var opt = (Option)args.sender;
            if (string.IsNullOrEmpty(args.e.Command.ArgumentsAsString))
            {
                channelName = args.bot.ChannelName;
            }
            else
            {
                channelName = args.e.Command.ArgumentsAsString.TrimStart('@').ToLower();
            }
            string channelId;
            if (string.Equals(channelName, args.bot.ChannelName, StringComparison.InvariantCultureIgnoreCase))
            {
                channelId = args.bot.UserId;
            }
            else
            {
                channelId = TwitchHelpers.GetIdByUsername(channelName);
            }
            string answer;
            var isStreamUp = TwitchHelpers.IsStreamUp(channelId);
            if (isStreamUp)
            {
                answer = opt.Options.Single(n => n.Id == "stream_on").GetUnformattedAnswer(args.lang);
            }
            else
            {
                var result = TwitchHelpers.GetLastVideoDate(channelId);
                if (result != null)
                {
                    var timespan = result.Value;
                    string timespanPart = UtilityFunctions.FormatTimespan(timespan);
                    answer = opt.Options.Single(n => n.Id == "waiting").GetUnformattedAnswer(args.lang);
                    answer = string.Format(answer, channelName, timespanPart);
                }
                else
                {                    
                    answer = opt.Options.Single(n => n.Id == "not_streamer").GetUnformattedAnswer(args.lang);
                }
            }
            return answer;
        }

        public static string BoatCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {           
            var answer = "";
            var opt = (Option)args.sender;
            var channel = args.e.Command.ChatMessage.Channel;
            var username = args.e.Command.ChatMessage.Username;

            var assessmentOptions = new List<string> { "слабовато чел...", "а ты скилловый Jebaited" };
            var pirates = new List<MinionInfo>();
            var assessment = "";

            MinionInfo specialPirate = null;
            var allPirates = HearthstoneApiClient.GetBattlegroundsMinions(MinionType.Pirate);
            if (args.e.Command.ArgumentsAsList.Count != 0 && args.e.Command.ArgumentsAsList[0] == "триплет" && Bot.IsMeOrBroadcaster(args.e))
            {
                if (args.e.Command.ArgumentsAsList[1] == "элизы")
                {
                    specialPirate = allPirates.Single(n => n.Id == 61047);
                }
                else
                {
                    specialPirate = allPirates[Config.rand.Next(0, allPirates.Count)];
                }
            }

            for (int i = 0; i < 3; i++)
            {
                var pirate = specialPirate ?? allPirates[Config.rand.Next(0, allPirates.Count)];
                pirates.Add(pirate);
                answer += $"{pirate}, ";
            }

            answer = answer.TrimEnd(new char[] { ' ', ',' });
            if (pirates.All(n => pirates[0].Id == n.Id))
            {
                switch (pirates[0].Id)
                {
                    // Check for Elisa
                    case 61047:
                        assessment = "ТРИ ЭЛИЗЫ ЭТ КОНЕЧНО ПРИКОЛ, ДО ВСТРЕЧИ ЧЕРЕЗ ПОЛЧАСА LUL";
                        UtilityFunctions.TimeoutCommandUser(args, TimeSpan.FromMinutes(30), "Выпало 3 Элизы с команды !лодка");
                        break;
                    // Check for Amalgadon
                    case 61444:
                        assessment = "777, ЛОВИ ВИПКУ";
                        if (!(args.e.Command.ChatMessage.IsModerator || args.e.Command.ChatMessage.IsVip))
                        {
                            args.bot.HelperBot.twitchClient.SendMessage(channel, $"/vip {username}");
                        }
                        break;
                    // Just triple
                    default:
                        assessment = "найс триплет с лодки, жаль все равно отъедешь BloodTrail";
                        break;
                }
            }
            else
            {
                var maxMark = 57; // Elisa x3
                // Assess pirates
                int mark = 0;
                foreach (var pirate in pirates)
                {
                    mark += pirate.Attack + pirate.Health + pirate.Battlegrounds.Tier;
                }
                var quotient = (double)mark / maxMark;
                assessment = assessmentOptions[quotient > 0.6 ? 1 : 0];
            }
            return $"YEP {answer} YEP , {assessment}";
        }

        public static string OfferDuelCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            string answer = null;
            var whoOffers = args.e.Command.ChatMessage.Username;
            string whomIsOffered = null;
            var argsAsList = args.e.Command.ArgumentsAsList;

            if (argsAsList.Count > 0)
            {
                whomIsOffered = argsAsList[0].TrimStart('@');
            }

            if (string.IsNullOrEmpty(whomIsOffered))
            {
                //var randChatter = TwitchHelpers.GetRandChatter(args.e.Command.ChatMessage.Channel);
                //whomIsOffered = randChatter.Username;
                //answer = $"@{whomIsOffered}, {whoOffers} предложил тебе дуэль! Чтобы принять, напиши !принять или !accept";
                return null;
            }            

            var newOffer = new DuelOffer(whoOffers, whomIsOffered);
            if (string.Equals(whomIsOffered, Config.BotUsername, Config.stringComparison))
            {                
                var regex = new Regex(@"(((\d{3,}),?)*\|((\d{3,}),?)*)");
                if (args.isTestMode && argsAsList.Count >= 2 && regex.IsMatch(argsAsList[1]))
                {
                    var allIds = argsAsList[1].Split("|");
                    var player1Ids = allIds[0].Split(",").Select(n => int.Parse(n)).ToArray();
                    var player2Ids = allIds[1].Split(",").Select(n => int.Parse(n)).ToArray();
                    return PerformDuel(newOffer, Tuple.Create(player1Ids, player2Ids));
                }
                return PerformDuel(newOffer);
            }

            if (duelOffersPendingAcceptance.Contains(newOffer)) return null;

            answer = $"@{whomIsOffered}, {whoOffers} предложил тебе дуэль! Чтобы принять, напиши !принять или !accept";
            duelOffersPendingAcceptance.Add(newOffer);

            Task.Delay(TimeSpan.FromSeconds(15)).ContinueWith(t => {
                if (!duelOffersPendingAcceptance.Contains(newOffer)) return;
                duelOffersPendingAcceptance.Remove(newOffer);
            });
            return answer;
        }

        public static string AcceptDuelCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var username = args.e.Command.ChatMessage.Username;
            var offer = duelOffersPendingAcceptance.FirstOrDefault(n => string.Equals(n.WhomIsOffered, username, Config.stringComparison));
            if (offer == null) return null;

            duelOffersPendingAcceptance.Remove(offer);

            return PerformDuel(offer);
        }

        private static string PerformDuel(DuelOffer offer, Tuple<int[], int[]> chosenIds = null)
        {
            var round = new BattlegroundsRound(offer.WhoOffers, offer.WhomIsOffered);

            var minions = round.SummonStartMinions(chosenIds);
            if (minions == null) return null;
            var (minion1, minion2) = minions;

            var outcome = round.Play();
            bgDuelsRecords.Add(round.Id, round.BoardStatesDuringRound);            

            string result;
            if (outcome == Outcome.Tie)
            {
                result = "обе стороны вышли в ничью!";
            }
            else
            {
                result = $"победил {(outcome == Outcome.Win ? offer.WhoOffers : offer.WhomIsOffered)}!";
            }
            return $"{offer.WhoOffers} выпало {minion1}, а {offer.WhomIsOffered} - {minion2}! В результате недолгой схватки {result}";
        }

        public static string RadishTiredOptionCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var currentOption = (Option)args.sender;
            string username = args.e.Command.ChatMessage.Username;
            string channel = args.e.Command.ChatMessage.Channel;
            //var usageFrequency = ((ProbabilityOption)args.opt).command.usageFrequency[username];
            var option = currentOption.GetRandProbabilityOption(Config.rand.NextDouble());
            //var answer = currentOption.GetAnswerFromOptions(args);
            string answer = currentOption.GetAnswer(args);
            var numOfUsing = currentOption.Parent.usageFrequency[username];
            var timesWord = "раз";
            var endings = args.lang == "ua" ? new string[] { "", "и", "iв" } : new string[] { "", "а", "" };
            timesWord += UtilityFunctions.GetWordEnding(numOfUsing, endings);
            answer = string.Format(answer, numOfUsing, timesWord);
            if (option == currentOption.Options[0])
            {
                var timeoutTime = TimeSpan.FromMinutes(1);
                if (args.e.Command.ChatMessage.IsModerator)
                {
                    args.bot.HelperBot.twitchClient.TimeoutModer(channel, username, timeoutTime);
                }
                else
                {
                    args.bot.twitchClient.TimeoutUser(channel, username, timeoutTime);
                }
            }
            return answer;
        }

        public static string RadishTalksOptionCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var opt = (Option)args.sender;
            var option = opt.GetRandProbabilityOption(Config.rand.NextDouble());
            return string.Format(opt.GetUnformattedAnswer(args.lang), option.GetAnswer(args));
        }

        public static string RadishTransformsOptionCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var opt = (Option)args.sender;
            ProbabilityOptionInfo option;
            var username = args.e.Command.ChatMessage.Username;
            if (opt.usageFrequency.ContainsKey(username))
            {
                if (opt.usageFrequency[username] >= 9)
                {
                    option = opt.GetRandProbabilityOption(Config.rand.NextDouble());
                    if (option == opt.Options[1])
                    {
                        opt.usageFrequency[username] = 0;
                    }
                }
                else
                {
                    option = opt.Options[0];
                }
            }
            else
            {
                opt.usageFrequency.Add(username, 0);
                option = opt.Options[0];
            }
            opt.usageFrequency[username]++;
            var answer = option.GetAnswer(args);
            return answer;
        }

        public static string RadishDetonatorOptionCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var currentOption = (Option)args.sender;
            var currentCommand = (Command)currentOption.Parent;
            ProbabilityOptionInfo option;
            var username = args.e.Command.ChatMessage.Username;
            if (currentOption.usageFrequency.ContainsKey(username))
            {
                if (currentCommand.lastOption[username] == currentOption)
                {
                    option = currentOption.GetRandProbabilityOption(Config.rand.NextDouble());
                }
                else
                {
                    option = currentOption.Options[0];
                }
            }
            else
            {
                currentOption.usageFrequency.Add(username, 0);
                option = currentOption.Options[0];
            }
            currentOption.usageFrequency[username]++;
            var answer = option.GetAnswer(args);
            return answer;
        }

        public static string ThrowRadishOptionCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var opt = (Option)args.sender;
            var randChatter = TwitchHelpers.GetRandChatter(args.e.Command.ChatMessage.Channel);
            var answer = opt.GetAnswerFromOptions(args);
            //var option = opt.GetRandProbabilityOption(Config.rand.NextDouble());
            //var answer = option.GetAnswer(args);
            return string.Format(opt.GetUnformattedAnswer(args.lang), answer, randChatter.Username);
        }

        public static string CardCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            string id, cardDescription = null;
            var opt = (Option)args.sender;
            if (args.isTestMode && int.TryParse(args.e.Command.ArgumentsAsString, out var customId))
            {
                id = customId.ToString();
                var option = opt.Options.SingleOrDefault(n => n.Id == id);
                if (option == null)
                {
                    cardDescription = "the card with such an id wasn't found";
                }
                else
                {
                    args.sender = option;
                    cardDescription = option.GetAnswer(args);
                }
            }
            else
            {
                var option = opt.GetRandProbabilityOption(Config.rand.NextDouble());
                args.sender = option;
                cardDescription = option.GetAnswer(args);
                id = option.Id;
            }

            if (id == "59935")
            {
                UtilityFunctions.TimeoutCommandUser(args, TimeSpan.FromMinutes(10));

            }
            return cardDescription;
        }

        public static string ManulCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            var manulsNumProperty = Config.GetJPropertyFromCommandsInfo("manuls");
            var manulsNum = (int)manulsNumProperty.Value;
            manulsNumProperty.Value = ++manulsNum;
            var manulWord = "манул";
            manulWord += UtilityFunctions.GetWordEnding(manulsNum, new string[] { "", "а", "ов" });
            var answer = $"{manulsNum} {manulWord}";

            // Temporarily disabled
            //var randNum = Config.rand.Next(0, 10);
            //if (randNum == -1)
            //{
            //    manulsEasterEggs ??= Config.GetManulsEasterEggs();
            //    var notPlayed = manulsEasterEggs.Where(n => !n.Value<bool>("wasPlayed")).ToList();
            //    randNum = Config.rand.Next(0, notPlayed.Count);
            //    var easterEgg = notPlayed[randNum];
            //    easterEgg["wasPlayed"] = true;
            //    Config.SaveManulsEasterEggs(manulsEasterEggs);
            //    answer += " " + easterEgg.Value<string>("link");
            //}

            return answer;
        }

        public static string ToggleUkrainianStreamCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            switch (args.e.Command.ArgumentsAsString)
            {
                case "on":
                    args.bot.BotLang = "ua";
                    break;
                case "off":
                    args.bot.BotLang = "ru";
                    break;
                default:
                    break;
            }
            return null;
        }

        public static string WhisperMeCommandCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            args.bot.twitchClient.SendMessage(args.bot.ChannelName, $"/w {args.e.Command.ChatMessage.DisplayName} Test");
            return null;
        }

        public static string SaveCommandsUsageCallback(CallbackArgs<OnChatCommandReceivedArgs> args)
        {
            Config.SaveCommandsInfo();
            return null;
        }
        #endregion Commands callbacks

        #region Rewards callbacks
        public static void TimeoutUserBelowRewardCallback(CallbackArgs<OnRewardRedeemedArgs> args)
        {
            timeoutUserBelowData.flag = true;
            timeoutUserBelowData.num++;
        }

        public static void TimeoutMyselfRewardCallback(CallbackArgs<OnRewardRedeemedArgs> args)
        {
            var timeoutTime = TimeSpan.FromMinutes(10);
            KiraTimeoutUser(args.bot, args.e.DisplayName, timeoutTime, "Награда за баллы канала - \"Таймач самому себе\"");
        }

        public static void KiraTimeoutUser(Bot bot, string username, TimeSpan timeoutTime, string timeoutMessage)
        {
            bot.twitchClient.TimeoutUser(bot.ChannelName, username, timeoutTime, timeoutMessage);
            bot.Log($"{username} was timedout on {timeoutTime.TotalMinutes} minutes");
            whoCanSendFromTimeout.Add(username);
            Task.Delay(timeoutTime).ContinueWith(t => whoCanSendFromTimeout.RemoveAll(n => string.Equals(n, username, StringComparison.InvariantCultureIgnoreCase)));
        }

        public static string UkrainianStreamRewardCallback(CallbackArgs<OnRewardRedeemedArgs> args)
        {
            //TwitchHelpers.FulFillRedemption(args.e.ChannelId, args.e.RewardId.ToString(), args.e.RedemptionId.ToString());
            var rewardTimespan = TimeSpan.FromMinutes(15);
            args.bot.BotLang = "ua";
            Task.Delay(rewardTimespan).ContinueWith(t => args.bot.BotLang = "ru");
            return null;
        }
        #endregion Rewards callbacks

        #region Custom events callbacks
        public static void KiraAdditionalOnMessageReceived(CallbackArgs<OnMessageReceivedArgs> args)
        {
            //chatMessages.Enqueue(e.ChatMessage);
            //if (chatMessages.Count > SAVEDCHATMESSAGESNUM) chatMessages.Dequeue();
            if (timeoutUserBelowData.flag && !args.e.ChatMessage.IsModerator && !args.e.ChatMessage.IsBroadcaster)
            {
                var timeout = TimeSpan.FromTicks(TimeSpan.FromMinutes(10).Ticks * timeoutUserBelowData.num);
                KiraTimeoutUser(args.bot, args.e.ChatMessage.Username, timeout,
                    "Награда за баллы канала - \"Таймач человеку снизу\"");
                timeoutUserBelowData = default;
            }
        }

        public static void KiraOnWhisperReceived(CallbackArgs<OnWhisperReceivedArgs> args)
        {
            var senderId = args.e.WhisperMessage.UserId;
            var isSubscriber = TwitchHelpers.IsSubscribeToChannel(args.bot.UserId, senderId, args.bot.HelperBot.AccessToken);
            if (isSubscriber && whoCanSendFromTimeout.Contains(args.e.WhisperMessage.Username, StringComparer.OrdinalIgnoreCase))
            {
                args.bot.twitchClient.SendMessage(args.bot.ChannelName,
                    $"{args.e.WhisperMessage.Username} передаёт: {args.e.WhisperMessage.Message}");
                whoCanSendFromTimeout.RemoveAll(n =>
                    n.Equals(args.e.WhisperMessage.Username, StringComparison.InvariantCultureIgnoreCase));
            }
        }

        public static void KiraOnCommunitySubscription(CallbackArgs<OnCommunitySubscriptionArgs> args)
        {
            var e = args.e;
            massGifts = e.GiftedSubscription.MsgParamMassGiftCount;
            var answer = e.GiftedSubscription.MsgParamMassGiftCount == 1
                ? $"{e.GiftedSubscription.DisplayName}, спасибо за подарочную подписку! PrideFlower"
                : $"{e.GiftedSubscription.DisplayName}, спасибо за подарочные подписки! peepoLove peepoLove peepoLove";
            args.bot.twitchClient.SendMessage(e.Channel, answer);
        }

        public static void KiraOnGiftedSubscription(CallbackArgs<OnGiftedSubscriptionArgs> args)
        {
            if (massGifts > 0) massGifts--;
            else
            {
                var e = args.e;
                var kiraChannelBot = args.bot;
                string answer;
                if (string.Equals(e.GiftedSubscription.MsgParamRecipientDisplayName, Config.BotUsername,
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var loveSmile =
                        TwitchHelpers.IsSubscribeToChannel(kiraChannelBot.UserId, Config.BotId,
                            kiraChannelBot.HelperBot.AccessToken)
                            ? "kupaLove"
                            : ":heart:";
                    answer = $"спасибо большое за подписку мне {loveSmile} {loveSmile} {loveSmile}";
                }
                else
                {
                    answer =
                        $"спасибо за подарочную подписку для {e.GiftedSubscription.MsgParamRecipientDisplayName}! peepoLove";
                }

                kiraChannelBot.twitchClient.SendMessage(e.Channel, $"{e.GiftedSubscription.DisplayName}, {answer}");
            }
        }

        public static void KiraOnReSubscriber(CallbackArgs<OnReSubscriberArgs> args)
        {
            var e = args.e;
            var bot = args.bot;
            bot.twitchClient.SendMessage(e.Channel,
                $"{e.ReSubscriber.DisplayName}, спасибо за продление подписки! Poooound");
        }

        public static void KiraOnNewSubscriber(CallbackArgs<OnNewSubscriberArgs> args)
        {
            var e = args.e;
            var kiraChannelBot = args.bot;

            var passportEmote =
                TwitchHelpers.IsSubscribeToChannel(kiraChannelBot.UserId, Config.BotId,
                    kiraChannelBot.HelperBot.AccessToken)
                    ? "kupaPasport"
                    : "";
            kiraChannelBot.twitchClient.SendMessage(e.Channel,
                $"{e.Subscriber.DisplayName}, спасибо за подписку! bleedPurple Давайте сюда Ваш паспорт FBCatch {passportEmote}");
            kiraChannelBot.twitchClient.SendMessageWithDelay(e.Channel, "!саб", TimeSpan.FromSeconds(2));
        }

        public static void KiraOnUserTimedout(CallbackArgs<OnUserTimedoutArgs> args)
        {
            var e = args.e;
            var kiraChannelBot = args.bot;

            var dict = new Dictionary<string, (bool, string)>
            {
                { "th3globalist", (false, "74415861") },
                { "rinael_lapki", (true, "116926816") }
            };

            var username = e.UserTimeout.Username;
            if (dict.ContainsKey(username))
            {
                var value = dict[username];
                var another = !value.Item1;
                var infoToBan = dict.FirstOrDefault(x => x.Value.Item1 == another);

                if (args.bot.HelperBot == null) return;
                if (TwitchHelpers.IsUserTimedOut(args.bot.UserId, infoToBan.Value.Item2, args.bot.HelperBot.AccessToken)) return;

                kiraChannelBot.twitchClient.TimeoutUser(kiraChannelBot.ChannelName, infoToBan.Key, TimeSpan.FromSeconds(e.UserTimeout.TimeoutDuration), $"Вместе со второй личностью {username}");
            }
        }
        #endregion Custom events callbacks
    }
}
