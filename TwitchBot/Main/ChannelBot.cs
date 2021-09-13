using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Fasterflect;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TwitchBot.Main.Callbacks;
using TwitchBot.Main.DonationAlerts;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Models;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;

namespace TwitchBot.Main
{
    public class ChannelBot
    {
        private TwitchClient _channelTwitchClient;
        private Timer _timer;
        private const int TimerDueTime = 1 * 60 * 1000;
        private const int TimerPeriod = -1;

        public TwitchClient ChannelTwitchClient
        {
            get
            {
                switch(_channelTwitchClient)
                {
                    case {IsConnected: true}:
                        _timer.Change(TimerDueTime, TimerPeriod);
                        break;
                    case null:
                        LoadChannelTwitchClient();
                        _timer = new Timer(state => _channelTwitchClient.Disconnect(), null, TimerDueTime, TimerPeriod);
                        break;
                    default:
                        _channelTwitchClient.FullConnect();
                        _channelTwitchClient.JoinChannel(ChannelBotInfo.ChannelUsername);
                        _timer.Change(TimerDueTime, TimerPeriod);
                        break;
                }

                return _channelTwitchClient;
            }
        }

        public TwitchPubSub TwitchPubSub { get; private set; }
        public DonationAlertsClient DonationAlertsClient { get; private set; }
        public TwitchHelpers ChannelTwitchHelpers { get; private set; }
        public ILogger Logger { get; private set; }

        private ChannelBotInfo ChannelBotInfo { get; }
        
        public ChannelBot(ChannelBotInfo channelBotInfo)
        {
            ChannelBotInfo = channelBotInfo;
            CreateLogger();
            AddBotTwitchClientEventHandlers();
            
            if (ChannelBotInfo.ChannelCredentials is {AccessToken: { } accessToken})
            {
                ChannelTwitchHelpers = new TwitchHelpers(MainBotService.BotClientId, accessToken);
            }
            if (channelBotInfo.PubSubInfo is {IsEnabled: true})
                LoadTwitchPubSub();
            if (channelBotInfo.DonationAlertsInfo is {IsEnabled: true})
                LoadDonationAlertsClient();
        }
        
        private void CreateLogger()
        {
            Logger = MainBotService.LoggerProvider.CreateLogger($"{ChannelBotInfo.ChannelUsername}");
        }
        
        private void Log<T>(LogLevel level, string answer)
        {
            var context = typeof(T).Name;
            Logger.Log(level, "[{Context}] {Answer}", context, answer);
        }

