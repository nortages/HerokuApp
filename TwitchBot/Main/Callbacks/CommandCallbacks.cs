using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Main.Hearthstone;
using TwitchBot.Models;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;

// ReSharper disable UnusedMember.Global
// ReSharper disable StringLiteralTypo

namespace TwitchBot.Main.Callbacks
{
    public class CommandCallbacks
    {
        public static string FactCommandCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var currentOption = args.Option;
            var answer = currentOption.GetAnswerFromOptions(s, e, args);

            if (e.Command.CommandText is not ("кирафакт" or "кирофакт")) return answer + " OSFrog";
            ;

            var frogWordToKira =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(currentOption.AdditionalData);
            foreach (var frogWord in frogWordToKira!.Keys)
                answer = answer.Replace(frogWord, frogWordToKira[frogWord], StringComparison.OrdinalIgnoreCase);

            return answer + " PETTHEkupa";
        }

        public static string GetCommandsCommandCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var currentOption = args.Option;
            var baseUrl = BotService.CurrentEnvironment.IsProduction()
                ? "https://nortages-twitch-bot.herokuapp.com"
                : "http://localhost:5000";
            var multiLangAnswer = currentOption.GetMultiLangAnswer(args);

            return string.Format(multiLangAnswer, baseUrl, args.ChannelInfo.ChannelUsername.ToLower());
        }

        public static string WaitingStreamCommandCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var command = e.Command;
            var currentOption = args.Option;
            var isAnyArguments = !string.IsNullOrEmpty(command.ArgumentsAsString);
            var channelName = isAnyArguments
                ? command.ArgumentsAsString.TrimStart('@').ToLower()
                : args.ChannelInfo.ChannelUsername;
            var channelBotInfo = args.ChannelInfo;

            var isThisChannel =
                string.Equals(channelName, args.ChannelInfo.ChannelUsername, BotService.StringComparison);
            var channelId = isThisChannel
                ? channelBotInfo.ChannelUserId
                : BotService.BotTwitchHelpers.GetIdByUsername(channelName);

            string answer;
            if (channelId is null)
            {
                answer = currentOption.ChildOptions.Single(n => n.Name == "nickname_not_found").GetAnswer(s, e, args);
                return answer;
            }

            var isStreamUp = BotService.BotTwitchHelpers.IsStreamUp(channelId).Result;
            if (isStreamUp)
            {
                answer = currentOption.ChildOptions.Single(n => n.Name == "stream_on").GetAnswer(s, e, args);
                return answer;
            }

            var result = BotService.BotTwitchHelpers.GetElapsedTimeFromLastStream(channelId);
            if (result is not null)
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

            return answer;
        }

        public static string BoatCommandCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var command = e.Command;
            var answer = "";
            var channel = command.ChatMessage.Channel;
            var username = command.ChatMessage.Username;

            var assessmentOptions = new List<string> {"слабовато чел...", "а ты скилловый Jebaited"};
            var pirates = new List<MinionInfo>();
            string assessment;

            MinionInfo specialPirate = null;
            var allPirates = BotService.HearthstoneApiClient
                .GetBattlegroundsMinions(MinionType.Pirate, notImplemented: true).ToList();
            if (command.ArgumentsAsList.Count != 0 && command.ArgumentsAsList[0] == "триплет" &&
                e.Command.ChatMessage.IsMeOrBroadcaster())
                specialPirate = command.ArgumentsAsList[1] switch
                {
                    "элизы" => allPirates.Single(n => n.Id == 61047),
                    "амальгадона" => allPirates.Single(n => n.Id == 61444),
                    _ => allPirates[Program.Rand.Next(0, allPirates.Count)]
                };

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
                        UtilityFunctions.TimeoutCommandUser(e.Command, args, TimeSpan.FromMinutes(30),
                            "Выпало 3 Элизы с команды !лодка");
                        break;
                    // Check for Amalgadon
                    case 61444:
                        assessment = "777, ЛОВИ ВИПКУ";
                        if (!command.ChatMessage.IsModerator)
                            args.ChannelBot.ChannelTwitchClient.SendMessage(channel, $"/vip {username}");
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
                var quotient = (double) mark / maxMark;
                assessment = assessmentOptions[quotient > 0.6 ? 1 : 0];
            }

            return $"YEP {answer} YEP , {assessment}";
        }

        public static string RadishTiredOptionCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var command = e.Command;
            var currentOption = args.Option;
            var username = command.ChatMessage.Username;
            var channel = command.ChatMessage.Channel;
            var childOption = currentOption.ChildOptions.GetRandProbableOption();
            var answer = childOption.GetAnswer(s, e, args);
            var numOfUsing = currentOption.ParentOption.UsageFrequency.GetValueAndSetIfNotExists(username);

            var timesWord = "раз";
            var endings = args.ChannelInfo.Lang == Lang.ua ? new[] {"", "и", "iв"} : new[] {"", "а", ""};
            timesWord += UtilityFunctions.GetRussianWordEnding(numOfUsing, endings);
            answer = string.Format(answer, numOfUsing, timesWord);
            currentOption.ParentOption.UsageFrequency[username]++;

            if (childOption.Name == "timeout")
            {
                var timeoutTime = TimeSpan.FromMinutes(1);
                if (command.ChatMessage.IsModerator)
                    args.ChannelBot.ChannelTwitchClient.TimeoutModer(channel, username, timeoutTime);
                else
                    BotService.BotTwitchClient.TimeoutUser(channel, username, timeoutTime);    
            }

            return answer;
        }
 
        public static string RadishTransformsOptionCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var command = e.Command;
            var currentOption = args.Option;

            Option childOption;
            var username = command.ChatMessage.Username;
            currentOption.UsageFrequency.GetValueAndSetIfNotExists(username);

            if (currentOption.UsageFrequency[username] >= 5)
            {
                childOption = currentOption.ChildOptions.GetRandProbableOption();
                if (childOption == currentOption.ChildOptions.Single(c => c.Name == "into_golden_strawberry"))
                    currentOption.UsageFrequency[username] = 0;
            }
            else
            {
                childOption = currentOption.ChildOptions.Single(c => c.Name == "into_plain_strawberry");
            }

            currentOption.UsageFrequency[username]++;
            var answer = childOption.GetAnswer(s, e, args);

            return answer;
        }

        public static string RadishDetonatorOptionCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var chatCommand = e.Command;
            var currentOption = args.Option;
            var currentCommand = args.Command;

            Option optionInfo;
            var username = chatCommand.ChatMessage.Username;
            if (currentOption.UsageFrequency.ContainsKey(username))
            {
                if (args.UserChannelCommand.LastOption == currentOption)
                    optionInfo = currentOption.ChildOptions.GetRandProbableOption();
                else
                    optionInfo = currentOption.ChildOptions.Single(co => co.Name == "radish_detonator_option1");
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

        public static string CardCommandCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var chatCommand = e.Command;
            var currentOption = args.Option;

            Option childOption;
            if (args.ChannelInfo.IsTestMode)
            {
                childOption = currentOption.ChildOptions.SingleOrDefault(n => n.Name == chatCommand.ArgumentsAsString);
                if (childOption == null)
                    return "карты с таким id не существует";
            }
            else
            {
                childOption = currentOption.ChildOptions.GetRandProbableOption();
            }

            args.Option = childOption;
            var cardDescription = childOption.GetAnswer(s, e, args);

            if (childOption.Name == "59935")
                UtilityFunctions.TimeoutCommandUser(e.Command, args, TimeSpan.FromMinutes(10));

            return cardDescription;
        }

        public static string ManulCommandCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var multiLangAnswer = args.Option.MultiLangAnswer.SingleOrDefault(a => a.Lang == Lang.ru);
            var manulsNum = int.Parse(multiLangAnswer.Text);
            manulsNum++;
            multiLangAnswer.Text = manulsNum.ToString();

            var manulWord = e.Command.CommandText;
            manulWord += UtilityFunctions.GetRussianWordEnding(manulsNum, new[] {"", "а", "ов"});
            var answer = $"{manulsNum} {manulWord}";

            return answer;
        }

        // Translates gibberish written in Russian in English layout into Russian layout.
        public static string FixCommandCallback(object s, OnChatCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var inputString = e.Command.ArgumentsAsString;
            var outputString = "";
            var isNickname = false;
            var latinToCyrillic = JsonConvert.DeserializeObject<Dictionary<char, char>>(args.Option.AdditionalData);

            var inputStringCases = new bool[inputString.Length];
            for (var i = 0; i < inputString.Length; i++) inputStringCases[i] = char.IsUpper(inputString[i]);
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
                    if (new[] {' ', ','}.Contains(ch))
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

        public static void ToggleUkrainianStreamCommandCallback(object s, OnChatCommandReceivedArgs e,
            CommandCallbackArgs args)
        {
            args.ChannelInfo.Lang = Lang.ua;
        }

        public static void ToggleTestModePrivateCommandCallback(object s, OnWhisperCommandReceivedArgs e,
            CommandCallbackArgs args)
        {
            var channelBotInfo = args.DbContext.ChannelInfos.SingleOrDefault(c =>
                EF.Functions.ILike(c.ChannelUsername, e.Command.ArgumentsAsString));
            if (channelBotInfo == null)
                return;

            channelBotInfo.IsTestMode = !channelBotInfo.IsTestMode;
        }

        public static void ChangeLangPrivateCommandCallback(object s, OnWhisperCommandReceivedArgs e, CommandCallbackArgs args)
        {
            var commandArgs = e.Command.ArgumentsAsList;
            var channelBotInfo = args.DbContext.ChannelInfos.SingleOrDefault(c =>
                EF.Functions.ILike(c.ChannelUsername, commandArgs[1]));
            if (channelBotInfo == null)
                return;

            channelBotInfo.Lang = Enum.TryParse<Lang>(commandArgs[0], out var result) ? result : Lang.ru;
        }
        
        public static void RefreshChannelInfosPrivateCommandCallback(object s, OnWhisperCommandReceivedArgs e, CommandCallbackArgs args)
        {
            args.ChannelBot.ChannelInfo = BotService.LoadChannelInfos(args.DbContext, args.ChannelBot.ChannelInfo.Id).GetAwaiter().GetResult().Single();
            args.Logger.LogInformation("ChannelInfo was refreshed");
        }
    }
}