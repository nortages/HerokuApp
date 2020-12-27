using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Extensions;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Events;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using System.Runtime.Loader;
using System.Reflection;

namespace HerokuApp
{
    public partial class TwitchChatBot
    {        
        static TwitchClient twitchClient;
        static readonly ConnectionCredentials credentials = new ConnectionCredentials(Config.BotUsername, Config.BotAccessToken);
        TwitchPubSub pubsub;
        static readonly Random rand = new Random();
        bool isDeployed = false;
        readonly DateTime upTime = DateTime.Now;

        readonly List<string> timedoutByBot = new List<string>();
        int massGifts = 0;
        readonly TimeSpan TIMEOUTTIME = TimeSpan.FromMinutes(10);
        const string OwnerUsername = "segatron_lapki";
        (bool flag, int num) timeoutUserBelowData = (false, 0);
        bool testCommandsMode = false;

        const int SAVEDCHATMESSAGESNUM = 100;
        public static Queue<ChatMessage> chatMessages = new Queue<ChatMessage>(SAVEDCHATMESSAGESNUM);

        static readonly RegexOptions regexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;
        readonly Regex regex_trimEndFromQuyaBot = new Regex(@"\[\d\]", regexOptions);
        readonly Regex regex_botsPlusToChat = new Regex(@".*?Боты?,? \+ в ча[тй].*", regexOptions);
        readonly Regex regex_hiToBot = new Regex(@".+?NortagesBot,?.+?(Привет|Здравствуй|Даров|kupaSubHype|kupaPrivet|KonCha|VoHiYo|PrideToucan|HeyGuys|basilaHi|Q{1,2}).*", regexOptions);
        readonly Regex regex_botCheck = new Regex(@"@NortagesBot,? (Жив|Живой|Тут|Здесь)\?", regexOptions);
        readonly Regex regex_botLox = new Regex(@"@NortagesBot,? (kupaLox|лох)", regexOptions);
        readonly Regex regex_botWorryStick = new Regex(@"@NortagesBot,?( worryStick)+", regexOptions);
        readonly Regex regex_marko = new Regex(@"@NortagesBot,? марко", regexOptions);
        readonly Regex regex_ping = new Regex(@"@NortagesBot,? ping", regexOptions);
        readonly Regex regex_mew = new Regex(@"@NortagesBot,? (мя+у+|му+р+)", regexOptions);
        readonly Regex regex_up = new Regex(@"@NortagesBot,? (up|uptime|time)", regexOptions);
        readonly Regex regex_howMuch = new Regex(@"@NortagesBot,? сколько .+", regexOptions);
        readonly Regex regex_when = new Regex(@"@NortagesBot,? когда (.+)", regexOptions);

        public void Connect()
        {
            //ManageProcessExit();
            //Config.LoadChatMessages();

            manulsEasterEggs = Config.GetManulsEasterEggs();

            DefineBots();
            ConnectToTwitchChannels();
            PubSubInitialize();

            isDeployed = Environment.GetEnvironmentVariable("DEPLOYED") != null;
        }

        private void ManageProcessExit()
        {
            AssemblyLoadContext.Default.Unloading += ctx =>
            {
                Console.WriteLine("[AssemblyLoadContext.Default.Unloading] The app is about to shutdown");
                Config.SaveChatMessages();
            };
            AppDomain.CurrentDomain.DomainUnload += (_, e) =>
            {
                Console.WriteLine("[AppDomain.CurrentDomain.DomainUnload] The app is about to shutdown");
                Config.SaveChatMessages();
            };
            AppDomain.CurrentDomain.ProcessExit += (_, e) =>
            {
                Console.WriteLine("[AppDomain.CurrentDomain.ProcessExit] The app is about to shutdown");
                Config.SaveChatMessages();
            };
            Console.CancelKeyPress += (_, e) =>
            {
                Console.WriteLine("[Console.CancelKeyPress] The app is about to shutdown");
                Config.SaveChatMessages();
            };
            // add a NuGet reference to System.Runtime.Loader
            OnExit(() =>
            {
                Console.WriteLine("OnExit");
                Config.SaveChatMessages();
            });
        }

