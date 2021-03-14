using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HerokuApp.Main;
using HerokuApp.Main.CustomTypes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TwitchLib.Client;
using TwitchLib.Client.Enums;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace HerokuApp
{
    public class HelperBotInfo
    {
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("channel_name")]
        public string ChannelName { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
               
        [JsonProperty("twitch_client")]
        private TwitchClientInfo TwitchClientInfo { get; set; }
    }

    public class Bot
    {
        [JsonProperty("enabled")]
        public bool IsEnabled { get; set; }

        [JsonProperty("channel_name")]
        public string ChannelName { get; set; }

        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }

        [JsonProperty("user_id")]
        public string UserId { get; set; }

        [JsonProperty("helper_bot")]
        public HelperBotInfo HelperBotInfo { get; set; }

        [JsonIgnore]
        public Bot HelperBot { get; set; }

        //[JsonProperty("cooldowns", IsReference = true)]
        //public Cooldowns Cooldowns { get; set; }

        [JsonProperty("user_cooldown", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(15)]
        public int UserCooldown { get; set; }

        [JsonProperty("global_cooldown", DefaultValueHandling = DefaultValueHandling.Populate)]
        [DefaultValue(5)]
        public int GlobalCooldown { get; set; }

        [JsonProperty("twitch_client")]
        private TwitchClientInfo TwitchClientInfo { get; set; }

        [JsonProperty("pubsub")]
        private PubSubInfo PubSubInfo { get; set; }

        [JsonProperty("donation_alerts")]
        public DonAlertsInfo DonAlertsInfo { get; set; }

        [JsonProperty("general_message_commands")]
        public List<GeneralCommandInfo> GeneralMessageCommandsInfo { get; set; }

        [JsonProperty("message_commands")]
        public List<MessageCommand> MessageCommands { get; set; }

        [JsonProperty("general_commands")]
        public List<GeneralCommandInfo> GeneralCommandsInfo { get; set; }

        [JsonProperty("commands")]
        public List<Command> Commands { get; set; }
        
        [JsonIgnore] public TwitchClient twitchClient;
        [JsonIgnore] public TwitchPubSub twitchPubSub;
        [JsonIgnore] public DonationAlertsClient donAlertsClient;
        [JsonIgnore] public Dictionary<string, (Command, DateTime)> lastCommand = new Dictionary<string, (Command, DateTime)>();

        public string BotLang { get; set; } = "ru";

        public Dictionary<string, bool> Modes = new Dictionary<string, bool>
        {
            { "testmode", false },
            { "l_mode", false }
        };

        public void Log(string message)
        {
            Console.WriteLine($"[{ChannelName}] {message}");
        }

        [OnDeserialized]
        internal void OnDeserializedMethod(StreamingContext context)
        {
            if (!IsEnabled) return;

            if (HelperBotInfo != null)
            {
                var infoStr = JsonConvert.SerializeObject(HelperBotInfo);
                HelperBot = JsonConvert.DeserializeObject<Bot>(infoStr);
            }
            if (TwitchClientInfo != null && TwitchClientInfo.IsEnabled) twitchClient = GetTwitchClient(ChannelName, AccessToken);
            if (PubSubInfo != null && PubSubInfo.IsEnabled) twitchPubSub = GetTwitchPubsub(ChannelName, AccessToken);
            if (DonAlertsInfo != null) donAlertsClient = GetDonAlertsClient(DonAlertsInfo, ChannelName);
        }

        private DonationAlertsClient GetDonAlertsClient(DonAlertsInfo donAlertsInfo, string channelName)
        {
            var donAlertsToChatClient = new DonationAlertsClient(donAlertsInfo.AccessToken, channelName);

            donAlertsToChatClient.OnDonationReceived += (o, args) =>
            {
                var answer = $"/me {args.Username} закинул(-а) {args.Amount} шекелей";
                if (args.MessageType == DonationMessageType.Text)
                {
                    if (string.IsNullOrEmpty(args.Message))
                    {
                        answer += "!";
                    }
                    else
                    {
                        answer += $" со словами: {args.Message}";
                    }
                }
                else if (args.MessageType == DonationMessageType.Audio)
                {
                    answer += ", записав аудио сообщение!";
                }
                else
                {
                    return;
                }
                twitchClient.SendMessage(channelName, answer);
            };

#pragma warning disable 4014
            donAlertsToChatClient.StartAsync();
#pragma warning restore 4014
            return donAlertsToChatClient;
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

            if (Commands != null)
            {
                twitchClient.OnChatCommandReceived += OnChatCommandReceived;
            }
            twitchClient.OnMessageReceived += OnMessageReceived;
            //twitchClient.OnWhisperCommandReceived += TwitchClient_OnWhisperCommandReceived;
            
            //if (TwitchClientInfo.Events != null)
            //{
            //    foreach (var item in TwitchClientInfo.Events)
            //    {
            //        UtilityFunctions.AddEventToTarget(twitchClient, item.Key, item.Value);
            //    }
            //}

            if (TwitchClientInfo.AdditionalOnMessageReceivedCallback != null)
            {
                twitchClient.OnMessageReceived += (o, e) => TwitchClientInfo.AdditionalOnMessageReceivedCallback(new CallbackArgs<OnMessageReceivedArgs> { e = e, bot = this });
            }
            if (TwitchClientInfo.OnUserTimedoutCallback != null)
            {
                twitchClient.OnUserTimedout += (s, e) => TwitchClientInfo.OnUserTimedoutCallback(new CallbackArgs<OnUserTimedoutArgs> { e = e, bot = this });
            }
            if (TwitchClientInfo.OnWhisperReceivedCallback != null)
            {
                twitchClient.OnWhisperReceived += (s, e) => TwitchClientInfo.OnWhisperReceivedCallback(new CallbackArgs<OnWhisperReceivedArgs> { e = e, bot = this });
            }

            twitchClient.OnNoPermissionError += TwitchClient_OnNoPermissionError;
            twitchClient.OnConnectionError += (s, e) => Log(e.Error.Message);
            var waitHandle = new AutoResetEvent(false);
            twitchClient.OnJoinedChannel += (s, e) => { Log("Joined to the channel."); };
            twitchClient.OnConnected += (s, e) => { Log("OnConnected"); };
            twitchClient.OnDisconnected += (s, e) => Log("The Twitch client is disconnected.");
            twitchClient.OnError += (s, e) => Log($"{e.Exception.Message}\n{e.Exception.StackTrace}");
            //twitchClient.OnUnaccountedFor += (s, e) => Log(e.RawIRC);
            //twitchClient.OnLog += (s, e) => Log(e.Data);

            twitchClient.Connect();
            return twitchClient;
        }

        private void TwitchClient_OnNoPermissionError(object sender, EventArgs e)
        {
            Log(e.ToString());
        }

        private void TwitchClient_OnWhisperCommandReceived(object sender, OnWhisperCommandReceivedArgs e)
        {
            Log($"A new whisper command - {e.Command.CommandText}");
        }

        public TwitchPubSub GetTwitchPubsub(string channelName, string accessToken)
        {
            var twitchPubSub = new TwitchPubSub();

            if (PubSubInfo.RewardsInfo != null)
            {
                twitchPubSub.OnRewardRedeemed += (s, e) =>
                {
                    if (e.Status != "UNFULFILLED") return;

                    var username = e.DisplayName;
                    Console.WriteLine("\nSomeone redeemed for a reward!");
                    Console.WriteLine($"Username: {username},\n Title: {e.RewardTitle}\n");

                    foreach (var item in PubSubInfo.RewardsInfo)
                    {
                        if (!string.Equals(e.RewardTitle, item.Name,
                            Config.stringComparison)) continue;

                        var args = new CallbackArgs<OnRewardRedeemedArgs>
                        {
                            e = e,
                            bot = this,
                            isTestMode = Modes["testmode"]
                        };
                        var answer = item.Callback(args);
                        if (string.IsNullOrEmpty(answer)) return;

                        twitchClient.SendMessage(channelName, answer);
                    }
                };
                twitchPubSub.ListenToRewards(UserId);
            }

            twitchPubSub.OnPubSubServiceConnected += delegate {
                Log("PubSub is connected");
                twitchPubSub.SendTopics(accessToken);
            };
            twitchPubSub.OnListenResponse += (_, e) => Log(e.Successful ? $"Successfully verified listening to topic: {e.Topic}" : $"Failed to listen! Error: {e.Response.Error}");
            twitchPubSub.OnPubSubServiceClosed += delegate { Log("PubSub closed"); };
            twitchPubSub.OnPubSubServiceError += PubSubServiceErrorCallback;

            twitchPubSub.Connect();
            return twitchPubSub;
        }

        public static bool IsMeOrBroadcaster(OnChatCommandReceivedArgs e) {
            return e.Command.ChatMessage.IsMe ||
                e.Command.ChatMessage.IsBroadcaster ||
                string.Equals(e.Command.ChatMessage.Username, Config.OwnerUsername, Config.stringComparison);
        }

        void OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
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

            string answer = null;
            foreach (var command in Commands)
            {
                if (command.Names.Contains(commandText))
                {
                    answer = HandleCommand(command, e);
                    break;
                }
                else if (Modes["l_mode"] && command.Names.Contains(commandText.Replace('л', 'р')))
                {
                    answer = HandleCommand(command, e, l_commandMode: true);
                    break;
                }
            }

            if (!string.IsNullOrEmpty(answer)) twitchClient.SendMessage(channel, answer);
        }

        private string HandleCommand(Command command, OnChatCommandReceivedArgs e, bool l_commandMode = false)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            if (!command.IsEnabled) return null;

            var username = e.Command.ChatMessage.Username;

            if (!Modes["testmode"])
            {
                if (!IsCommandAvailable(command, username))
                {
                    return null;
                }
                command.IncreaseUsageFrequency(username);
            }

            var args = new CallbackArgs<OnChatCommandReceivedArgs>
            {
                sender = command,
                e = e,
                bot = this,
                isTestMode = Modes["testmode"],
                lang = BotLang
            };
            var answer = command.GetAnswer(args);
            if (string.IsNullOrEmpty(answer)) return null;

            if (l_commandMode) answer = answer.Replace('р', 'л').Replace('Р', 'Л');
            if (command.IsMentionRequired) answer = $"@{e.Command.ChatMessage.DisplayName}, {answer}";
            return answer;
        }

        private bool IsCommandAvailable(Command command, string username)
        {
            command.UserCooldown ??= UserCooldown;
            command.GlobalCooldown ??= GlobalCooldown;

            // Checks user cooldown.
            if (lastCommand.ContainsKey(username) &&
                lastCommand[username].Item1 == command &&
                DateTime.Now - lastCommand[username].Item2 < TimeSpan.FromSeconds((double)command.UserCooldown))
            {
                return false;
            }

            // Checks global cooldown.
            if (command.lastUsage != null &&
                DateTime.Now - command.lastUsage < TimeSpan.FromSeconds((double)command.GlobalCooldown))
            {
                return false;
            }

            lastCommand[username] = (command, DateTime.Now);
            command.lastUsage = DateTime.Now;

            return true;
        }

        void OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            string answer = null;
            Match match = null;
            var channel = e.ChatMessage.Channel;
            var message = e.ChatMessage.Message;

            MessageCommand messageCommand = null;
            //if (messageCommands != null)
            //{
            //    command = GetMessageCommand(message, in messageCommands, ref match);
            //}
            if (MessageCommands != null)
            {
                messageCommand ??= GetMessageCommand(message, MessageCommands, ref match);
            }

            if (messageCommand == null) return;

            if (messageCommand.Callback != null)
            {
                //answer = command.Callback(command, e, this, match);
            }
            else
            {
                answer = messageCommand.Answer[BotLang];
            }

            if (string.IsNullOrEmpty(answer)) return;
            if (messageCommand.IsMentionRequired) answer = $"@{e.ChatMessage.DisplayName}, {answer}";
            twitchClient.SendMessage(channel, answer);
        }

        private MessageCommand GetMessageCommand(string message, List<MessageCommand> commands_, ref Match match)
        {
            var command = commands_.SingleOrDefault(n => n.Regex.Match(message).Success);
            if (command != null)
            {
                match = command.Regex.Match(message);
            }
            return command;
        }

        void PubSubServiceErrorCallback(object sender, OnPubSubServiceErrorArgs e)
        {
            Log($"Message: {e.Exception.Message}\nStackTrace: {e.Exception.StackTrace}\nData: {e.Exception.Data}\nSource: {e.Exception.Source}");
        }

        public void Disconnect()
        {
            if (twitchClient != null && twitchClient.JoinedChannels.Count != 0)
            {
                twitchClient.OnChatCommandReceived -= OnChatCommandReceived;
                twitchClient.Disconnect();
            }
            if (twitchPubSub != null)
            {
                twitchPubSub.OnPubSubServiceError -= PubSubServiceErrorCallback;
                twitchPubSub.Disconnect();
            }

            HelperBot?.Disconnect();
        }
    }

    public class DonAlertsInfo
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }
    }

    [JsonObject(IsReference = true)]
    public class Cooldowns
    {
        [JsonProperty("user_cooldown")]
        public int UserCooldown { get; set; }

        [JsonProperty("global_cooldown")]
        public int GlobalCooldown { get; set; }
    }
}
