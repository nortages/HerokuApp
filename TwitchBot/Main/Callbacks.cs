using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.PubSub.Events;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchBot.Main.DonationAlerts;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Main.Hearthstone;
using TwitchLib.Client;
using TwitchLib.Client.Models;
// ReSharper disable StringLiteralTypo

namespace TwitchBot.Main
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public static class Callbacks
    {
        private static int _massGifts;
        private static bool _flag;
        private static (bool flag, int num) _timeoutUserBelowData = (false, 0);
        private static readonly List<string> WhoCanSendFromTimeout = new();
        private static readonly List<DuelOffer> DuelOffersPendingAcceptance = new();
        private static readonly Dictionary<string, List<Tuple<Board, Board>>> BgDuelsRecords = new();
        
        #region Commands callbacks
        [CallbackInfo(Id = "get_commands_command")]
        public static string GetCommandsCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var currentOption = (OptionInfo)s;
            var baseUrl = Config.IsDeployed ? "https://nortagesbot.herokuapp.com" : "http://localhost:5000";
            return string.Format(currentOption.GetMultiLangAnswer(args.Lang), baseUrl, args.Bot.ChannelName.ToLower());
        }

        [CallbackInfo(Id = "waiting_stream_command")]
        public static string WaitingStreamCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = (OptionInfo)s;
            var isAnyArguments = !string.IsNullOrEmpty(command.ArgumentsAsString);
            var channelName = isAnyArguments ? command.ArgumentsAsString.TrimStart('@').ToLower() : args.Bot.ChannelName;

            var isThisChannel = string.Equals(channelName, args.Bot.ChannelName, Config.StringComparison);
            var channelId = isThisChannel ? args.Bot.ChannelUserId : TwitchHelpers.GetIdByUsername(channelName);
            
            string answer;
            var isStreamUp = TwitchHelpers.IsStreamUp(channelId);
            if (isStreamUp)
            {
                answer = currentOption.Options.Single(n => n.Id == "stream_on").GetMultiLangAnswer(args.Lang);
            }
            else
            {
                var result = TwitchHelpers.GetLastVideoDate(channelId);
                if (result != null)
                {
                    var timespan = result.Value;
                    var timespanPart = UtilityFunctions.FormatTimespan(timespan);
                    answer = currentOption.Options.Single(n => n.Id == "waiting").GetMultiLangAnswer(args.Lang);
                    answer = string.Format(answer, channelName, timespanPart);
                }
                else
                {                    
                    answer = currentOption.Options.Single(n => n.Id == "not_streamer").GetMultiLangAnswer(args.Lang);
                }
            }
            return answer;
        }

        [CallbackInfo(Id = "boat_command")]
        public static string BoatCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var answer = "";
            var channel = command.ChatMessage.Channel;
            var username = command.ChatMessage.Username;

            var assessmentOptions = new List<string> { "слабовато чел...", "а ты скилловый Jebaited" };
            var pirates = new List<MinionInfo>();
            var assessment = "";

            MinionInfo specialPirate = null;
            var allPirates = HearthstoneApiClient.GetBattlegroundsMinions(MinionType.Pirate, notImplemented: true).ToList();
            if (command.ArgumentsAsList.Count != 0 && command.ArgumentsAsList[0] == "триплет" && Bot.IsMeOrBroadcaster(e))
            {
                if (command.ArgumentsAsList[1] == "элизы")
                {
                    specialPirate = allPirates.Single(n => n.Id == 61047);
                }
                else
                {
                    specialPirate = allPirates[Program.Rand.Next(0, allPirates.Count)];
                }
            }

            for (var i = 0; i < 3; i++)
            {
                var pirate = specialPirate ?? allPirates[Program.Rand.Next(0, allPirates.Count)];
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
                        UtilityFunctions.TimeoutCommandUser(e.Command, args, TimeSpan.FromMinutes(30), "Выпало 3 Элизы с команды !лодка");
                        break;
                    // Check for Amalgadon
                    case 61444:
                        assessment = "777, ЛОВИ ВИПКУ";
                        if (!(command.ChatMessage.IsModerator || command.ChatMessage.IsVip))
                        {
                            args.Bot.StreamerBot.TwitchClient.SendMessage(channel, $"/vip {username}");
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
                const int maxMark = 57; // Elisa x3
                // Assess pirates
                var mark = pirates.Sum(pirate => pirate.Attack + pirate.Health + pirate.Battlegrounds.Tier);
                var quotient = (double)mark / maxMark;
                assessment = assessmentOptions[quotient > 0.6 ? 1 : 0];
            }
            return $"YEP {answer} YEP , {assessment}";
        }

        [CallbackInfo(Id = "offer_combat_command")]
        public static string OfferCombatCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            return PerformDuel(e.Command, args, isCombat: true);
        }

        [CallbackInfo(Id = "offer_duel_command")]
        public static string OfferDuelCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            return PerformDuel(e.Command, args);
        }

        [CallbackInfo(Id = "accept_duel_command")]
        public static string AcceptDuelCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var username = command.ChatMessage.Username;
            var argsAsStr = command.ArgumentsAsString;
            DuelOffer offerToAccept;
            if (!string.IsNullOrEmpty(argsAsStr))
            {
                offerToAccept = DuelOffersPendingAcceptance
                    .Where(n => string.Equals(n.WhomIsOffered, username, Config.StringComparison))
                    .SingleOrDefault(n => string.Equals(n.WhoOffers, argsAsStr, Config.StringComparison));
            }
            else
            {
                offerToAccept = DuelOffersPendingAcceptance.FirstOrDefault(n => string.Equals(n.WhomIsOffered, username, Config.StringComparison));
            }
            if (offerToAccept == null) return null;

            DuelOffersPendingAcceptance.Remove(offerToAccept);

            return offerToAccept.IsCombat ? PerformCombat(offerToAccept, e.Command, args) : PerformDuel(offerToAccept);
        }

        private static string PerformDuel(ChatCommand command, CallbackArgs args, bool isCombat = false)
        {
            var whoOffers = command.ChatMessage.Username;
            string whomIsOffered = null;
            var argsAsList = command.ArgumentsAsList;

            if (argsAsList.Count > 0) whomIsOffered = argsAsList[0].TrimStart('@');

            if (string.IsNullOrEmpty(whomIsOffered)) return null;

            if (string.Equals(whoOffers, whomIsOffered, Config.StringComparison)) return $"@{whoOffers} нельзя вызвать самого себя на дуэль Kappa";

            var newOffer = new DuelOffer(whoOffers, whomIsOffered, isCombat);
            if (string.Equals(whomIsOffered, Config.BotUsername, Config.StringComparison))
            {
                // Ex. 778-40428|49279
                const string numbersPart = @"((\d{3,})-?)*";
                var regex = new Regex($@"{numbersPart}\|{numbersPart}");
                Tuple<int[], int[]> specialMinionsIds = null;
                if (args.IsTestMode && argsAsList.Count >= 2 && regex.IsMatch(argsAsList[1]))
                {
                    var allIds = argsAsList[1].Split("|");
                    var allIdsParsed = allIds.Select(n => n.Split("-").Select(int.Parse).ToArray()).ToArray();
                    specialMinionsIds = Tuple.Create(allIdsParsed[0], allIdsParsed[1]);
                }
                return isCombat ? PerformCombat(newOffer, command, args) : PerformDuel(newOffer, specialMinionsIds);
            }

            if (DuelOffersPendingAcceptance.Contains(newOffer)) return null;

            DuelOffersPendingAcceptance.Add(newOffer);
            Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith(t =>
            {
                if (!DuelOffersPendingAcceptance.Contains(newOffer)) return;
                DuelOffersPendingAcceptance.Remove(newOffer);
            });
            return null;
        }
        
        private static string PerformCombat(DuelOffer offer, ChatCommand command, CallbackArgs args)
        {
            var player1 = offer.WhoOffers;
            var player2 = offer.WhomIsOffered;

            var standings = new Dictionary<string, int> {
                { player1, 0 },
                { player2, 0 },
            };

            var startMinionsOnEachRound = new List<Tuple<MinionInfo, MinionInfo>>();
            do
            {
                var round = new BattlegroundsRound(offer.WhoOffers, offer.WhomIsOffered);
                var startMinions = round.SummonStartMinions();
                startMinionsOnEachRound.Add(startMinions);
                var outcome = round.Play();
                if (outcome == Outcome.Tie)
                {
                    standings[player1]++;
                    standings[player2]++;
                } else
                {
                    var playerWhoWon = outcome == Outcome.Win ? player1 : player2;
                    standings[playerWhoWon]++;
                }
            } while (!(standings.Any(n => n.Value >= 2) && standings[player1] != standings[player2]));

            var ordered = standings.OrderBy(k => k.Value).ToArray();
            var playerWhoLost = ordered.First().Key;
            if (!args.IsTestMode) UtilityFunctions.TimeoutCommandUser(command, args, TimeSpan.FromMinutes(5), playerWhoLost, "Проиграл в сражении в миниигре");
            var playerWhoWin = ordered.Last().Key;
            var answer = $"{player1} бросил вызов {player2}! ";
            for (var i = 0; i < startMinionsOnEachRound.Count; i++)
            {
                answer += $"{i + 1}й раунд - {startMinionsOnEachRound[i].Item1} vs {startMinionsOnEachRound[i].Item2}, ";
            }
            answer = answer.TrimEnd(new char[] { ',', ' ' });
            return $"{answer}. Победил {playerWhoWin} со счётом {standings[playerWhoWin]}:{standings[playerWhoLost]}!";
        }

        private static string PerformDuel(DuelOffer offer, Tuple<int[], int[]> specialMinionsIds = null)
        {
            var round = new BattlegroundsRound(offer.WhoOffers, offer.WhomIsOffered);

            var minions = round.SummonStartMinions(specialMinionsIds);
            if (minions == null) return null;
            var (minion1, minion2) = minions;

            var outcome = round.Play();
            BgDuelsRecords.Add(round.Id, round.BoardStatesDuringRound);            

            string result;
            if (outcome == Outcome.Tie)
            {
                result = "Ничья!";
            }
            else
            {
                result = $"Победил {(outcome == Outcome.Win ? offer.WhoOffers : offer.WhomIsOffered)}!";
            }
            return $"{offer.WhoOffers} выпало {minion1}, а {offer.WhomIsOffered} - {minion2}! {result}";
        }

        [CallbackInfo(Id = "radish_tired")]
        public static string RadishTiredOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = (OptionInfo)s;
            var username = command.ChatMessage.Username;
            var channel = command.ChatMessage.Channel;
            var childOption = currentOption.Options.GetRandProbableOption();
            var answer = childOption.GetAnswer(s, e, args);
            var numOfUsing = currentOption.Parent.UsageFrequency[username];
            var timesWord = "раз";
            var endings = args.Lang == "ua" ? new string[] { "", "и", "iв" } : new string[] { "", "а", "" };
            timesWord += UtilityFunctions.GetWordEnding(numOfUsing, endings);
            answer = string.Format(answer, numOfUsing, timesWord);
            
            if (childOption.Id != "timeout") return answer;
            var timeoutTime = TimeSpan.FromMinutes(1);
            if (command.ChatMessage.IsModerator)
            {
                args.Bot.StreamerBot.TwitchClient.TimeoutModer(channel, username, timeoutTime);
            }
            else
            {
                args.Bot.TwitchClient.TimeoutUser(channel, username, timeoutTime);
            }
            return answer;
        }

        [CallbackInfo(Id = "radish_wants_to_talk")]
        public static string RadishTalksOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var currentOption = (OptionInfo)s;
            var childOption = (OptionInfo)currentOption.Options.GetRandProbableOption();
            return string.Format(currentOption.GetMultiLangAnswer(args.Lang), childOption.GetAnswer(s, e, args));
        }

        [CallbackInfo(Id = "radish_transforms")]
        public static string RadishTransformsOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = (OptionInfo)s;
            OptionInfo optionInfo;
            var username = command.ChatMessage.Username;
            if (currentOption.UsageFrequency.ContainsKey(username))
            {
                if (currentOption.UsageFrequency[username] >= 9)
                {
                    optionInfo = (OptionInfo)currentOption.Options.GetRandProbableOption();
                    if (optionInfo == currentOption.Options[1])
                    {
                        currentOption.UsageFrequency[username] = 0;
                    }
                }
                else
                {
                    optionInfo = (OptionInfo)currentOption.Options[0];
                }
            }
            else
            {
                currentOption.UsageFrequency.Add(username, 0);
                optionInfo = (OptionInfo)currentOption.Options[0];
            }
            currentOption.UsageFrequency[username]++;
            var answer = optionInfo.GetAnswer(s, e, args);
            return answer;
        }

        [CallbackInfo(Id = "radish_detonator")]
        public static string RadishDetonatorOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = (OptionInfo)s;
            var currentCommand = (CommandInfo)currentOption.Parent;
            OptionInfo optionInfo;
            var username = command.ChatMessage.Username;
            if (currentOption.UsageFrequency.ContainsKey(username))
            {
                if (currentCommand.LastOption[username] == currentOption)
                {
                    optionInfo = currentOption.Options.GetRandProbableOption();
                }
                else
                {
                    optionInfo = currentOption.Options[0];
                }
            }
            else
            {
                currentOption.UsageFrequency.Add(username, 0);
                optionInfo = (OptionInfo)currentOption.Options[0];
            }
            currentOption.UsageFrequency[username]++;
            var answer = optionInfo.GetAnswer(s, e, args);
            return answer;
        }

        [CallbackInfo(Id = "throw_radish")]
        public static string ThrowRadishOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = (OptionInfo)s;
            var randChatter = TwitchHelpers.GetRandChatter(command.ChatMessage.Channel);
            var answer = currentOption.GetAnswerFromOptions(s, e, args);
            return string.Format(currentOption.GetMultiLangAnswer(args.Lang), answer, randChatter.Username);
        }

        [CallbackInfo(Id = "card_command")]
        public static string CardCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var chatCommand = e.Command;
            var command = (CommandInfo)s;
            if (command.Options == null)
            {
                var optionsPath = $"./Main/Profiles/th3globalist/card_command_options.json";
                command.Options = JArray.Parse(File.ReadAllText(optionsPath)).ToObject<List<OptionInfo>>();
                if (command.Options == null)
                    throw new InvalidOperationException($"Card command doesn't have any options.");
            }
            
            string id;
            OptionInfo childOptionInfo;
            if (args.IsTestMode && int.TryParse(chatCommand.ArgumentsAsString, out var customId))
            {
                id = customId.ToString();
                childOptionInfo = command.Options.SingleOrDefault(n => n.Id == id);
                if (childOptionInfo == null) return "карты с таким id не существует";
            }
            else
            {
                childOptionInfo = command.Options.GetRandProbableOption();
                id = childOptionInfo.Id;
            }
            s = childOptionInfo;
            var cardDescription = childOptionInfo.GetAnswer(s, e, args);
            
            if (id == "59935")
            {
                UtilityFunctions.TimeoutCommandUser(e.Command, args, TimeSpan.FromMinutes(10));
            }
            return cardDescription;
        }

        [CallbackInfo(Id = "manul_command")]
        public static string ManulCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var manulsNum = Config.CommandsData.Value<int>("manuls");
            manulsNum++;
            Config.CommandsData["manuls"] = manulsNum;
            var manulWord = e.Command.CommandText;
            manulWord += UtilityFunctions.GetWordEnding(manulsNum, new[] { "", "а", "ов" });
            var answer = $"{manulsNum} {manulWord}";

            // Temporarily disabled
            //var randNum = Program.Rand.Next(0, 10);
            //if (randNum == -1)
            //{
            //    manulsEasterEggs ??= Config.GetManulsEasterEggs();
            //    var notPlayed = manulsEasterEggs.Where(n => !n.Value<bool>("wasPlayed")).ToList();
            //    randNum = Program.Rand.Next(0, notPlayed.Count);
            //    var easterEgg = notPlayed[randNum];
            //    easterEgg["wasPlayed"] = true;
            //    Config.SaveManulsEasterEggs(manulsEasterEggs);
            //    answer += " " + easterEgg.Value<string>("link");
            //}

            return answer;
        }

        [CallbackInfo(Id = "fix_command")]
        public static string FixCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var inputString = e.Command.ArgumentsAsString;
            
            Dictionary<char, char> latinToCyrillic = new() 
            {
                {'`', 'ё'},
                {'q', 'й'},
                {'w', 'ц'},
                {'e', 'у'},
                {'r', 'к'},
                {'t', 'е'},
                {'y', 'н'},
                {'u', 'г'},
                {'i', 'ш'},
                {'o', 'щ'},
                {'p', 'з'},
                {'[', 'х'},
                {']', 'ъ'},
                {'a', 'ф'},
                {'s', 'ы'},
                {'d', 'в'},
                {'f', 'а'},
                {'g', 'п'},
                {'h', 'р'},
                {'j', 'о'},
                {'k', 'л'},
                {'l', 'д'},
                {';', 'ж'},
                {'\'', 'э'},
                {'z', 'я'},
                {'x', 'ч'},
                {'c', 'с'},
                {'v', 'м'},
                {'b', 'и'},
                {'n', 'т'},
                {'m', 'ь'},
                {',', 'б'},
                {'.', 'ю'},
                {'/', '.'}
            };
            
            var outputString = "";
            foreach (var ch in inputString)
            {
                outputString += latinToCyrillic[ch];
            }

            return outputString;
        }
        
        [CallbackInfo(Id = "ukrainian_stream_command")]
        public static void ToggleUkrainianStreamCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            args.Bot.BotLang = command.ArgumentsAsString switch
            {
                "on" => "ua",
                "off" => "ru",
                _ => args.Bot.BotLang
            };
        }

        [CallbackInfo(Id = "whisper_command")]
        public static string WhisperMeCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            args.Bot.TwitchClient.SendMessage(args.Bot.ChannelName, $"/w {command.ChatMessage.DisplayName} Test");
            return null;
        }

        [CallbackInfo(Id = "save_commands_info_command")]
        public static string SaveCommandsUsageCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            Config.SaveCommandsInfo();
            return null;
        }

            
        
        // public static string CastSpell(object s)
        // {
        //     var command = (CommandInfo)s;
        //     var spellName = command.Names.Last();
        //     
        // }
        
        
        // [CallbackInfo(Id = "accio_spell_command")]
        // public static string AccioSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     CastSpell(s);
        // }
        //
        // [CallbackInfo(Id = "aguamenti_spell_command")]
        // public static string AguamentiSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "alohomora_spell_command")]
        // public static string AlohomoraCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "arrestomomentum_spell_command")]
        // public static string ArrestomomentumCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "bombarda_spell_command")]
        // public static string BombardaSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "combatbolt_spell_command")]
        // public static string CombatboltSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "diffindo_spell_command")]
        // public static string DiffindoSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "expectopatronum_spell_command")]
        // public static string ExpectopatronumSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "finite_spell_command")]
        // public static string FiniteSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        //     
        // }
        //
        // [CallbackInfo(Id = "flipendo_spell_command")]
        // public static string FlipendoSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "incendio_spell_command")]
        // public static string IncendioSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        //
        // [CallbackInfo(Id = "riddikkulus_spell_command")]
        // public static string RiddikkulusSpellCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     
        // }
        
        #endregion Commands callbacks

        #region Rewards callbacks
        [CallbackInfo(Id = "timeout_user_below_reward")]
        public static void TimeoutUserBelowRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            _timeoutUserBelowData.flag = true;
            _timeoutUserBelowData.num++;
        }

        [CallbackInfo(Id = "timeout_myself_reward")]
        public static void TimeoutMyselfRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            var timeoutTime = TimeSpan.FromMinutes(10);
            KiraTimeoutUser(args.Bot, e.DisplayName, timeoutTime, "Награда за баллы канала - \"Таймач самому себе\"");
        }

        private static void KiraTimeoutUser(Bot bot, string username, TimeSpan timeoutTime, string timeoutMessage)
        {            
            bot.TwitchClient.TimeoutUser(bot.ChannelName, username, timeoutTime, timeoutMessage);
            var timeoutMinutes = timeoutTime.TotalMinutes;
            bot.Logger.LogInformation("{Username} was timedout on {TimeoutMinutes} minutes", username, timeoutMinutes);
            WhoCanSendFromTimeout.Add(username);
            Task.Delay(timeoutTime).ContinueWith(t => WhoCanSendFromTimeout.RemoveAll(n => string.Equals(n, username, StringComparison.OrdinalIgnoreCase)));
        }

        [CallbackInfo(Id = "ukrainian_stream_reward")]
        public static void UkrainianStreamRewardCallback(object s, OnRewardRedeemedArgs e, CallbackArgs args)
        {
            //TwitchHelpers.FulFillRedemption(args.e.ChannelId, args.e.RewardId.ToString(), args.e.RedemptionId.ToString());
            var rewardTimespan = TimeSpan.FromMinutes(15);
            args.Bot.BotLang = "ua";
            Task.Delay(rewardTimespan).ContinueWith(t => args.Bot.BotLang = "ru");
        }
        #endregion Rewards callbacks

        #region Custom events callbacks
        
        [CallbackInfo(Id = "KiraAdditionalOnMessageReceived")]
        public static void KiraAdditionalOnMessageReceived(object s, OnMessageReceivedArgs e, CallbackArgs args)
        {
            if (_timeoutUserBelowData.flag && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                var timeout = TimeSpan.FromTicks(TimeSpan.FromMinutes(10).Ticks * _timeoutUserBelowData.num);
                KiraTimeoutUser(args.Bot, e.ChatMessage.Username, timeout,
                    "Попал(-а) под награду за баллы канала - \"Таймач человеку снизу\"");
                _timeoutUserBelowData = default;
            }
        }

        [CallbackInfo(Id = "KiraOnWhisperReceived")]
        public static void KiraOnWhisperReceived(object s, OnWhisperReceivedArgs e, CallbackArgs args)
        {
            var whisperMessage = e.WhisperMessage;
            var senderId = whisperMessage.UserId;
            var isSubscriber = TwitchHelpers.IsSubscribeToChannel(args.Bot.ChannelUserId, senderId, args.Bot.StreamerBot.AccessToken);
            if (!isSubscriber ||
                !WhoCanSendFromTimeout.Contains(whisperMessage.Username, StringComparer.OrdinalIgnoreCase)
            ) return;
            
            args.Bot.TwitchClient.SendMessage(args.Bot.ChannelName,
                $"{whisperMessage.Username} передаёт: {whisperMessage.Message}");
            WhoCanSendFromTimeout.RemoveAll(n =>
                n.Equals(whisperMessage.Username, Config.StringComparison));
        }

        [CallbackInfo(Id = "KiraOnCommunitySubscription")]
        public static void KiraOnCommunitySubscription(object s, OnCommunitySubscriptionArgs e, CallbackArgs args)
        {  
            _massGifts = e.GiftedSubscription.MsgParamMassGiftCount;
            var answer = e.GiftedSubscription.MsgParamMassGiftCount == 1
                ? $"{e.GiftedSubscription.DisplayName}, спасибо за подарочную подписку! PrideFlower"
                : $"{e.GiftedSubscription.DisplayName}, спасибо за подарочные подписки! peepoLove peepoLove peepoLove";
            args.Bot.TwitchClient.SendMessage(e.Channel, answer);
        }

        [CallbackInfo(Id = "KiraOnGiftedSubscription")]
        public static void KiraOnGiftedSubscription(object s, OnGiftedSubscriptionArgs e, CallbackArgs args)
        {
            if (!string.Equals(e.GiftedSubscription.MsgParamRecipientDisplayName, Config.BotUsername,
                Config.StringComparison)) return;
            var answer = $"спасибо большое за подписку kupaLove kupaLove kupaLove";
            args.Bot.TwitchClient.SendMessage(e.Channel, $"{e.GiftedSubscription.DisplayName}, {answer}");
        }

        [CallbackInfo(Id = "KiraOnReSubscriber")]
        public static void KiraOnReSubscriber(object s, OnReSubscriberArgs e, CallbackArgs args)
        {
            var bot = args.Bot;
            bot.TwitchClient.SendMessage(e.Channel,
                $"{e.ReSubscriber.DisplayName}, спасибо за продление подписки! Poooound");
        }

        [CallbackInfo(Id = "KiraOnNewSubscriber")]
        public static void KiraOnNewSubscriber(object s, OnNewSubscriberArgs e, CallbackArgs args)
        { 
            var kiraChannelBot = args.Bot;

            //var passportEmote =
            //    TwitchHelpers.IsSubscribeToChannel(kiraChannelBot.UserId, Config.BotId,
            //        kiraChannelBot.HelperBot.AccessToken)
            //        ? "kupaPasport"
            //        : "";
            //kiraChannelBot.twitchClient.SendMessage(e.Channel,
            //    $"{e.Subscriber.DisplayName}, спасибо за подписку! bleedPurple Давайте сюда Ваш паспорт FBCatch {passportEmote}");
            kiraChannelBot.TwitchClient.SendMessageWithDelay(e.Channel, $"!саб @{e.Subscriber.DisplayName}", TimeSpan.FromSeconds(1));
        }
        
        [CallbackInfo(Id = "KiraOnUserTimedout")]
        public static void KiraOnUserTimedout(object s, OnUserTimedoutArgs e, CallbackArgs args)
        {
            var twoUsers = new List<Tuple<string, string>>
            {
                new("th3globalist", "74415861"),
                new("rinael_lapki", "116926816")
            };

            var username = e.UserTimeout.Username;
            if (twoUsers.All(n => n.Item1 != username)) return;
            if (_flag) return;
            
            var wasBannedIndex = twoUsers.FindIndex(n => n.Item1 == username);
            var (usernameToBan, userId) = twoUsers[twoUsers.Count - 1 - wasBannedIndex];
            if (args.Bot.StreamerBot == null) return;
            
            var isTimedout = TwitchHelpers.IsUserTimedOut(args.Bot.StreamerBot.ChannelUserId, userId, args.Bot.StreamerBot.AccessToken).Result;
            if (isTimedout) return;

            var reason = $"Вместе со второй личностью {username}";
            var timeoutTime = TimeSpan.FromSeconds(e.UserTimeout.TimeoutDuration);

            var isModerator = TwitchHelpers.IsUserModerator(args.Bot.ChannelUserId, userId, args.Bot.StreamerBot.AccessToken);
            Console.WriteLine($"Is {usernameToBan} a moderator? " + isModerator);
            if (isModerator)
            {
                if (args.Bot.StreamerBot?.TwitchClient == null) return;
                args.Bot.StreamerBot.TwitchClient.TimeoutModer(args.Bot.ChannelName, usernameToBan, timeoutTime, reason);
            }
            else
            {
                args.Bot.TwitchClient.TimeoutUser(args.Bot.ChannelName, usernameToBan, timeoutTime, reason);
            }

            _flag = true;
            Task.Delay(TimeSpan.FromSeconds(15)).ContinueWith(t => _flag = false);
        }
        
        [CallbackInfo(Id = "Kira_OnDonationReceived")]
        public static void Kira_OnDonationReceived(object s, OnDonationAlertArgs e, CallbackArgs args)
        {
            Console.WriteLine("A new donation!");
            Console.WriteLine(JsonConvert.SerializeObject(e));
            
            if (!e.IsShown) return;
            
            var username = string.IsNullOrEmpty(e.Username) ? "Аноним" : e.Username;
            var answer = $"/me {username} закинул(-а) {e.Amount} шекелей";
            switch (e.MessageType)
            {
                case DonationMessageType.Text when string.IsNullOrEmpty(e.Message):
                    answer += "!";
                    break;
                case DonationMessageType.Text:
                    answer += $" со словами: {e.Message}";
                    break;
                case DonationMessageType.Audio:
                    answer += ", записав аудио сообщение!";
                    break;
            }

            args.Bot.TwitchClient.SendMessage(args.Bot.ChannelName, answer);
        }
        #endregion Custom events callbacks
    }
}