        public static void OnExit(Action onExit)
        {
            var assemblyLoadContextType = Type.GetType("System.Runtime.Loader.AssemblyLoadContext, System.Runtime.Loader");
            if (assemblyLoadContextType != null)
            {
                var currentLoadContext = assemblyLoadContextType.GetTypeInfo().GetProperty("Default").GetValue(null, null);
                var unloadingEvent = currentLoadContext.GetType().GetTypeInfo().GetEvent("Unloading");
                var delegateType = typeof(Action<>).MakeGenericType(assemblyLoadContextType);
                Action<object> lambda = (context) => onExit();
                unloadingEvent.AddEventHandler(currentLoadContext, lambda.GetMethodInfo().CreateDelegate(delegateType, lambda.Target));
                return;
            }

            var appDomainType = Type.GetType("System.AppDomain, mscorlib");
            if (appDomainType != null)
            {
                var currentAppDomain = appDomainType.GetTypeInfo().GetProperty("CurrentDomain").GetValue(null, null);
                var processExitEvent = currentAppDomain.GetType().GetTypeInfo().GetEvent("ProcessExit");
                EventHandler lambda = (sender, e) => onExit();
                processExitEvent.AddEventHandler(currentAppDomain, lambda);
                return;
                // Note that .NETCore has a private System.AppDomain which lacks the ProcessExit event.
                // That's why we test for AssemblyLoadContext first!
            }


            bool isNetCore = (Type.GetType("System.Object, System.Runtime") != null);
            if (isNetCore) throw new Exception("Before calling this function, declare a variable of type 'System.Runtime.Loader.AssemblyLoadContext' from NuGet package 'System.Runtime.Loader'");
            else throw new Exception("Neither mscorlib nor System.Runtime.Loader is referenced");

        }

        #region Initialization

        //void GoogleSheetsServiceInitialize()
        //{
        //    GoogleCredential credential;
        //    string[] scopes = { SheetsService.Scope.Spreadsheets };
        //    credential = GoogleCredential.FromJson(Config.GoogleCredentials).CreateScoped(scopes);

        //    // Create Google Sheets API service.
        //    sheetsService = new SheetsService(new BaseClientService.Initializer()
        //    {
        //        HttpClientInitializer = credential,
        //        ApplicationName = "ApplicationName",
        //    });
        //}

        void PubSubInitialize()
        {
            pubsub?.Disconnect();
            pubsub = new TwitchPubSub();
            pubsub.OnPubSubServiceConnected += PubSub_OnPubSubServiceConnected;
            pubsub.OnListenResponse += PubSub_OnListenResponse;
            pubsub.OnPubSubServiceClosed += Pubsub_OnPubSubServiceClosed;
            pubsub.OnPubSubServiceError += Pubsub_OnPubSubServiceError;

            foreach (var bot in bots)
            {
                if (bot.OnStreamUp != null)
                    pubsub.OnStreamUp += (s, e) => bot.OnStreamUp(s, e);
                if (bot.OnRewardRedeemed != null)
                    pubsub.OnRewardRedeemed += (s, e) => bot.OnRewardRedeemed(s, e);
                pubsub.ListenToRewards(TwitchHelpers.GetIdByUsername(bot.ChannelName));
            }

            pubsub.Connect();
        }

        static readonly List<Bot> bots = new List<Bot>();

        void DefineBots()
        {
            var kiraChannelBot = new Bot();
            kiraChannelBot.ChannelName = "k_i_r_a";
            kiraChannelBot.commands = GetKiraCommands();
            kiraChannelBot.OnNewSubscriber = Kira_OnNewSubscriber;
            kiraChannelBot.OnReSubscriber = Kira_OnReSubscriber;
            kiraChannelBot.OnGiftedSubscription = Kira_OnGiftedSubscription;
            kiraChannelBot.OnCommunitySubscription = Kira_OnCommunitySubscription;
            kiraChannelBot.OnWhisperReceived = Kira_OnWhisperReceived;
            kiraChannelBot.OnRewardRedeemed = Kira_OnRewardRedeemed;

            var th3gloChannelBot = new Bot();
            th3gloChannelBot.ChannelName = "th3globalist";
            //th3gloChannelBot.ChannelName = "segatron_lapki";
            th3gloChannelBot.commands = GetTh3globalistCommands();
            th3gloChannelBot.cooldown = TimeSpan.FromSeconds(10);

            bots.AddRange(new List<Bot> {
                kiraChannelBot,
                th3gloChannelBot,
            });
        }

