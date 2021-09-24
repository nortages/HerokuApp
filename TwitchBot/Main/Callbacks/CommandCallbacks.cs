using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Main.Hearthstone;
using TwitchBot.Models;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;

// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

namespace TwitchBot.Main.Callbacks
{
    public class CommandCallbacks
    {
        private static readonly List<DuelOffer> DuelOffersPendingAcceptance = new();
        
        private static readonly Dictionary<string, string> FrogWordToKira = new()
        {
            {"лягушачьем", "Кирином"},
            {"лягушка", "Кира"},
            {"лягушачьих", "Кириных"},
            {"лягушкам", "Кирам"},
            {"лягушонки", "Кирёнки"},
            {"лягушачьи", "Кирины"},
            {"лягушкой", "Кирой"},
            {"лягушачья", "Кирина"},
            {"лягушке", "Кире"},
            {"лягушки", "Киры"},
            {"лягушек", "Кир"},
            {"лягушку", "Киру"},
            {"лягушинный", "Кирин"},
            {"лягушком", "Кириллом"},
            {"лягушками", "Кирами"},
                
            {"жаб", "Кир"},
            {"жабий", "Кирин"},
            {"жабы", "Киры"},
            {"жаба", "Кира"},
            {"жабьи", "Кирины"},
            {"жабье", "Кирино"},
            {"жабами", "Кирами"},
            {"жабоидные", "Кироидные"},
            {"жабой", "Кирой"},
            {"жабу", "Киру"},
        };
        
        public static string FactCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            if (e.Command.CommandText is not "кирафакт" or "кирофакт") return null;
            
            var currentOption = args.Option;
            var answer = currentOption.GetAnswerFromOptions(s, e, args);
            
            foreach (var frogWord in FrogWordToKira.Keys)
            {
                answer = answer.Replace(frogWord, FrogWordToKira[frogWord], StringComparison.OrdinalIgnoreCase);
            }

            return answer;
        }
        