        private void AddHandlerToEvent<T>(Action<EventHandler<T>> addHandlerToEventAction, string serviceCallbackId) where T : EventArgs
        {
            addHandlerToEventAction((s, e) =>
            {
                try
                {
                    using var scope = MainBotService.ScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NortagesTwitchBotContext>();
                    var args = new CallbackArgs
                    {
                        ChannelBot = this,
                        DbContext = dbContext,
                        ChannelBotInfo = dbContext.ChannelBots.Single(cb => cb.Id == ChannelBotInfo.Id),
                    };
                    typeof(EventCallbacks).CallMethod(serviceCallbackId, s, e, args);
                    dbContext.SaveChanges();
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
            });
        }

        private void AddBotTwitchClientEventHandlers()
        {
            var botTwitchClient = MainBotService.BotTwitchClient;
            botTwitchClient.JoinChannel(ChannelBotInfo.ChannelUsername);

            if (ChannelBotInfo.ChannelCommands is {Count: > 0})
            {
                AddHandlerToEvent<OnChatCommandReceivedArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnChatCommandReceived += (s, e) =>
                        {
                            if (e.Command.ChatMessage.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    "GeneralOnChatCommandReceivedCallback");
            }
            
            if (ChannelBotInfo.ChannelMessageCommands is {Count: > 0})
            {
                AddHandlerToEvent<OnMessageReceivedArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnMessageReceived += (s, e) =>
                        {
                            if (e.ChatMessage.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    "GeneralOnMessageReceivedCallback");
            }
            
            if (ChannelBotInfo.TwitchClientInfo.OnChatCommandReceivedServiceCallback 
                is {IsEnabled: true} onChatCommandReceivedServiceCallback)
            {
                AddHandlerToEvent<OnChatCommandReceivedArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnChatCommandReceived += (s, e) =>
                        {
                            if (e.Command.ChatMessage.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    onChatCommandReceivedServiceCallback.CallbackId);
            }

            if (ChannelBotInfo.TwitchClientInfo.OnMessageReceivedServiceCallback
                is {IsEnabled: true} onMessageReceivedServiceCallback)
            {
                AddHandlerToEvent<OnMessageReceivedArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnMessageReceived += (s, e) =>
                        {
                            if (e.ChatMessage.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    onMessageReceivedServiceCallback.CallbackId);
            }
            
            if (ChannelBotInfo.TwitchClientInfo.OnNewSubscriberServiceCallback
                is {IsEnabled: true} onNewSubscriberServiceCallback)
            {
                AddHandlerToEvent<OnNewSubscriberArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnNewSubscriber += (s, e) =>
                        {
                            if (e.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    onNewSubscriberServiceCallback.CallbackId);
            }
            
            if (ChannelBotInfo.TwitchClientInfo.OnReSubscriberServiceCallback
                is {IsEnabled: true} onReSubscriberServiceCallback)
            {
                AddHandlerToEvent<OnReSubscriberArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnReSubscriber += (s, e) =>
                        {
                            if (e.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    onReSubscriberServiceCallback.CallbackId);
            }
            
            if (ChannelBotInfo.TwitchClientInfo.OnGiftedSubscriptionServiceCallback
                is {IsEnabled: true} onGiftedSubscriptionServiceCallback)
            {
                AddHandlerToEvent<OnGiftedSubscriptionArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnGiftedSubscription += (s, e) =>
                        {
                            if (e.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    onGiftedSubscriptionServiceCallback.CallbackId);
            }
            
            if (ChannelBotInfo.TwitchClientInfo.OnUserTimedoutServiceCallback
                is {IsEnabled: true} onUserTimedoutServiceCallback)
            {
                AddHandlerToEvent<OnUserTimedoutArgs>(
                    eventHandler =>
                    {
                        botTwitchClient.OnUserTimedout += (s, e) =>
                        {
                            if (e.UserTimeout.Channel != ChannelBotInfo.ChannelUsername) return;
                            eventHandler(s, e);
                        };
                    },
                    onUserTimedoutServiceCallback.CallbackId);
            }
            
            // ReSharper disable once InvertIf
            if (ChannelBotInfo.TwitchClientInfo.OnWhisperReceivedServiceCallback
                is {IsEnabled: true} onWhisperReceivedServiceCallback)
            {
                AddHandlerToEvent<OnWhisperReceivedArgs>(
                    eventHandler => botTwitchClient.OnWhisperReceived += eventHandler,
                    onWhisperReceivedServiceCallback.CallbackId);
            }
        }
        
        private void LoadChannelTwitchClient()
        {
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1,
            };
            
            var customClient = new WebSocketClient(clientOptions);
            _channelTwitchClient = new TwitchClient(customClient);
            var credentials = new ConnectionCredentials(MainBotService.BotUsername, ChannelBotInfo.ChannelCredentials.AccessToken);
            _channelTwitchClient.Initialize(credentials, ChannelBotInfo.ChannelUsername);
            _channelTwitchClient.OnConnectionError += (_, e) =>
            {
                if (e == null) throw new ArgumentNullException(nameof(e));
                Log<TwitchClient>(LogLevel.Error, e.Error.Message);
            };
            _channelTwitchClient.OnNoPermissionError += (_, _) => Log<TwitchClient>(LogLevel.Warning, "No permission.");
            _channelTwitchClient.OnJoinedChannel += (_, _) => { Log<TwitchClient>(LogLevel.Information, "Joined to the channel."); };
            _channelTwitchClient.OnConnected += (_, _) => { Log<TwitchClient>(LogLevel.Information, "Connected."); };
            _channelTwitchClient.OnDisconnected += (_, _) => Log<TwitchClient>(LogLevel.Warning, "Disconnected.");
            _channelTwitchClient.OnError += (_, e) => Log<TwitchClient>(LogLevel.Error, $"{e.Exception.Message}\n{e.Exception.StackTrace}");

            _channelTwitchClient.FullConnect();
        }
        
        private void LoadTwitchPubSub()
        {
            TwitchPubSub = new TwitchPubSub();

            var hasRewardRedemptions = ChannelBotInfo.RewardRedemptions is {Count: > 0};
            var hasOnRewardRedeemedServiceCallback =
                ChannelBotInfo.PubSubInfo.OnRewardRedeemedServiceCallbackId != null;
            
            if (hasRewardRedemptions)
            {
                AddHandlerToEvent<OnRewardRedeemedArgs>(eventHandler => TwitchPubSub.OnRewardRedeemed += eventHandler, "GeneralOnRewardRedeemedCallback");
            }
            
            if (hasOnRewardRedeemedServiceCallback)
            {
                var onRewardRedeemedCallback = ChannelBotInfo.PubSubInfo.OnRewardRedeemedServiceCallback;
                AddHandlerToEvent<OnRewardRedeemedArgs>(eventHandler => TwitchPubSub.OnRewardRedeemed += eventHandler, onRewardRedeemedCallback.CallbackId);    
            }
            
            if (hasRewardRedemptions || hasOnRewardRedeemedServiceCallback)
                TwitchPubSub.ListenToRewards(ChannelBotInfo.ChannelUserId);

            TwitchPubSub.OnListenResponse += (_, e) =>
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
            TwitchPubSub.OnPubSubServiceClosed += delegate { Log<TwitchPubSub>(LogLevel.Warning, "Closed."); };
            TwitchPubSub.OnPubSubServiceError += PubSubServiceErrorCallback;

            TwitchPubSub.OnPubSubServiceConnected += delegate {
                Log<TwitchPubSub>(LogLevel.Information, "Connected.");
                TwitchPubSub.SendTopics(ChannelBotInfo.BotCredentials.AccessToken);
            };

            TwitchPubSub.Connect();
        }
        
        private void LoadDonationAlertsClient()
        {
            DonationAlertsClient = new DonationAlertsClient(ChannelBotInfo.DonationAlertsInfo.AccessToken);
            
            if (ChannelBotInfo.DonationAlertsInfo.OnDonationReceivedServiceCallback is {IsEnabled: true} onDonationReceivedServiceCallback)
            {
                AddHandlerToEvent<OnDonationAlertArgs>(eventHandler => DonationAlertsClient.OnDonationAlert += eventHandler, onDonationReceivedServiceCallback.CallbackId);    
            }
            if (ChannelBotInfo.DonationAlertsInfo.OnDonationGoalUpdateReceivedServiceCallback is {IsEnabled: true} onDonationGoalUpdateReceivedServiceCallback)
            {
                AddHandlerToEvent<OnDonationAlertArgs>(eventHandler => DonationAlertsClient.OnDonationAlert += eventHandler, onDonationGoalUpdateReceivedServiceCallback.CallbackId);    
            }

            DonationAlertsClient.ListenToDonationAlerts();
            DonationAlertsClient.SetLogger(Logger);
            DonationAlertsClient.Connect();
        }

        public void Stop()
        {
            if (_channelTwitchClient is {IsConnected: true})
                _channelTwitchClient.Disconnect();
            
            if (TwitchPubSub != null)
            {
                TwitchPubSub.OnPubSubServiceError -= PubSubServiceErrorCallback;
                TwitchPubSub.Disconnect();
            }

            DonationAlertsClient?.Close();
        }
        
        private void PubSubServiceErrorCallback(object sender, OnPubSubServiceErrorArgs e)
        {
            Logger.LogTrace(e.Exception, e.Exception.Message, e.Exception.Source);
            // _logger.LogError($"Message: {e.Exception.Message}\nStackTrace: {e.Exception.StackTrace}\nData: {e.Exception.Data}\nSource: {e.Exception.Source}");
        }

        public void OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            Log<ChannelBot>(LogLevel.Information, $"{ChannelBotInfo.ChannelUsername} is online!");
        }

        public void OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            Log<ChannelBot>(LogLevel.Information, $"{ChannelBotInfo.ChannelUsername} is offline!");
        }
    }
}