        void ConnectToTwitchChannels()
        {
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1,
            };
            var customClient = new WebSocketClient(clientOptions);

            twitchClient = new TwitchClient(customClient);
            twitchClient.Initialize(credentials);

            twitchClient.OnUnaccountedFor += Client_OnUnaccountedFor;
            twitchClient.OnChatCommandReceived += OnChatCommandReceived;
            twitchClient.OnConnectionError += Client_OnConnectionError;
            twitchClient.OnDisconnected += Client_OnDisconnected;
            twitchClient.OnJoinedChannel += Client_OnJoinedChannel;
            twitchClient.OnMessageReceived += Client_OnMessageReceived;
            twitchClient.OnError += Client_OnError;

            foreach (var bot in bots)
            {
                twitchClient.OnConnected += (s, e) => twitchClient.JoinChannel(bot.ChannelName);
                if (bot.OnNewSubscriber != null)
                {
                    twitchClient.OnNewSubscriber += (s, e) => bot.OnNewSubscriber(s, e);
                }
                if (bot.OnReSubscriber != null)
                {
                    twitchClient.OnReSubscriber += (s, e) => bot.OnReSubscriber(s, e);
                }
                if (bot.OnGiftedSubscription != null)
                {
                    twitchClient.OnGiftedSubscription += (s, e) => bot.OnGiftedSubscription(s, e);
                }
                if (bot.OnCommunitySubscription != null)
                {
                    twitchClient.OnCommunitySubscription += (s, e) => bot.OnCommunitySubscription(s, e);
                }
                if (bot.OnWhisperReceived != null)
                {
                    twitchClient.OnWhisperReceived += (s, e) => bot.OnWhisperReceived(s, e);
                }
            }

            twitchClient.Connect();
        }

        #endregion Initialization

        #region TWITCH CLIENT SUBSCRIBERS

        void OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            if (e.Command.CommandText == "testmode")
            {
                var argString = e.Command.ArgumentsAsString;
                if (argString == "on")
                {
                    testCommandsMode = true;
                }
                else if (argString == "off")
                {
                    testCommandsMode = false;
                }
                return;
            }

            string answer = null;
            var username = e.Command.ChatMessage.Username;
            var channel = e.Command.ChatMessage.Channel;
            Command command = null;
            var bot = bots.Where(bot => bot.ChannelName == channel).First();
            foreach (var command_ in bot.commands)
            {
                if (command_.names.Contains(e.Command.CommandText))
                {
                    command = command_;                    
                    break;
                }
            }
            if (command == null) return;

            if (!testCommandsMode)
            {
                if (!bot.lastCommand.ContainsKey(username))
                {
                    bot.lastCommand.Add(username, default);
                }
                if (bot.lastCommand[username].Item1 == command)
                {
                    if (DateTime.Now - bot.lastCommand[username].Item2 < bot.cooldown)
                    {
                        return;
                    }
                }
                bot.lastCommand[username] = (command, DateTime.Now);

                if (!command.usageFrequency.ContainsKey(username))
                {
                    command.usageFrequency.Add(username, default);
                }
                command.usageFrequency[username]++; 
            }

