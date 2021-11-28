using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Fasterflect;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using NpgsqlTypes;
using RestSharp;
using TwitchBot.Main.Callbacks;
using TwitchBot.Main.DonationAlerts;
using TwitchBot.Main.Enums;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Main.Hearthstone;
using TwitchBot.Main.Interfaces;
using TwitchBot.Models;
using TwitchLib.Api.Services;
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
    public class BotService : IHostedService
    {
        public const string LogFormat = "[{context}] {message}";
        private static ILogger _logger;
        private static IConfiguration _configuration;

        private static LiveStreamMonitorService _streamMonitorService;

        public BotService(
            IConfiguration configuration,
            ILoggerProvider loggerProvider,
            IServiceScopeFactory scopeFactory,
            IWebHostEnvironment environment)
        {
            _configuration = configuration;
            LoggerProvider = loggerProvider;
            ScopeFactory = scopeFactory;
            CurrentEnvironment = environment;

            OwnerUsername = GetSecret("OWNER_USERNAME");
            BotUsername = GetSecret("BOT_USERNAME");
            BotUserId = GetSecret("BOT_USER_ID");
            BotClientId = GetSecret("BOT_CLIENT_ID");
            BotClientSecret = GetSecret("BOT_CLIENT_SECRET");
            BattleNetClientId = GetSecret("BATTLE_NET_CLIENT_ID");
            BattleNetSecret = GetSecret("BATTLE_NET_SECRET");
        }

        public static ILoggerProvider LoggerProvider { get; set; }
        public static IServiceScopeFactory ScopeFactory { get; set; }
        public static IWebHostEnvironment CurrentEnvironment { get; set; }

        public static string OwnerUsername { get; set; }
        public static string BotUsername { get; set; }
        public static string BotUserId { get; set; }
        public static string BotClientId { get; set; }
        public static string BotClientSecret { get; set; }
        public static string BattleNetClientId { get; set; }
        public static string BattleNetSecret { get; set; }
        public static HearthstoneApiClient HearthstoneApiClient { get; private set; }
        public static TwitchHelpers BotTwitchHelpers { get; private set; }
        public static TwitchClient BotTwitchClient { get; private set; }
        public static TwitchPubSub BotTwitchPubSub { get; private set; }
        private static List<ChannelBot> ChannelBots { get; } = new();
        public static StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;
        public static RegexOptions RegexOptions => RegexOptions.Compiled | RegexOptions.IgnoreCase;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger = LoggerProvider.CreateLogger(nameof(BotService));
            HearthstoneApiClient = new HearthstoneApiClient(BattleNetClientId, BattleNetSecret);

            using var scope = ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NortagesTwitchBotDbContext>();

            RefreshAccessTokens(dbContext);
            var botAccessToken = dbContext.Credentials.Single(c => c.Id == 1).AccessToken;
            BotTwitchHelpers = new TwitchHelpers(BotClientId, BotClientSecret, botAccessToken);

            var channelBotInfos = await LoadChannelInfos(dbContext, cancellationToken: cancellationToken);
            
            LoadBotTwitchClient(botAccessToken, channelBotInfos, dbContext);
            LoadBotTwitchPubSub(botAccessToken, channelBotInfos, dbContext);

            foreach (var channelBotInfo in channelBotInfos)
            {
                var channelBot = new ChannelBot(channelBotInfo, LoggerProvider);
                ChannelBots.Add(channelBot);
                channelBot.Init(dbContext);
            }

            LoadLiveStreamMonitorService(channelBotInfos);
        }

        public static async Task<List<ChannelInfo>> LoadChannelInfos(NortagesTwitchBotDbContext dbContext, int? channelInfoId = null, CancellationToken cancellationToken = default)
        {
            var channelInfos = dbContext.ChannelInfos.Where(c => c.IsEnabled);
            if (channelInfoId is not null)
                channelInfos = channelInfos.Where(ci => ci.Id == channelInfoId);
            
            return await channelInfos
                // Channel credentials
                .Include(ci => ci.BotCredentials)
                .Include(ci => ci.ChannelCredentials)
                
                // Channel commands/option
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(cc => cc.ChannelInfo)
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(cc => cc.UserChannelCommands)
                        .ThenInclude(ucc => ucc.ChannelCommand)
                // .ThenInclude(cc => cc.Command)
                
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(c => c.Command.Option)
                        .ThenInclude(o => o.CallbackInfo)
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(c => c.Command.Option)
                        .ThenInclude(o => o.MultiLangAnswer)
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(c => c.Command.MiniGame)
                
                // Channel commands/option/option
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(c => c.Command.Option)
                        .ThenInclude(o => o.ChildOptions.Where(co => co.IsEnabled))
                            .ThenInclude(o => o.CallbackInfo)
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(c => c.Command.Option)
                        .ThenInclude(o => o.ChildOptions.Where(co => co.IsEnabled))
                            .ThenInclude(o => o.MultiLangAnswer)
                
                // Channel commands/option/option
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(c => c.Command.Option)
                        .ThenInclude(o => o.ChildOptions.Where(co => co.IsEnabled))
                            .ThenInclude(o => o.ChildOptions.Where(co => co.IsEnabled))
                                .ThenInclude(o => o.CallbackInfo)
                .Include(ci => ci.ChannelCommands.Where(cc => cc.IsEnabled))
                    .ThenInclude(c => c.Command.Option)
                        .ThenInclude(o => o.ChildOptions.Where(co => co.IsEnabled))
                            .ThenInclude(o => o.ChildOptions.Where(co => co.IsEnabled))
                                .ThenInclude(o => o.MultiLangAnswer)
                
                // Channel message commands
                .Include(c => c.ChannelMessageCommands.Where(cmc => cmc.IsEnabled))
                    .ThenInclude(c => c.MessageCommand.Option)
                        .ThenInclude(o => o.CallbackInfo)
                .Include(c => c.ChannelMessageCommands.Where(cmc => cmc.IsEnabled))
                    .ThenInclude(c => c.MessageCommand.Option)
                        .ThenInclude(o => o.MultiLangAnswer)
                
                // Channel reward redemptions
                .Include(ci => ci.ChannelRewardRedemptions.Where(crr => crr.IsEnabled))
                    .ThenInclude(c => c.RewardRedemption.CallbackInfo)
                .Include(ci => ci.ChannelRewardRedemptions.Where(crr => crr.IsEnabled))
                    .ThenInclude(c => c.RewardRedemption.MiniGame)
                
                // Channel mini games
                .Include(ci => ci.ChannelMiniGames.Where(cm => cm.IsEnabled))
                    .ThenInclude(cmg => cmg.MiniGame)
                
                // Channel services
                .Include(ci => ci.ChannelServices)
                    .ThenInclude(cs => cs.Credentials)
                .Include(ci => ci.ChannelServices)
                    .ThenInclude(cs => cs.Service)
                
                // Channel service events
                .Include(ci => ci.ChannelServiceEventCallbacks.Where(cse => cse.IsEnabled))
                    .ThenInclude(cse => cse.ServiceEventCallback.CallbackInfo)
                .Include(ci => ci.ChannelServiceEventCallbacks.Where(cse => cse.IsEnabled))
                    .ThenInclude(cse => cse.ServiceEventCallback.ServiceEvent)
                .Include(ci => ci.ChannelServiceEventCallbacks.Where(cse => cse.IsEnabled))
                    .ThenInclude(cse => cse.ServiceEventCallback.MiniGame)
                
                .AsSplitQuery()
                .ToListAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _streamMonitorService.Stop();

            if (BotTwitchClient != null && BotTwitchClient.JoinedChannels.Count != 0 && BotTwitchClient.IsConnected)
                BotTwitchClient.Disconnect();

            if (BotTwitchPubSub != null)
            {
                BotTwitchPubSub.OnPubSubServiceError -= OnPubSubServiceError;
                BotTwitchPubSub.Disconnect();
            }

            foreach (var channelBot in ChannelBots) channelBot.Stop();

            return Task.CompletedTask;
        }

        private static string GetChannelNameFromEventHandlerArgs(object eventHandlerArgs)
        {
            return eventHandlerArgs switch
            {
                OnChatCommandReceivedArgs onChatCommandReceivedArgs => onChatCommandReceivedArgs.Command.ChatMessage.Channel,
                OnMessageReceivedArgs onMessageReceivedArgs => onMessageReceivedArgs.ChatMessage.Channel,
                OnGiftedSubscriptionArgs onGiftedSubscriptionArgs => onGiftedSubscriptionArgs.Channel,
                OnCommunitySubscriptionArgs onCommunitySubscriptionArgs => onCommunitySubscriptionArgs.Channel,
                OnUserTimedoutArgs onUserTimedoutArgs => onUserTimedoutArgs.UserTimeout.Channel,
                OnNewSubscriberArgs onNewSubscriberArgs => onNewSubscriberArgs.Channel,
                OnReSubscriberArgs onReSubscriberArgs => onReSubscriberArgs.Channel,
                OnDonationAlertArgs onDonationAlertArgs => onDonationAlertArgs.ChannelUsername,
                OnDonationGoalUpdateArgs onDonationGoalUpdateArgs => onDonationGoalUpdateArgs.ChannelUsername,
                _ => null
            };
        }

        private static void OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            var channelBot = ChannelBots.Single(cb => cb.ChannelUserId == e.Stream.UserId);
            channelBot.OnStreamOnline(sender, e);
        }

        private static void OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            var channelBot = ChannelBots.Single(cb => cb.ChannelUserId == e.Stream.UserId);
            channelBot.OnStreamOffline(sender, e);
        }

        private static ChannelBot GetChannelBotFromEventHandlerArgs(object eventHandlerArgs)
        {
            ChannelBot channelBot;
            
            switch (eventHandlerArgs)
            {
                case OnRewardRedeemedArgs onRewardRedeemedArgs:
                    channelBot = ChannelBots.Single(c =>
                        string.Equals(c.ChannelUserId, onRewardRedeemedArgs.ChannelId, StringComparison));
                    break;
                case OnWhisperCommandReceivedArgs onWhisperCommandReceivedArgs:
                    channelBot = ChannelBots.SingleOrDefault(c => string.Equals(c.ChannelUsername,
                        onWhisperCommandReceivedArgs.Command.ArgumentsAsList.Last(), StringComparison));
                    break;
                default:
                {
                    var channelName = GetChannelNameFromEventHandlerArgs(eventHandlerArgs);
                    if (string.IsNullOrEmpty(channelName))
                        return null;
                    channelBot = ChannelBots.SingleOrDefault(c =>
                        string.Equals(c.ChannelUsername, channelName, StringComparison));
                    break;
                }
            }

            return channelBot;
        }
        
        public static void AddEventHandlersToService(object serviceInstance, ServiceEvent[] serviceEvents)
        {
            if (serviceEvents == null)
                throw new ArgumentNullException(nameof(serviceEvents));
            
            foreach (var serviceEvent in serviceEvents)
            {
                var serviceEventName = serviceEvent.Name;
                serviceInstance.AddHandler(serviceEventName, handlerParams =>
                {
                    var eventHandlerArgs = handlerParams[1];
                    var channelBot = GetChannelBotFromEventHandlerArgs(eventHandlerArgs);
                    if (channelBot is null)
                        return null;
                    var channelInfo = channelBot.ChannelInfo;

                    var callbackIdToCallMethodTarget = new Dictionary<string, object>();
                    
                    switch (serviceEventName)
                    {
                        case nameof(TwitchClient.OnChatCommandReceived):
                            callbackIdToCallMethodTarget.Add(nameof(EventCallbacks.GeneralOnChatCommandReceivedCallback), typeof(EventCallbacks));
                            break;
                        case nameof(TwitchClient.OnMessageReceived):
                            callbackIdToCallMethodTarget.Add(nameof(EventCallbacks.GeneralOnMessageReceivedCallback), typeof(EventCallbacks));
                            break;
                        case nameof(TwitchClient.OnWhisperCommandReceived):
                            callbackIdToCallMethodTarget.Add(nameof(EventCallbacks.GeneralOnWhisperCommandReceivedCallback), typeof(EventCallbacks));
                            break;
                        case nameof(TwitchPubSub.OnRewardRedeemed):
                            callbackIdToCallMethodTarget.Add(nameof(EventCallbacks.GeneralOnRewardRedeemedCallback), typeof(EventCallbacks));
                            break;
                    }
                    
                    var channelServiceEventCallbacks = channelInfo.ChannelServiceEventCallbacks.Where(cse => cse.ServiceEventCallback.ServiceEvent.Name == serviceEventName);
                    foreach (var channelServiceEventCallback in channelServiceEventCallbacks)
                    {
                        object callMethodTarget;
                        var miniGame = channelServiceEventCallback.ServiceEventCallback.MiniGame;
                        if (miniGame is null)
                        {
                            callMethodTarget = typeof(EventCallbacks);
                        }
                        else
                        {
                            if (!miniGame.IsEnabled)
                                continue;
                            
                            var miniGameInstance = channelBot.MiniGameNameToInstance[miniGame.Id];
                            callMethodTarget = miniGameInstance;
                        }
                        callbackIdToCallMethodTarget.Add(channelServiceEventCallback.ServiceEventCallback.CallbackInfo.Id, callMethodTarget);
                    }
                    
                    InvokeServiceEventCallbacks(callbackIdToCallMethodTarget, handlerParams, channelBot);
                    return null;
                });
            }
        }

        private static void InvokeServiceEventCallbacks(Dictionary<string, object> callbackIdToCallMethodTarget, object[] handlerParams,
            ChannelBot channelBot)
        {
            using var scope = ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NortagesTwitchBotDbContext>();
            
            var args = new CallbackArgs
            {
                Logger = channelBot.Logger,
                ChannelBot = channelBot,
                DbContext = dbContext,
                ChannelInfo = channelBot.ChannelInfo
            };

            handlerParams = handlerParams.Append(args).ToArray();
            foreach (var (callbackId, callMethodTarget) in callbackIdToCallMethodTarget)
            {
                try
                {
                    callMethodTarget.CallMethod(callbackId, handlerParams);
                }
                catch (Exception exception)
                {
                    Console.WriteLine(exception);
                    throw;
                }
            }
            dbContext.SaveChanges();
        }

        private static void LoadBotTwitchClient(string accessToken, IEnumerable<ChannelInfo> channelBotInfos,
            NortagesTwitchBotDbContext dbContext)
        {
            var twitchClientService = dbContext.Services.Single(s => s.Name == nameof(TwitchClient));
            
            static void TwitchClientLog(LogLevel level, string s)
            {
                _logger.Log(level, LogFormat, nameof(TwitchClient), s);
            }

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1
            };

            var customClient = new WebSocketClient(clientOptions);
            BotTwitchClient = new TwitchClient(customClient);
            var credentials = new ConnectionCredentials(BotUsername, accessToken);
            BotTwitchClient.Initialize(credentials);
            BotTwitchClient.AddChatCommandIdentifier('?');

            AddEventHandlersToService(BotTwitchClient, twitchClientService.ServiceEvents.ToArray());

            BotTwitchClient.OnConnectionError += (_, e) =>
            {
                if (e == null) 
                    throw new ArgumentNullException(nameof(e));
                
                TwitchClientLog(LogLevel.Error, e.Error.Message);
            };
            BotTwitchClient.OnNoPermissionError += (_, _) => TwitchClientLog(LogLevel.Warning, "No permission.");
            BotTwitchClient.OnJoinedChannel += (_, _) => TwitchClientLog(LogLevel.Information, "Joined to the channel.");
            BotTwitchClient.OnDisconnected += (_, _) => TwitchClientLog(LogLevel.Warning, "Disconnected.");
            BotTwitchClient.OnError += (_, e) => TwitchClientLog(LogLevel.Error, $"{e.Exception.Message}\n{e.Exception.StackTrace}");
            BotTwitchClient.OnConnected += (_, _) => { TwitchClientLog(LogLevel.Information, "Connected."); };
            BotTwitchClient.OnIncorrectLogin += (sender, args) => TwitchClientLog(LogLevel.Error, args.ToString());

            BotTwitchClient.FullConnect();

            foreach (var channelBotInfo in channelBotInfos)
                BotTwitchClient.JoinChannel(channelBotInfo.ChannelUsername);
        }

        private static void LoadBotTwitchPubSub(string botAccessToken, IEnumerable<ChannelInfo> channelBotInfos,
            NortagesTwitchBotDbContext dbContext)
        {
            var twitchPubSubService = dbContext.Services.Single(s => s.Name == nameof(TwitchPubSub));
            
            static void TwitchPubSubLog(LogLevel level, string s)
            {
                _logger.Log(level, LogFormat, nameof(TwitchPubSub), s);
            }

            BotTwitchPubSub = new TwitchPubSub();

            AddEventHandlersToService(BotTwitchPubSub, twitchPubSubService.ServiceEvents.ToArray());

            BotTwitchPubSub.OnListenResponse += (_, e) =>
            {
                if (e.Successful)
                    TwitchPubSubLog(LogLevel.Information, $"Successfully verified listening to topic: {e.Topic}.");
                else
                    TwitchPubSubLog(LogLevel.Error, $"Failed to listen. Error: {e.Response.Error}.");
            };
            BotTwitchPubSub.OnPubSubServiceClosed += delegate { TwitchPubSubLog(LogLevel.Warning, "Closed."); };
            BotTwitchPubSub.OnPubSubServiceError += OnPubSubServiceError;

            BotTwitchPubSub.OnPubSubServiceConnected += delegate
            {
                TwitchPubSubLog(LogLevel.Information, "Connected.");
                BotTwitchPubSub.SendTopics(botAccessToken);
            };

            foreach (var channelBotInfo in channelBotInfos)
                BotTwitchPubSub.ListenToRewards(channelBotInfo.ChannelUserId);

            BotTwitchPubSub.Connect();
        }

        private static void LoadLiveStreamMonitorService(List<ChannelInfo> channelBotInfos)
        {
            if (channelBotInfos == null) throw new ArgumentNullException(nameof(channelBotInfos));
            _streamMonitorService = new LiveStreamMonitorService(BotTwitchHelpers.TwitchApi);
            _streamMonitorService.OnStreamOnline += OnStreamOnline;
            _streamMonitorService.OnStreamOffline += OnStreamOffline;
            _streamMonitorService.SetChannelsById(channelBotInfos.Select(c => c.ChannelUserId).ToList());
            _streamMonitorService.Start();
        }

        private static void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
        {
            _logger.LogTrace(e.Exception, e.Exception.Message, e.Exception.Source);
        }

        private static void RefreshAccessTokens(NortagesTwitchBotDbContext dbDbContextCredentials)
        {
            var client = new RestClient("https://id.twitch.tv");
            foreach (var credentials in dbDbContextCredentials.Credentials)
            {
                if (credentials.RefreshToken is null || credentials.ExpirationDate - DateTime.Now > TimeSpan.FromDays(30))
                    continue;

                var request = new RestRequest($"oauth2/token");
                request.AddParameter("client_id", BotClientId, ParameterType.GetOrPost);
                request.AddParameter("client_secret", BotClientSecret, ParameterType.GetOrPost);
                request.AddParameter("refresh_token", credentials.RefreshToken, ParameterType.GetOrPost);
                request.AddParameter("grant_type", "refresh_token", ParameterType.GetOrPost);
                
                var response = client.Execute(request, Method.POST);
                var jObjectResponse = JObject.Parse(response.Content);

                var newAccessToken = jObjectResponse.Value<string>("access_token");
                var expiresInSeconds = jObjectResponse.Value<int>("expires_in");
                credentials.AccessToken = newAccessToken;
                credentials.ExpirationDate = DateTime.Now.ToUniversalTime() + TimeSpan.FromSeconds(expiresInSeconds);

                dbDbContextCredentials.Update(credentials);
            }

            dbDbContextCredentials.SaveChanges();
        }

        public static string GetSecret(string key)
        {
            return Environment.GetEnvironmentVariable(key) ?? _configuration[key];
        }
    }
}