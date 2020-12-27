using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;

using TwitchLib.Client;
using TwitchLib.Client.Events;

using Newtonsoft.Json;

using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using RestSharp;
using Newtonsoft.Json.Linq;
using TwitchLib.Client.Extensions;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace HerokuApp
{
    public partial class TwitchChatBot
    {
        //const string linkToHOF = "https://docs.google.com/spreadsheets/d/19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";
        //const string spreadsheetId_HOF = "19RwGl1i79-3ZuVYyytfyvsg_wVprvozMSyooAy3HaU8";
        static (bool isHitBySnowball, string userName) hitBySnowballData = (false, null);
        readonly Dictionary<string, string> GTAcodes = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText("gta_codes.json"));
        static JArray manulsEasterEggs;
        static JArray allPirates = Config.GetPirates();
        static ChromeDriver driver;

        static List<Command> GetTh3globalistCommands()
        {
            // Main radish command
            var radishCommand = new Command();
            radishCommand.usageFrequency = Config.GetRadishCommandUsage().ToObject<Dictionary<string, int>>();

            // Option1 - Throw radish (0.4)
            var throwRadishOption1 = new ProbabilityOption
            {
                probability = 0.25,
                callback = (e, option) => "ногу",
            };
            var throwRadishOption2 = new ProbabilityOption
            {
                probability = 0.25,
                callback = (e, option) => "руку",
            };
            var throwRadishOption3 = new ProbabilityOption
            {
                probability = 0.15,
                callback = (e, option) => "рот",
            };
            var throwRadishOption4 = new ProbabilityOption
            {
                probability = 0.15,
                callback = (e, option) => "спину",
            };
            var throwRadishOption5 = new ProbabilityOption
            {
                probability = 0.1,
                callback = (e, option) => "глаз",
            };
            var throwRadishOption6 = new ProbabilityOption
            {
                probability = 0.1,
                callback = (e, option) => "голову",
            };
            var throwRadishOptions = new List<ProbabilityOption>
            {
                throwRadishOption1,
                throwRadishOption2,
                throwRadishOption3,
                throwRadishOption4,
                throwRadishOption5,
                throwRadishOption6,
            };
            var radishCommandOption1 = new ProbabilityOption
            {
                title = "Throw radish",
                probability = 0.4,
                callback = ThrowRadishOption,
                commandOptions = throwRadishOptions,
                convertedProbabilities = GetConvertedProbabilities(throwRadishOptions),
                command = radishCommand
            };

            // Option2 - Drop radish (0.65)
            var radishCommandOption2 = new ProbabilityOption
            {
                title = "Drop radish",
                probability = 0.25,
                callback = DropRadishOption,
                command = radishCommand
            };

            // Option 3 - Radish detonator (0.8)
            var radishDetanatorOption1 = new ProbabilityOption
            {
                probability = 0.5,
                callback = (e, option) => "ЭТО НЕ РЕДИСКА, ЭТО ДЕТОНАТОР, ПОЛОЖИ НА МЕСТО WutFace",
            };
            var radishDetanatorOption2 = new ProbabilityOption
            {
                probability = 0.5,
                callback = (e, option) => "остановись пока... больше никаких детонаций..",
            };
            var radishDetanatorOptions = new List<ProbabilityOption>
            {
                radishDetanatorOption1,
                radishDetanatorOption2,
            };
            var radishCommandOption3 = new ProbabilityOption
            {
                title = "Radish detonator",
                probability = 0.15,
                callback = RadishDetanatorOption,
                commandOptions = radishDetanatorOptions,
                convertedProbabilities = GetConvertedProbabilities(radishDetanatorOptions),
                command = radishCommand
            };

            // Option 4 - Radish transform (0.9)
            var radishTransformOption1 = new ProbabilityOption
            {
                probability = 0.9,
                callback = (e, option) => "редиска превратилась в клубнику прямо у тебя в руках. Приятного аппетита <3",
            };
            var radishTransformOption2 = new ProbabilityOption
            {
                probability = 0.1,
                callback = (e, option) => "Редиска превратилась в ЗОЛОТУЮ клубнику прямо у тебя в руках PogChamp! Береги её!",
            };
            var radishTransformOptions = new List<ProbabilityOption>
            {
                radishTransformOption1,
                radishTransformOption2,
            };
            var radishCommandOption4 = new ProbabilityOption
            {
                title = "Radish transforms",
                probability = 0.1,
                callback = RadishTransformOption,
                commandOptions = radishTransformOptions,
                convertedProbabilities = GetConvertedProbabilities(radishTransformOptions),
                command = radishCommand
            };

            // Option 5 - Radish wants to talk (0.95)
            var radishWantsToTalkOption1 = new ProbabilityOption
            {
                probability = 0.5,
                callback = (e, option) => "лёгенький разговорчик",
            };
            var radishWantsToTalkOption2 = new ProbabilityOption
            {
                probability = 0.25,
                callback = (e, option) => "еСтЬ нЕсКоЛьКо ВоПрОсОв",
            };
            var radishWantsToTalkOption3 = new ProbabilityOption
            {
                probability = 0.2,
                callback = (e, option) => "серьёзный разговор",
            };
            var radishWantsToTalkOption4 = new ProbabilityOption
            {
                probability = 0.05,
                callback = (e, option) => "никаких претензий. Прости, что побеспокоила",
            };
            var radishWantsToTalkOptions = new List<ProbabilityOption>
            {
                radishWantsToTalkOption1,
                radishWantsToTalkOption2,
                radishWantsToTalkOption3,
                radishWantsToTalkOption4,
            };
            var radishCommandOption5 = new ProbabilityOption
            {
                title = "Radish wants to talk",
                probability = 0.05,
                callback = RadishWantsToTalkOption,
                commandOptions = radishWantsToTalkOptions,
                convertedProbabilities = GetConvertedProbabilities(radishWantsToTalkOptions),
                command = radishCommand
            };

            // Option 6 - Radish tired (0.9759)
            var radishTiredOption1 = new ProbabilityOption
            {
                probability = 0.96,
                callback = (o, e) => "Послушай... ты уже {0} {1} взывал ко мне..  может, тебе хочется умиротворения? Покоя? Я даю тебе его. Лети на крыльях ветра и возвращайся через минуту <3",
            };
            var radishTiredOption2 = new ProbabilityOption
            {
                probability = 0.04,
                callback = (o, e) => "Ты уже {0} {1} пытаелся выведать у меня правду.. так вот, знай, что... я......... я на самом деле жёлтый банан.",
            };
            var radishTiredOptions = new List<ProbabilityOption>
            {
                radishTiredOption1,
                radishTiredOption2,
            };
            var radishCommandOption6 = new ProbabilityOption
            {
                title = "Radish tired",
                probability = 0.0259,
                callback = RadishTiredOption,
                commandOptions = radishTiredOptions,
                convertedProbabilities = GetConvertedProbabilities(radishTiredOptions),
                command = radishCommand
            };

            // Option 7 - Отрывок из Садистки (0.9999)
            var radishCommandOption7 = new ProbabilityOption
            {
                title = "Садистка",
                probability = 0.024,
                command = radishCommand,
                callback = (c, e) => "Я ВЫРВУ ТВОИ ГЛАЗА catJAM " +
                "ЧТОБ НЕ МОГ ТЫ СМОТРЕТЬ НА ДРУГИХ catJAM " +
                "ЧТОБЫ МОГ ЛЮБИТЬ ЛИШЬ МЕНЯ catJAM " +
                "И КАСАТЬСЯ ГУБ ЛИШЬ МОИХ catJAM",
            };

            // Option 8 - Subcription! (1)
            var radishCommandOption8 = new ProbabilityOption
            {
                title = "Subcription",
                probability = 0.0001,
                command = radishCommand,
                callback = (c, e) => "Ого! За такое упорство и везение редиска награждает тебя подпиской PogChamp",
            };

            var radishCommandOptions = new List<ProbabilityOption>
            {
                radishCommandOption1,
                radishCommandOption2,
                radishCommandOption3,
                radishCommandOption4,
                radishCommandOption5,
                radishCommandOption6,
                radishCommandOption7,
                radishCommandOption8,
            };
            radishCommand.commandOptions = radishCommandOptions;
            radishCommand.callback = (o, e) => RadishCommandCallback((Command)o, e);
            radishCommand.names.Add("редиска");
            radishCommand.convertedProbabilities = GetConvertedProbabilities(radishCommandOptions);

            //var sampleNum = 1000;
            //var result = CheckCommandOptionsFrequency(radishCommand, sampleNum);
            //foreach (var item in result)
            //{
            //    Console.WriteLine($"{item.Key.title} - {(double)item.Value / sampleNum:0.##}");
            //}

            // Boat command
            var boatCommand = new Command();
            boatCommand.callback = BoatCommandCallback;
            boatCommand.names.AddRange(new List<string> { "лодка", "boat" });


            // A new command
            var newCommand = new Command();
            newCommand.callback = NewCommandCallback;
            newCommand.names.AddRange(new List<string> { "карта" });

            return new List<Command>
            {
                radishCommand,
                boatCommand,
                newCommand,
            };
        }

        static List<Command> GetKiraCommands()
        {
            // Song command
            var songCommand = new Command();
            songCommand.callback = SongCommandCallback;
            songCommand.names.AddRange(new List<string> { "song", "music", "песня", "музыка" });

            // Manul command
            var manulCommand = new Command();
            manulCommand.callback = ManulCommandCallback;
            manulCommand.names.AddRange(new List<string> { "манул", "manul", "манулы", "manuls" });

            // Promocode command
            var promocodeCommand = new Command();
            promocodeCommand.callback = (o, e) => "kira - лучший промокод на mycsgoo.net TakeNRG";
            promocodeCommand.names.AddRange(new List<string> { "промокод" });

            // Tyanochka command
            var tyanochkaCommand = new Command();
            tyanochkaCommand.callback = (o, e) => "Как же хочется тяночку. Как же хочется худенькую, бледную, не очень высокую, девственную, нецелованную, с тонкими руками, небольшими ступнями, синяками под глазами, растрёпанными или неуложенными волосами, ненакрашенную, забитую хикку, лохушку без друзей и подруг, закрытую социофобку, одновременно мечтающую о ком-то близком, чтобы зашёл к ней в мирок, но ничего не ломал по возможности, дабы вместе с ней изолироваться от неприятного социума.";
            tyanochkaCommand.names.AddRange(new List<string> { "тяночка" });

            // Boy command
            var boyCommand = new Command();
            boyCommand.callback = (o, e) => "Как же хочется ♂️ boy next door ♂️.  Как же хочется мускулистого, накаченного, нецелованного ♂️ leatherman ♂️ с ♂️ finger in my ass ♂️, который ♂️ turns me on ♂️ , одновременно мечтающего о ♂️ fucking slaves ♂️ , дабы зайти к нему в ♂️ dungeon ♂️ , ♂️ suck some dick ♂️ and ♂️ swallow cum ♂️";
            boyCommand.names.AddRange(new List<string> { "boy" });

            // Promocode command
            var donateGoalCommand = new Command();
            donateGoalCommand.callback = (o, e) => "Кира, когда донатгол на БЛ? BibleThump";
            donateGoalCommand.names.AddRange(new List<string> { "бл" });

            // Snowball command
            var snowballCommand = new Command();
            snowballCommand.callback = BotIsHitBySnowball;
            snowballCommand.names.AddRange(new List<string> { "снежок" });

            return new List<Command>
            {
                songCommand,
                manulCommand,
                promocodeCommand,
                tyanochkaCommand,
                boyCommand,
                donateGoalCommand,
                promocodeCommand,
                snowballCommand,
            };
        }

        static bool IsMeOrBroadcaster(OnChatCommandReceivedArgs e) => e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.Username.ToLower() == OwnerUsername;

        void TimeoutUser(string username, string channel)
        {
            var mult = 1;
            if (timeoutUserBelowData.flag) mult = timeoutUserBelowData.num;
            var timeoutTime = TimeSpan.FromTicks(TIMEOUTTIME.Ticks * mult);
            twitchClient.TimeoutUser(channel, username, timeoutTime);
            timedoutByBot.Add(username.ToLower());
            timeoutUserBelowData = (false, 0);
            Console.WriteLine($"{username} is banned on {timeoutTime} minutes!");
            Task.Delay(timeoutTime).ContinueWith(t => timedoutByBot.Remove(username.ToLower()));
        }

        static ProbabilityOption GetRandOption(Option option, double randVal = default)
        {
            if (randVal == default) randVal = rand.NextDouble();
            if (randVal > 1) randVal = 1;

            var converted = option.convertedProbabilities;

            ProbabilityOption result = null;
            for (int i = 0; i < converted.Count; i++)
            {
                if (converted[i] >= randVal)
                {
                    result = option.commandOptions[i];
                    break;
                } 
            }
            return result;
        }

        private static List<double> GetConvertedProbabilities(List<ProbabilityOption> options)
        {
            var sums = new List<double>();
            foreach (var option in options)
            {
                var sum = sums.LastOrDefault();
                var newSum = sum + option.probability;
                sums.Add(newSum);
            }

            return sums;
        }

        static string GetWordEnding(int num, string[] endings)
        {
            var ending = "";
            switch (num % 10)
            {
                case 1:
                    ending = endings[0];
                    break;
                case 2:
                case 3:
                case 4:
                    ending = endings[1];
                    break;
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 0:
                    ending = endings[2];
                    break;
            }
            switch (num % 100)
            {
                case 11:
                case 12:
                case 13:
                case 14:
                    ending = endings[2];
                    break;
            }

            return ending;
        }

        static string SongCommandCallback(Option o, OnChatCommandReceivedArgs e)
        {
            var prefix = string.IsNullOrEmpty(e.Command.ArgumentsAsString) ? "" : e.Command.ArgumentsAsString + ", ";
            var message = "Все песни, кроме тех, что с ютуба, транслируются у стримера в группе вк, заходи GivePLZ https://vk.com/k_i_ra_group TakeNRG";
            return $"{prefix}{message}";
        }

        static string BotIsHitBySnowball(Option o, OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsString.TrimStart('@') == Config.BotUsername)
            {
                Console.WriteLine("Someone just throw a snowball to the bot!");
                hitBySnowballData.isHitBySnowball = true;
                hitBySnowballData.userName = e.Command.ChatMessage.DisplayName;
            }
            return null;
        }

        static string ManulCommandCallback(Option o, OnChatCommandReceivedArgs e)
        {
            int manulsNum = Config.GetManuls();
            manulsNum++;
            string manulWord = "манул";
            manulWord += GetWordEnding(manulsNum, new string[] { "", "а", "ов" });
            var answer = $"{manulsNum} {manulWord}";
            Config.SaveManuls(manulsNum);
            var randNum = rand.Next(0, 10);
            if (randNum == 0)
            {
                var notPlayed = manulsEasterEggs.Where(n => n.Value<bool>("wasPlayed") == false).ToList();
                randNum = rand.Next(0, notPlayed.Count);
                var easterEgg = notPlayed[randNum];
                manulsEasterEggs.ToList().Find(x => x["link"] == easterEgg["link"])["wasPlayed"] = true;
                Config.SaveManulsEasterEggs(manulsEasterEggs);
                answer += " " + easterEgg.Value<string>("link");
            }
            return answer;
        }

        static (string timeouts, string bans) GetChannelStats(string userName)
        {
            var inputElement = driver.FindElement(By.XPath("//input[@name='viewers-filter']"), 10);
            inputElement.Clear();
            inputElement.SendKeys(userName);

            var userElement = driver.FindElement(By.XPath($"//p[text()='{userName.ToLower()}']"), 5);
            userElement.Click();
            var infoPanel = driver.FindElement(By.XPath("//div[@data-test-selector='viewer-card-mod-drawer']"), 2);
            var panelElements = infoPanel.FindElements(By.XPath(".//div[@data-test-selector='viewer-card-mod-drawer-tab']"));

            var xpath = ".//p[contains(@class, 'tw-c-text-link')]";
            var timeouts = panelElements[1].FindElement(By.XPath(xpath), 3).Text;
            var bans = panelElements[2].FindElement(By.XPath(xpath), 3).Text;

            driver.FindElement(By.XPath("//button[@data-a-target='user-details-close']")).Click();

            return (timeouts, bans);
        }

        static string ThrowRadishOption(Option commandOption, OnChatCommandReceivedArgs e)
        {
            var chatters = TwitchHelpers.GetChatters(e.Command.ChatMessage.Channel);
            var randChatter = chatters[rand.Next(0, chatters.Count)];
            var answer = "кинул(а) редиску прямо в {0} {1}";
            var option = GetRandOption(commandOption);
            answer = string.Format(answer, option.callback(commandOption, e), randChatter.Username);
            return answer;
        }

        static string DropRadishOption(Option commandOption, OnChatCommandReceivedArgs e) => "молодец, ты уронил(а) редиску.";

        static string RadishDetanatorOption(Option commandOption, OnChatCommandReceivedArgs e)
        {
            ProbabilityOption option;
            var username = e.Command.ChatMessage.Username;
            var currentOption = (ProbabilityOption)commandOption;
            if (commandOption.usageFrequency.ContainsKey(username))
            {
                if (currentOption.command.lastOption[username] == currentOption)
                {
                    option = GetRandOption(commandOption);
                }
                else
                {
                    option = commandOption.commandOptions[0];
                }
            }
            else
            {
                commandOption.usageFrequency.Add(username, 0);
                option = commandOption.commandOptions[0];
            }
            commandOption.usageFrequency[username]++;
            var answer = option.callback(commandOption, e);
            return answer;
        }

        static string RadishTransformOption(Option commandOption, OnChatCommandReceivedArgs e)
        {
            ProbabilityOption option;
            var username = e.Command.ChatMessage.Username;
            if (commandOption.usageFrequency.ContainsKey(username))
            {
                if (commandOption.usageFrequency[username] >= 9)
                {
                    option = GetRandOption(commandOption);
                    if (option == commandOption.commandOptions[1])
                    {
                        commandOption.usageFrequency[username] = 0;
                    }
                }
                else
                {
                    option = commandOption.commandOptions[0];
                }
            }
            else
            {
                commandOption.usageFrequency.Add(username, 0);
                option = commandOption.commandOptions[0];
            }
            commandOption.usageFrequency[username]++;
            var answer = option.callback(commandOption, e);
            return answer;
        }

        static string RadishWantsToTalkOption(Option commandOption, OnChatCommandReceivedArgs e)
        {
            var option = GetRandOption(commandOption);
            var answer = "У меня к тебе " + option.callback(commandOption, e);
            return answer;
        }

        static string RadishTiredOption(Option commandOption, OnChatCommandReceivedArgs e)
        {
            var usageFrequency = ((ProbabilityOption)commandOption).command.usageFrequency[e.Command.ChatMessage.Username];
            var option = GetRandOption(commandOption);
            var answer = option.callback(commandOption, e);
            var numOfUsing = (commandOption as ProbabilityOption).command.usageFrequency[e.Command.ChatMessage.Username];
            var timesWord = "раз";
            timesWord += GetWordEnding(numOfUsing, new string[] { "", "а", "" });
            answer = string.Format(answer, numOfUsing, timesWord);
            if (option == commandOption.commandOptions[0])
                twitchClient.TimeoutUser(e.Command.ChatMessage.Channel, e.Command.ChatMessage.Username, TimeSpan.FromMinutes(1));
            return answer;
        }

        public static Dictionary<ProbabilityOption, int> CheckCommandOptionsFrequency(Command command, int sampleNum)
        {
            var optionsFrequency = new Dictionary<ProbabilityOption, int>();
            for (int i = 0; i < sampleNum; i++)
            {
                var option = GetRandOption(command);
                if (!optionsFrequency.ContainsKey(option))
                {
                    optionsFrequency.Add(option, 0);
                }
                optionsFrequency[option]++;
            }
            return optionsFrequency.OrderByDescending(kv => kv.Key.probability).ToDictionary(kv => kv.Key, kv => kv.Value);
        }

        private static string RadishCommandCallback(Command command, OnChatCommandReceivedArgs e)
        {
            var username = e.Command.ChatMessage.Username;
            var option = GetRandOption(command);
            if (!command.lastOption.ContainsKey(username))
            {
                command.lastOption.Add(username, default);
                Config.SaveRadishCommandUsage(new KeyValuePair<string, int>(username, default));
            }

            if (e.Command.ArgumentsAsList.Count != 0 && IsMeOrBroadcaster(e))
            {
                if (double.TryParse(e.Command.ArgumentsAsList[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var value))
                {
                    option = GetRandOption(command, value);
                }
            }
            var answer = option.callback(option, e);
            Config.SaveRadishCommandUsage(new KeyValuePair<string, int>(username, command.usageFrequency[username]));
            command.lastOption[username] = option;
            return answer;
        }

        static int AssessPirates(JArray pirates)
        {
            int result = 0;
            foreach (var pirate in pirates)
            {
                result += pirate.Value<int>("attack") + pirate.Value<int>("health") + pirate.Value<JObject>("battlegrounds").Value<int>("tier");
            }
            return result;
        }

        private static string BoatCommandCallback(Option option_, OnChatCommandReceivedArgs e)
        {
            string answer;
            var assessmentOptions = new List<string> { "слабовато чел...", "а ты скилловый Jebaited" };
            var assessment = "";
            var pirates = new JArray();
            answer = "";

            if (e.Command.ArgumentsAsList.Count != 0 && e.Command.ArgumentsAsList[0] == "триплет" && IsMeOrBroadcaster(e))
            {
                JObject pirate;
                if (e.Command.ArgumentsAsList[1] == "элизы")
                {
                    pirate = (JObject)allPirates[9]; // Elisa
                }
                else
                {
                    pirate = (JObject)allPirates[rand.Next(0, allPirates.Count)];
                }
                for (int i = 0; i < 3; i++)
                {
                    pirates.Add(pirate);
                    answer += $"{pirate.Value<string>("name")} {pirate.Value<int>("attack")}-{pirate.Value<int>("health")}, ";
                }
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    var pirate = allPirates[rand.Next(0, allPirates.Count)];
                    pirates.Add(pirate);
                    answer += $"{pirate.Value<string>("name")} {pirate.Value<int>("attack")}-{pirate.Value<int>("health")}, ";
                }
            }

            answer = answer.TrimEnd(new char[] { ' ', ',' });
            if (pirates.All(n => pirates[0].Value<int>("id") == n.Value<int>("id")))
            {
                // Check for Elisa
                if (pirates[0].Value<int>("id") == 61047)
                {
                    assessment = "ТРИ ЭЛИЗЫ ЭТ КОНЕЧНО ПРИКОЛ, ДО ВСТРЕЧИ ЧЕРЕЗ ПОЛЧАСА LUL";
                    twitchClient.TimeoutUser(e.Command.ChatMessage.Channel, e.Command.ChatMessage.Username, TimeSpan.FromMinutes(30));
                }
                // Check for Amalgadon
                else if (pirates[0].Value<int>("id") == 61444)
                {
                    assessment = "777, ЛОВИ ВИПКУ";
                }
                else
                {
                    assessment = "найс триплет с лодки, жаль все равно отъедешь BloodTrail";
                }
            }
            else
            {
                var maxMark = 57; // Elisa x3
                var mark = AssessPirates(pirates);
                var quotient = (double)mark / maxMark;
                assessment = assessmentOptions[quotient > 0.6 ? 1 : 0];
            }
            return $"{e.Command.ChatMessage.DisplayName}, YEP {answer} YEP , {assessment}";
        }

        private static string NewCommandCallback(Option option_, OnChatCommandReceivedArgs e)
        {
            var fileLines = File.ReadLines("CardsCustomDescription.txt").ToList();
            var randLine = fileLines[rand.Next(0, fileLines.Count())];
            return randLine;
        }
    }
}