            answer = command.callback(command, e);
            if (answer != null)
            {
                twitchClient.SendMessage(channel, $"{e.Command.ChatMessage.DisplayName}, {answer}");
            }
        }

        void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            var channel = e.ChatMessage.Channel;
            chatMessages.Enqueue(e.ChatMessage);
            if (chatMessages.Count > SAVEDCHATMESSAGESNUM) chatMessages.Dequeue();
            
            if (timeoutUserBelowData.flag && !e.ChatMessage.IsModerator && !e.ChatMessage.IsBroadcaster)
            {
                TimeoutUser(e.ChatMessage.Username, channel);
            }

            // Regexes
            if (regex_botsPlusToChat.IsMatch(e.ChatMessage.Message))
            {
                twitchClient.SendMessage(e.ChatMessage.Channel, "+");
            }
            else if (regex_howMuch.IsMatch(e.ChatMessage.Message))
            {
                var howMuch = rand.Next(0, 100);
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} {howMuch}");
            }
            else if (regex_when.IsMatch(e.ChatMessage.Message))
            {
                var variants = new string[] { "никогда", "завтра", "сегодня", "в следующем году", "на следующей неделе", "через час" };
                var when = variants[rand.Next(0, variants.Length)];
                if (regex_when.Match(e.ChatMessage.Message).Groups[1].Value.Contains("инфа по"))
                {
                    when = "через час";
                }
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} {when}");
            }
            else if (regex_up.IsMatch(e.ChatMessage.Message))
            {
                twitchClient.SendMessage(channel, @$"{e.ChatMessage.DisplayName} {upTime - DateTime.Now:dd\.hh\:mm}");
            }
            else if (regex_marko.IsMatch(e.ChatMessage.Message))
            {
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} Поло");
            }
            else if (regex_mew.IsMatch(e.ChatMessage.Message))
            {
                var num = rand.Next(1, 5);
                if (rand.Next(0, 10) > 4)
                {
                    var result = string.Concat(Enumerable.Repeat("я", num));
                    twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} м{result}у");
                }
                else
                {
                    var result = string.Concat(Enumerable.Repeat("р", num));
                    twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} му{result}");
                }
            }
            else if (regex_ping.IsMatch(e.ChatMessage.Message))
            {
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} pong");
            }
            else if (regex_hiToBot.IsMatch(e.ChatMessage.Message))
            {
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} Привет MrDestructoid");
            }
            else if (regex_botCheck.IsMatch(e.ChatMessage.Message))
            {
                var answer = regex_botCheck.Match(e.ChatMessage.Message).Groups[1].Value;
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} {answer}.");
            }
            else if (regex_botLox.IsMatch(e.ChatMessage.Message))
            {
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} сам {regex_botLox.Match(e.ChatMessage.Message).Groups[1].Value}");
            }
            else if (regex_botWorryStick.IsMatch(e.ChatMessage.Message))
            {
                twitchClient.SendMessage(channel, $"{e.ChatMessage.DisplayName} KEKWait");
            }
            else if (GTAcodes.ContainsKey(e.ChatMessage.Message.ToUpper().Split()[0]))
            {
                var args = e.ChatMessage.Message.Split();
                string arg = null;
                if (args.Length > 1) arg = args[1];

                if (GTAcodes[args[0].ToUpper()].Contains("{1}"))
                {
                    if (arg != null)
                    {
                        twitchClient.SendMessage(channel, string.Format(GTAcodes[args[0].ToUpper()], e.ChatMessage.Username, arg));
                    }
                    else
                    {
                        var chatters = TwitchHelpers.GetChatters(e.ChatMessage.Channel);
                        arg = chatters[rand.Next(0, chatters.Count)].Username;
                        twitchClient.SendMessage(channel, string.Format(GTAcodes[args[0].ToUpper()], e.ChatMessage.Username, arg));
                    }
                }
                else
                {
                    if (arg != null)
                    {
                        twitchClient.SendMessage(channel, string.Format(GTAcodes[args[0].ToUpper()], arg));
                    }
                    else
                    {
                        twitchClient.SendMessage(channel, string.Format(GTAcodes[args[0].ToUpper()], e.ChatMessage.Username));
                    }
                }
            }
            else if (hitBySnowballData.isHitBySnowball && e.ChatMessage.DisplayName == "QuyaBot")
            {
                hitBySnowballData.isHitBySnowball = false;
                var message = e.ChatMessage.Message;
                message = regex_trimEndFromQuyaBot.Replace(message, "");
                var snowballSender = hitBySnowballData.userName;
                var commandCooldown = TimeSpan.FromSeconds(15);
                var messageCooldown = TimeSpan.FromSeconds(5);
                var botUsername = Config.BotUsername.ToLower();
                if (message == $"Снежок прилетает прямо в {botUsername}, а {snowballSender}, задорно хохоча, скрывается с места преступления!")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " та за шо?(", messageCooldown);
                }
                else if (message == $"Снежок, запущенный {snowballSender} по невероятной траектории, попадает по жо... попадает ниже спины {botUsername}.")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ах ты... ну, погоди! Kappa", messageCooldown);
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, $"!снежок @{snowballSender}", commandCooldown);
                }
                else if (message == $"{snowballSender} хватает камень и кидает его в {botUsername}. Ты вообще адекватен? Так делать нельзя!")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ай! Вообще-то, очень больно было BibleThump", messageCooldown);
                }
                else if (message == $"{snowballSender} коварно подкрадывается со снежком к {botUsername} и засовывет пригорошню снега прямо за шиворот! Такой подлости никто не ждал!")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " Твою ж... Холодно-то как... Ну, ладно! Ща тоже снега попробуешь KappaClaus", messageCooldown);
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, $"!снежок @{snowballSender}", commandCooldown);
                }
                else if (message == $"{snowballSender} кидается с кулаками на {botUsername}. Кажется ему никто не объяснил правил!")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ты чего дерёшься?? SMOrc", messageCooldown);
                }
                else if (message == $"Видимо {snowballSender} имеет небольшое косоглазие, потому что не попадает снежком в {botUsername}!")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " ха, мазила! PepeLaugh", messageCooldown);
                }
                else if (message == $"{snowballSender} метко попадает снежком в лицо {botUsername}. Ну что, вкусный снег в этом году?")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " *Пфу-пфу* Микросхемы мне в корпус, ты что творишь??", messageCooldown);
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, $"!снежок @{snowballSender}", commandCooldown);
                }
                else if (message == $"{snowballSender} пытается кинуть снежок, но неклюже поскальзывается и падает прямо в сугроб. Видимо, сегодня неудачный день!")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " PepeLaugh", messageCooldown);
                }
                else if (message == $"{snowballSender} кидает снежок, но {botUsername} мастерстки ловит его на лету и кидает в обратную сторону! Нет, ну вы это видели?")
                {
                    twitchClient.SendMessageWithDelay(e.ChatMessage.Channel, snowballSender + " не в этот раз EZY", messageCooldown);
                }
            }
        }

        void Client_OnLog(object sender, TwitchLib.Client.Events.OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime}: {e.BotUsername} - {e.Data}");
            // TODO: Once in a while save logs to a file
        }

        void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine("Hey guys! I am a bot connected via TwitchLib!");
        }

        void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        void Client_OnUnaccountedFor(object sender, OnUnaccountedForArgs e)
        {
            Console.WriteLine("[OnUnaccountedForArgs] " + e.RawIRC);
        }

        void Client_OnError(object sender, OnErrorEventArgs e)
        {
            Console.WriteLine($"ERROR: {e.Exception.Message}\n{e.Exception.StackTrace}");
        }

        void Client_OnConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Console.WriteLine(e.Error);
        }

        void Client_OnDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Console.WriteLine("Twitch client is disconned. Trying to reconnect...");
            twitchClient.Reconnect();
            //Disconnect();
        }

        void Kira_OnCommunitySubscription(object sender, OnCommunitySubscriptionArgs e)
        {
            if (e.Channel != "k_i_r_a") return;

            massGifts = e.GiftedSubscription.MsgParamMassGiftCount;
            if (e.GiftedSubscription.MsgParamMassGiftCount == 1)
            {
                twitchClient.SendMessage(e.Channel, $"{e.GiftedSubscription.DisplayName}, спасибо за подарочную подписку! PrideFlower");
            }
            else
            {
                twitchClient.SendMessage(e.Channel, $"{e.GiftedSubscription.DisplayName}, спасибо за подарочные подписки! peepoLove peepoLove peepoLove");
            }
        }

        void Kira_OnGiftedSubscription(object sender, OnGiftedSubscriptionArgs e)
        {
            if (e.Channel != "k_i_r_a") return;

            if (massGifts > 0)
            {
                massGifts--;
            }
            else
            {
                var answer = $"спасибо за подарочную подписку для {e.GiftedSubscription.MsgParamRecipientDisplayName}! peepoLove";
                if (e.GiftedSubscription.MsgParamRecipientDisplayName.ToLower() == Config.BotUsername.ToLower())
                {
                    answer = "спасибо большое за подписку мне kupaLove kupaLove kupaLove";
                }
                twitchClient.SendMessage(e.Channel, $"{e.GiftedSubscription.DisplayName}, {answer}");
            }
        }

        void Kira_OnReSubscriber(object sender, OnReSubscriberArgs e)
        {
            if (e.Channel != "k_i_r_a") return;

            twitchClient.SendMessage(e.Channel, $"{e.ReSubscriber.DisplayName}, спасибо за продление подписки! Poooound");
        }

        void Kira_OnNewSubscriber(object sender, OnNewSubscriberArgs e)
        {
            if (e.Channel != "k_i_r_a") return;

            twitchClient.SendMessage(e.Channel, $"{e.Subscriber.DisplayName}, спасибо за подписку! bleedPurple Давайте сюда Ваш паспорт FBCatch kupaPasport");
            twitchClient.SendMessageWithDelay(e.Channel, "!саб", TimeSpan.FromSeconds(2));
        }

        void Kira_OnWhisperReceived(object sender, OnWhisperReceivedArgs e)
        {
            var arguments = e.WhisperMessage.Message.Split(" ");
            if (e.WhisperMessage.Message == "!saveMessages")
            {
                Config.SaveChatMessages();
            }
            else if (e.WhisperMessage.Message == "!updateEasterEggs")
            {
                manulsEasterEggs = Config.GetManulsEasterEggs();
            }
            else if (e.WhisperMessage.Message == "!pubsub")
            {
                if (new string[] { "reset", "restart" }.Contains(arguments[1]))
                {
                    PubSubInitialize();
                }
                else if (arguments[1] == "off")
                {
                    pubsub.Disconnect();
                }
            }

            var senderId = e.WhisperMessage.UserId;
            if (TwitchHelpers.IsSubscribeToChannel(Config.ChannelUserID, senderId, Config.ChannelAccessToken) && timedoutByBot.Contains(e.WhisperMessage.Username))
            {
                twitchClient.SendMessage(Config.ChannelName, $"{e.WhisperMessage.Username} передаёт: {e.WhisperMessage.Message}");
                timedoutByBot.Remove(e.WhisperMessage.Username);
            }
        }

        #endregion TWITCH CLIENT SUBSCRIBERS

        #region PUBSUB SUBSCRIBERS

        void Pubsub_OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            Console.WriteLine($"[PUBSUB_ERROR]\nMessage: {e.Exception.Message}\nStackTrace: {e.Exception.StackTrace}\nData: {e.Exception.Data}\nSource: {e.Exception.Source}");
        }

        void Pubsub_OnPubSubServiceClosed(object sender, EventArgs e)
        {
            Console.WriteLine("[PUBSUB_CLOSED]");
        }

        void PubSub_OnPubSubServiceConnected(object sender, EventArgs e)
        {
            // SendTopics accepts an oauth optionally, which is necessary for some topics
            Console.WriteLine("PubSub Service is Connected");

            pubsub.SendTopics(Config.BotAccessToken);
        }

        void PubSub_OnListenResponse(object sender, OnListenResponseArgs e)
        {
            if (e.Successful)
                Console.WriteLine($"Successfully verified listening to topic: {e.Topic}");
            else
                Console.WriteLine($"Failed to listen! Error: {e.Response.Error}");
        }

        void Kira_OnRewardRedeemed(object sender, OnRewardRedeemedArgs e)
        {
            if (TwitchHelpers.GetUsernameById(e.ChannelId).ToLower() != "k_i_r_a") return;
            if (e.Status != "UNFULFILLED") return;

            Console.WriteLine("\nSomeone redeemed a reward!");
            Console.WriteLine($"Name: {e.DisplayName},\nTitle: {e.RewardTitle}\n");

            if (e.RewardTitle.Contains("Таймач самому себе"))
            {
                TimeoutUser(e.DisplayName, TwitchHelpers.GetUsernameById(e.ChannelId));
            }
            else if (e.RewardTitle.Contains("Таймач человеку снизу"))
            {
                timeoutUserBelowData.flag = true;
                timeoutUserBelowData.num++;
            }
        }

        #endregion PUBSUB SUBSCRIBERS
    }
}