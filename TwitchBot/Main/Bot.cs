using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchBot.Main.DonationAlerts;
using TwitchBot.Main.ExtensionsMethods;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TwitchBot.Main
{
    [JsonObject]
    public class Bot
    {
        public bool IsEnabled { get; set; }
        public string Id { get; set; }
        public string ChannelName { get; set; }
        public string ChannelUserId { get; set; }
        public bool IsHelperBot { get; set; }

        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(10)] public int UserCooldown { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(5)] public int GlobalCooldown { get; set; }
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue("ru")] public string BotLang { get; set; }

        [JsonProperty] private HelperBotInfo HelperBotInfo { get; set; }
        [JsonProperty] private TwitchClientInfo TwitchClientInfo { get; set; }
        [JsonProperty] private PubSubInfo PubSubInfo { get; set; }
        
        // [JsonConverter(typeof(DonationAlertsClientConverter))]
        // [JsonProperty("DonationAlertsInfo")] private DonationAlertsClient DonationAlerts { get; set; }

        public DonationAlertsInfo DonationAlertsInfo;

        public List<CommandInfo> Commands { get; set; } = new();
        public List<MessageCommandInfo> MessageCommands { get; set; } = new();

        [JsonIgnore] public string AccessToken { get; set; }
        [JsonIgnore] public string RefreshToken { get; set; }
        [JsonIgnore] public Bot StreamerBot { get; private set; }
        [JsonIgnore] public TwitchClient TwitchClient { get; private set; }
        [JsonIgnore] public TwitchPubSub TwitchPubSub { get; private set; }
        [JsonIgnore] public Dictionary<string, (CommandInfo, DateTime)> UsernameToLastCommandInfo = new();
         
        public ILogger Logger { get; private set; }
        public ILoggerProvider LoggerProvider { get; private set; }
        
        public Dictionary<string, bool> Modes = new()
        {
            { "test_mode", false },
            { "l_mode", false }
        };
       
        public void CreateLogger(ILoggerProvider loggerProvider)
        {
            if (loggerProvider == null) return;
            LoggerProvider = loggerProvider;
            Logger = loggerProvider.CreateLogger($"{ChannelName}{ (IsHelperBot ? " – HelperBot" : "")}");
        }
        
        private void Log<T>(LogLevel level, string answer)
        {
            var context = typeof(T).Name;
            Logger.Log(level, "[{Context}] {Answer}", context, answer);
        }
       
        public void Connect()
        {
            AccessToken = IsHelperBot ? Config.GetTwitchAccessToken(ChannelName) : Config.GetTwitchAccessToken();

            if (HelperBotInfo is {IsEnabled: true}) StreamerBot = GetStreamerBot();
            if (TwitchClientInfo is {IsEnabled: true}) TwitchClient = GetTwitchClient(ChannelName, AccessToken);
            if (PubSubInfo is {IsEnabled: true}) TwitchPubSub = GetTwitchPubSub(ChannelName, AccessToken);
            if (DonationAlertsInfo is {IsEnabled: true}) GetDonAlertsClient(DonationAlertsInfo, ChannelName);
        }

        private Bot GetStreamerBot()
        {
            var helperBot = HelperBotInfo.ConvertToBot();
            
            helperBot.ChannelUserId = ChannelUserId;
            helperBot.ChannelName = ChannelName;
            helperBot.CreateLogger(LoggerProvider);
            helperBot.Connect();
            
            return helperBot;
        }

        private void GetDonAlertsClient(DonationAlertsInfo donationAlertsInfo, string channelName)
        {
            var donAlertsClient = new DonationAlertsClient(donationAlertsInfo.AccessToken);

            LoadEvents(donAlertsClient, donationAlertsInfo.EventsCallbacks);
            
            // if (donationAlertsInfo.OnDonationAlertCallback != null)
            // {
            //     donAlertsClient.OnDonationAlert += (o, args) => donationAlertsInfo.OnDonationAlertCallback(
            //         new CallbackArgs<OnDonationAlertArgs> { bot = this, e = args});
            // }

            donAlertsClient.ListenToDonationAlerts();
            DonationAlertsClient.SetLogger(Logger);
            donAlertsClient.Connect();
            
            Program.OnProcessExit += (o, _) =>
            {
                donAlertsClient.Close();
            };
        }

        private void DonAlertsClient_OnDonationGoalUpdateReceived(object sender, DonationGoalUpdate e)
        {
            Console.WriteLine(e);
        }

        public TwitchClient GetTwitchClient(string channelName, string accessToken)
        {
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1,
            };
            var customClient = new WebSocketClient(clientOptions);
            var twitchClient = new TwitchClient(customClient);
            var credentials = new ConnectionCredentials(Config.BotUsername, accessToken);
            twitchClient.Initialize(credentials, channelName);

            twitchClient.AddChatCommandIdentifier('?');
            if (Commands.Count != 0) 
                twitchClient.OnChatCommandReceived += OnChatCommandReceived;
            if (MessageCommands.Count != 0)
                twitchClient.OnMessageReceived += OnMessageReceived;
            
            //twitchClient.OnWhisperCommandReceived += TwitchClient_OnWhisperCommandReceived;
            
            LoadEvents(twitchClient, TwitchClientInfo.EventsCallbacks);

            twitchClient.OnNoPermissionError += (sender, args) => Log<TwitchClient>(LogLevel.Warning, "No permission.");
            twitchClient.OnConnectionError += (s, e) => Log<TwitchClient>(LogLevel.Error, (e.Error.Message));
            // var waitHandle = new AutoResetEvent(false);
            twitchClient.OnJoinedChannel += (s, e) => { Log<TwitchClient>(LogLevel.Information, "Joined to the channel."); };
            twitchClient.OnConnected += (s, e) => { Log<TwitchClient>(LogLevel.Information, "Connected."); };
            twitchClient.OnDisconnected += (s, e) => Log<TwitchClient>(LogLevel.Warning, "Disconnected.");
            twitchClient.OnError += (s, e) => Log<TwitchClient>(LogLevel.Error, $"{e.Exception.Message}\n{e.Exception.StackTrace}");
            //twitchClient.OnUnaccountedFor += (s, e) => Log(e.RawIRC);
            //twitchClient.OnLog += (s, e) => Log(e.Data);

            twitchClient.Connect();
            
            return twitchClient;
        }
        
        public static void EventCallbackWrapper(object o, EventArgs e, EventCallbackInfo callbackInfo, Bot bot)
        {
            callbackInfo.Callback(o, e, new CallbackArgs {Bot = bot});
        }

        public Delegate GetHandler(EventInfo ev, EventCallbackInfo callbackInfo)
        {
            var parameters = ev.EventHandlerType.GetMethod("Invoke").GetParameters().
                Select((p, i) => Expression.Parameter(p.ParameterType, "p" + i)).ToList();
            
            var mi = GetType().GetMethod(
                "EventCallbackWrapper",
                BindingFlags.Public | BindingFlags.Static);
            
            var lambda = Expression.Lambda(ev.EventHandlerType, 
                Expression.Call(
                    mi, 
                    parameters[0], 
                    parameters[1],
                    Expression.Constant(callbackInfo),
                    Expression.Constant(this)),
                parameters.ToArray());
            
            return lambda.Compile();
        }

        private void LoadEvents<T>(T type, List<EventCallbackInfo> eventsCallbacks)
        {
            foreach (var item in eventsCallbacks)
            {
                var evInfo = typeof(T).GetEvent(item.EventName);
                if (evInfo == null)
                    throw new InvalidOperationException(
                        $"{item.EventName} event does not exist in the {nameof(type)} class.");

                var addHandler = evInfo.GetAddMethod();
                if (addHandler == null)
                    throw new InvalidOperationException(
                        $"{item.EventName} event of the {nameof(type)} class does not have the add accessor method.");

                evInfo.AddEventHandler(type, GetHandler(evInfo, item));
                
                if (!string.IsNullOrEmpty(item.ListenMethodName))
                    UtilityFunctions.CallMethodByName(typeof(DonationAlertsClient), item.ListenMethodName);
            }
        }

        private void TwitchClient_OnWhisperCommandReceived(object sender, OnWhisperCommandReceivedArgs e)
        {
            Logger.LogDebug($"A new whisper command - {e.Command.CommandText}");
        }

        public TwitchPubSub GetTwitchPubSub(string channelName, string accessToken)
        {
            var twitchPubSub = new TwitchPubSub();

            if (PubSubInfo.OnRewardRedeemedCallbacks != null)
            {
                twitchPubSub.OnRewardRedeemed += (s, e) => OnRewardRedeemed(s, e, channelName);
                twitchPubSub.ListenToRewards(ChannelUserId);
            }

            twitchPubSub.OnListenResponse += (_, e) =>
            {
                if (e.Successful)
                {
                    Log<TwitchPubSub>(LogLevel.Information, $"Successfully verified listening to topic: {e.Topic}.");
                }
                else
                {
                    Log<TwitchPubSub>(LogLevel.Error, $"Failed to listen. Error: {e.Response.Error}.");
                }
            };
            twitchPubSub.OnPubSubServiceClosed += delegate { Log<TwitchPubSub>(LogLevel.Warning, "Closed."); };
            twitchPubSub.OnPubSubServiceError += PubSubServiceErrorCallback;

            twitchPubSub.OnPubSubServiceConnected += delegate {
                Log<TwitchPubSub>(LogLevel.Information, "Connected.");
                twitchPubSub.SendTopics(accessToken);
            };

            twitchPubSub.Connect();
            return twitchPubSub;
        }

        private void OnRewardRedeemed(object s, OnRewardRedeemedArgs e, string channelName)
        {
            if (e.Status != "UNFULFILLED") return;

            Console.WriteLine("\nSomeone redeemed for a reward!");
            Console.WriteLine($"Username: {e.DisplayName},\n Title: {e.RewardTitle}\n");

            foreach (var item in PubSubInfo.OnRewardRedeemedCallbacks)
            {
                if (!item.IsEnabled) continue;  
                
                if (!string.Equals(e.RewardTitle, item.Title,
                    Config.StringComparison)) continue;

                var args = new CallbackArgs
                {
                    Bot = this,
                    IsTestMode = Modes["test_mode"],
                    Lang = BotLang
                };
                
                item.Callback(s, e, args);
            }
        }

        public static bool IsMeOrBroadcaster(OnChatCommandReceivedArgs e) {
            return e.Command.ChatMessage.IsMe ||
                e.Command.ChatMessage.IsBroadcaster ||
                string.Equals(e.Command.ChatMessage.Username, Config.OwnerUsername, Config.StringComparison);
        }

        private void OnChatCommandReceived(object s, OnChatCommandReceivedArgs e)
        {
            var argString = e.Command.ArgumentsAsString;
            var commandText = e.Command.CommandText;
            var channel = e.Command.ChatMessage.Channel;

            if (IsMeOrBroadcaster(e) && Modes.ContainsKey(commandText))
            {
                Modes[commandText] = argString switch
                {
                    "on" => true,
                    "off" => false,
                    _ => Modes[commandText]
                };
                return;
            }

            var lMode = false;
            if (Modes["l_mode"])
            {
                lMode = true;
                commandText = commandText.Replace('л', 'р');
            }
            
            string answer = null;
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var command in Commands)
            {
                if (!command.IsEnabled ||
                    !command.Names.Contains(commandText)) continue;

                if (e.Command.CommandIdentifier == '?')
                {
                    answer = $"@{e.Command.ChatMessage.DisplayName} {command.Description}";
                }
                else
                {
                    answer = HandleCommand(command, e, lMode);
                }
                
                break;
            }

            if (string.IsNullOrEmpty(answer)) return;
            
            TwitchClient.SendMessage(channel, answer);
        }

        private string HandleCommand(CommandInfo commandInfo, OnChatCommandReceivedArgs e, bool lCommandMode = false)
        {
            var isMentionRequired = commandInfo.IsMentionRequired ?? true;
            
            if (commandInfo == null) throw new ArgumentNullException(nameof(commandInfo));
            if (!commandInfo.IsEnabled) return null;

            var username = e.Command.ChatMessage.Username;

            if (!Modes["test_mode"])
            {
                if (!IsCommandAvailable(commandInfo, username)) return null;
                commandInfo.IncreaseUsageFrequency(username);
                commandInfo.UpdateLastUsage(username);
            }

            var args = new CallbackArgs
            {
                Bot = this,
                IsTestMode = Modes["test_mode"],
                Lang = BotLang,
                IsMentionRequired = isMentionRequired
            };
            var answer = commandInfo.GetAnswer(commandInfo, e, args);
            if (string.IsNullOrEmpty(answer)) return null;

            if (lCommandMode) answer = answer.Replace('р', 'л').Replace('Р', 'Л');
            if (args.IsMentionRequired) answer = $"@{e.Command.ChatMessage.DisplayName}, {answer}";

            const string randChatterVariable = "${random.chatter}";
            if (answer.Contains(randChatterVariable))
            {
                answer = answer.Replace(randChatterVariable, TwitchHelpers.GetRandChatter(e.Command.ChatMessage.Channel).Username);
            }
            answer = answer.Replace("${sender}", e.Command.ChatMessage.DisplayName);
            
            return answer;
        }

        private bool IsCommandAvailable(CommandInfo commandInfo, string username)
        {
            commandInfo.UserCooldown ??= UserCooldown;
            commandInfo.GlobalCooldown ??= GlobalCooldown;

            // Checks user cooldown.
            if (commandInfo.UsersToLastUsageDatetime.ContainsKey(username) &&
                DateTime.Now - commandInfo.UsersToLastUsageDatetime[username] < TimeSpan.FromSeconds((double)commandInfo.UserCooldown))
            {
                return false;
            }

            // Checks global cooldown.
            if (commandInfo.LastUsage != default &&
                DateTime.Now - commandInfo.LastUsage < TimeSpan.FromSeconds((double)commandInfo.GlobalCooldown))
            {
                return false;
            }

            UsernameToLastCommandInfo[username] = (commandInfo, DateTime.Now);
            commandInfo.LastUsage = DateTime.Now;

            return true;
        }

        private void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            string answer = null;
            Match match = null;
            var channel = e.ChatMessage.Channel;
            var message = e.ChatMessage.Message;

            var messageCommand = GetMessageCommand(message, MessageCommands, ref match);
            if (messageCommand == null) return;

            if (messageCommand.Probability != null)
            {
                var randVal = Program.Rand.NextDouble();
                if (messageCommand.Probability < randVal) return;
            }
            
            if (messageCommand.Callback != null)
            {
                //answer = command.Callback(command, e, this, match);
            }
            else
            {
                answer = messageCommand.MultiLangAnswer[BotLang];
            }

            if (string.IsNullOrEmpty(answer)) return;
            if (messageCommand.IsMentionRequired) answer = $"@{e.ChatMessage.DisplayName}, {answer}";
            TwitchClient.SendMessage(channel, answer);
        }

        private static MessageCommandInfo GetMessageCommand(string message, IEnumerable<MessageCommandInfo> commands, ref Match match)
        {
            var command = commands.SingleOrDefault(n => n.Regex.Match(message).Success);
            if (command != null)
            {
                match = command.Regex.Match(message);
            }
            return command;
        }

        private void PubSubServiceErrorCallback(object sender, OnPubSubServiceErrorArgs e)
        {
            Logger.LogTrace(e.Exception, e.Exception.Message, e.Exception.Source);
            // _logger.LogError($"Message: {e.Exception.Message}\nStackTrace: {e.Exception.StackTrace}\nData: {e.Exception.Data}\nSource: {e.Exception.Source}");
        }

        public void Disconnect()
        {
            if (TwitchClient != null && TwitchClient.JoinedChannels.Count != 0 && TwitchClient.IsConnected)
            {
                TwitchClient.OnChatCommandReceived -= OnChatCommandReceived;
                TwitchClient.Disconnect();
            }
            if (TwitchPubSub != null)
            {
                TwitchPubSub.OnPubSubServiceError -= PubSubServiceErrorCallback;
                TwitchPubSub.Disconnect();
            }

            StreamerBot?.Disconnect();
        }
    }
}