        public static string GetCommandsCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var currentOption = args.Option;
            var baseUrl = MainBotService.CurrentEnvironment.IsProduction() ?
                "https://nortages-twitch-bot.herokuapp.com" : "http://localhost:5000";
            var multiLangAnswer = currentOption.GetMultiLangAnswer(args);
            return string.Format(multiLangAnswer, baseUrl, args.ChannelBotInfo.ChannelUsername.ToLower());
        }

        public static string WaitingStreamCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = args.Option;
            var isAnyArguments = !string.IsNullOrEmpty(command.ArgumentsAsString);
            var channelName = isAnyArguments ? command.ArgumentsAsString.TrimStart('@').ToLower() : args.ChannelBotInfo.ChannelUsername;
            var channelBotInfo = args.ChannelBotInfo;

            var isThisChannel = string.Equals(channelName, args.ChannelBotInfo.ChannelUsername, MainBotService.StringComparison);
            var channelId = isThisChannel ? channelBotInfo.ChannelUserId : MainBotService.BotTwitchHelpers.GetIdByUsername(channelName);
            
            string answer;
            var isStreamUp = MainBotService.BotTwitchHelpers.IsStreamUp(channelId);
            if (isStreamUp)
            {
                answer = currentOption.ChildOptions.Single(n => n.Name == "stream_on").GetAnswer(s, e, args);
            }
            else
            {
                var result = MainBotService.BotTwitchHelpers.GetElapsedTimeFromLastStream(channelId);
                if (result != null)
                {
                    var timespan = result.Value;
                    var timespanPart = UtilityFunctions.FormatTimespan(timespan);
                    answer = currentOption.ChildOptions.Single(n => n.Name == "waiting_for_stream").GetAnswer(s, e, args);
                    answer = string.Format(answer, channelName, timespanPart);
                }
                else
                {                    
                    answer = currentOption.ChildOptions.Single(n => n.Name == "not_streamer").GetAnswer(s, e, args);
                }
            }
            return answer;
        }

        public static string BoatCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var answer = "";
            var channel = command.ChatMessage.Channel;
            var username = command.ChatMessage.Username;

            var assessmentOptions = new List<string> { "слабовато чел...", "а ты скилловый Jebaited" };
            var pirates = new List<MinionInfo>();
            string assessment;

            MinionInfo specialPirate = null;
            var allPirates = MainBotService.HearthstoneApiClient
                .GetBattlegroundsMinions(MinionType.Pirate, notImplemented: true).ToList();
            if (command.ArgumentsAsList.Count != 0 && command.ArgumentsAsList[0] == "триплет" && e.Command.ChatMessage.IsMeOrBroadcaster())
            {
                specialPirate = command.ArgumentsAsList[1] switch
                {
                    "элизы" => allPirates.Single(n => n.Id == 61047),
                    "амальгадона" => allPirates.Single(n => n.Id == 61444),
                    _ => allPirates[Program.Rand.Next(0, allPirates.Count)]
                };
            }

            for (var i = 0; i < 3; i++)
            {
                var pirate = specialPirate ?? allPirates[Program.Rand.Next(0, allPirates.Count)];
                pirates.Add(pirate);
                answer += $"{pirate}, ";
            }

            answer = answer.TrimEnd(' ', ',');
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
                        if (!(command.ChatMessage.IsModerator))
                        {
                            args.ChannelBot.ChannelTwitchClient.SendMessage(channel, $"/vip {username}");
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

        public static string OfferCombatCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            return PerformDuel(e.Command, args, isCombat: true);
        }

        public static string OfferDuelCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            return PerformDuel(e.Command, args);
        }

        public static string AcceptDuelCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var username = command.ChatMessage.Username;
            var argsAsStr = command.ArgumentsAsString;
            DuelOffer offerToAccept;
            if (!string.IsNullOrEmpty(argsAsStr))
            {
                offerToAccept = DuelOffersPendingAcceptance
                    .Where(n => string.Equals(n.WhomIsOffered, username, MainBotService.StringComparison))
                    .SingleOrDefault(n => string.Equals(n.WhoOffers, argsAsStr, MainBotService.StringComparison));
            }
            else
            {
                offerToAccept = DuelOffersPendingAcceptance.FirstOrDefault(n => string.Equals(n.WhomIsOffered, username, MainBotService.StringComparison));
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

            if (string.IsNullOrEmpty(whomIsOffered)) 
                return null;

            if (string.Equals(whoOffers, whomIsOffered, MainBotService.StringComparison)) 
                return $"@{whoOffers} нельзя вызвать самого себя на дуэль Kappa";

            var newOffer = new DuelOffer(whoOffers, whomIsOffered, isCombat);
            if (string.Equals(whomIsOffered, MainBotService.BotUsername, MainBotService.StringComparison))
            {
                // Ex. 778-40428|49279
                const string numbersPart = @"((\d{3,})-?)*";
                var regex = new Regex($@"{numbersPart}\|{numbersPart}");
                Tuple<int[], int[]> specialMinionsIds = null;
                if (args.ChannelBotInfo.IsTestMode && argsAsList.Count >= 2 && regex.IsMatch(argsAsList[1]))
                {
                    var allIds = argsAsList[1].Split("|");
                    var allIdsParsed = allIds.Select(n => n.Split("-").Select(int.Parse).ToArray()).ToArray();
                    specialMinionsIds = Tuple.Create(allIdsParsed[0], allIdsParsed[1]);
                }
                return isCombat ? PerformCombat(newOffer, command, args) : PerformDuel(newOffer, specialMinionsIds);
            }

            if (DuelOffersPendingAcceptance.Contains(newOffer))
                return null;

            DuelOffersPendingAcceptance.Add(newOffer);
            Task.Delay(TimeSpan.FromSeconds(30)).ContinueWith( _ =>
            {
                if (!DuelOffersPendingAcceptance.Contains(newOffer)) 
                    return;
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
            if (!args.ChannelBotInfo.IsTestMode) UtilityFunctions.TimeoutCommandUser(command, args, TimeSpan.FromMinutes(5), playerWhoLost, "Проиграл в сражении в миниигре");
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

        public static string RadishTiredOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = args.Option;
            var username = command.ChatMessage.Username;
            var channel = command.ChatMessage.Channel;
            var childOption = currentOption.ChildOptions.GetRandProbableOption();
            var answer = childOption.GetAnswer(s, e, args);
            var numOfUsing = currentOption.ParentOption.UsageFrequency[username];
            
            var timesWord = "раз";
            var endings = args.ChannelBotInfo.Lang == Lang.ua ? new[] { "", "и", "iв" } : new[] { "", "а", "" };
            timesWord += UtilityFunctions.GetWordEnding(numOfUsing, endings);
            answer = string.Format(answer, numOfUsing, timesWord);
            
            if (childOption.Id != 0) return answer;
            var timeoutTime = TimeSpan.FromMinutes(1);
            if (command.ChatMessage.IsModerator)
            {
                args.ChannelBot.ChannelTwitchClient.TimeoutModer(channel, username, timeoutTime);
            }
            else
            {
                MainBotService.BotTwitchClient.TimeoutUser(channel, username, timeoutTime);
            }
            
            return answer;
        }

        public static string RadishTransformsOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = args.Option;
            
            Option childOption;
            var username = command.ChatMessage.Username;
            if (currentOption.UsageFrequency.ContainsKey(username))
            {
                if (currentOption.UsageFrequency[username] >= 9)
                {
                    childOption = currentOption.ChildOptions.GetRandProbableOption();
                    if (childOption == currentOption.ChildOptions.ElementAt(1))
                    {
                        currentOption.UsageFrequency[username] = 0;
                    }
                }
                else
                {
                    childOption = currentOption.ChildOptions.ElementAt(0);
                }
            }
            else
            {
                currentOption.UsageFrequency.Add(username, 0);
                childOption = currentOption.ChildOptions.ElementAt(0);
            }
            currentOption.UsageFrequency[username]++;
            var answer = childOption.GetAnswer(s, e, args);
            return answer;
        }

        public static string RadishDetonatorOptionCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            var currentOption = args.Option;
            var currentCommand = currentOption.ParentOption.Command;
            
            Option optionInfo;
            var username = command.ChatMessage.Username;
            if (currentOption.UsageFrequency.ContainsKey(username))
            {
                if (currentCommand.ChannelCommand.LastOption[username] == currentOption)
                {
                    optionInfo = currentOption.ChildOptions.GetRandProbableOption();
                }
                else
                {
                    optionInfo = currentOption.ChildOptions.ElementAt(0);
                }
            }
            else
            {
                currentOption.UsageFrequency.Add(username, 0);
                optionInfo = currentOption.ChildOptions.ElementAt(0);
            }
            currentOption.UsageFrequency[username]++;
            var answer = optionInfo.GetAnswer(s, e, args);
            
            return answer;
        }

        public static string CardCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var chatCommand = e.Command;
            var currentOption = args.Option;

            Option childOption;
            if (args.ChannelBotInfo.IsTestMode && int.TryParse(chatCommand.ArgumentsAsString, out var childOptionId))
            {
                childOption = currentOption.ChildOptions.SingleOrDefault(n => n.Id == childOptionId);
                if (childOption == null) return "карты с таким id не существует";
            }
            else
            {
                childOption = currentOption.ChildOptions.GetRandProbableOption();
            }

            args.Option = childOption;
            var cardDescription = childOption.GetAnswer(s, e, args);
            
            if (childOption.Name == "59935")
            {
                UtilityFunctions.TimeoutCommandUser(e.Command, args, TimeSpan.FromMinutes(10));
            }
            
            return cardDescription;
        }

        // public static string ManulCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        // {
        //     var manulsNum = MainBotService.CommandsData.Value<int>("manuls");
        //     manulsNum++;
        //     MainBotService.CommandsData["manuls"] = manulsNum;
        //     var manulWord = e.Command.CommandText;
        //     manulWord += UtilityFunctions.GetWordEnding(manulsNum, new[] { "", "а", "ов" });
        //     var answer = $"{manulsNum} {manulWord}";
        //
        //     return answer;
        // }

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
            var isNickname = false;

            var inputStringCases = new bool[inputString.Length];
            for (var i = 0; i < inputString.Length; i++)
            {
                inputStringCases[i] = char.IsUpper(inputString[i]);
            }
            var inputStringLower = inputString.ToLower();
            
            for (var i = 0; i < inputStringLower.Length; i++)
            {
                var ch = inputStringLower[i];
                if (ch == '@')
                {
                    isNickname = true;
                    outputString += ch;
                    continue;
                }

                if (isNickname)
                {
                    outputString += ch;
                    if (new[]{' ', ','}.Contains(ch))
                        isNickname = false;
                    continue;
                }

                var charToAdd = latinToCyrillic.TryGetValue(ch, out var fixedChar) ? fixedChar : ch;
                if (inputStringCases[i])
                    charToAdd = char.ToUpper(charToAdd);
                outputString += charToAdd;
            }

            return outputString;
        }
        
        public static void ToggleUkrainianStreamCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            args.ChannelBotInfo.Lang = Lang.ua;
        }

        public static void WhisperMeCommandCallback(object s, OnChatCommandReceivedArgs e, CallbackArgs args)
        {
            var command = e.Command;
            MainBotService.BotTwitchClient.SendMessage(args.ChannelBotInfo.ChannelUsername, $"/w {command.ChatMessage.DisplayName} Test");
        }
        
        public static void ToggleTestModePrivateCommandCallback(object s, OnWhisperCommandReceivedArgs e, CallbackArgs args)
        {
            var channelBotInfo = args.DbContext.ChannelBots.SingleOrDefault(c =>
                    string.Equals(c.ChannelUsername, e.Command.ArgumentsAsString, MainBotService.StringComparison));
            if (channelBotInfo == null)
                return;
            
            channelBotInfo.IsTestMode = !channelBotInfo.IsTestMode;
            args.DbContext.Update(args.ChannelBotInfo);
        }
        
        public static void ChangeLangPrivateCommandCallback(object s, OnWhisperCommandReceivedArgs e, CallbackArgs args)
        {
            var commandArgs = e.Command.ArgumentsAsList;
            var channelBotInfo = args.DbContext.ChannelBots.SingleOrDefault(c =>
                EF.Functions.ILike(c.ChannelUsername, commandArgs[1]));
            if (channelBotInfo == null)
                return;
            
            channelBotInfo.Lang = Enum.TryParse<Lang>(commandArgs[0], out var result) ? result : Lang.ru;
            args.DbContext.Update(channelBotInfo);
        }
    }
}
