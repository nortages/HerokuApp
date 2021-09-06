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
using RestSharp;
using TwitchBot.Main.Callbacks;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Main.Hearthstone;
using TwitchBot.Models;
using TwitchLib.Api.Services;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchBot.Main
{
    public class MainBotService : IHostedService
    {
        private static ILogger _logger;
        private static IConfiguration _configuration;
        public static ILoggerProvider LoggerProvider { get; set; }
        public static IServiceScopeFactory ScopeFactory { get; set; }
        public static IWebHostEnvironment CurrentEnvironment { get; set; }

        public static string OwnerUsername { get; set; }
        public static string BotUsername { get; set; }
        public static string BotUserId { get; set; }
        public static string BotClientId { get; set; }
        public static string BattleNetClientId { get; set; }
        public static string BattleNetSecret { get; set; }

        private static LiveStreamMonitorService _streamMonitorService;
        public static HearthstoneApiClient HearthstoneApiClient { get; private set; }
        public static TwitchHelpers BotTwitchHelpers { get; private set; }
        public static TwitchClient BotTwitchClient { get; private set; }
        private List<ChannelBot> ChannelBots { get; } = new();
        public static StringComparison StringComparison => StringComparison.OrdinalIgnoreCase;
        public static RegexOptions RegexOptions => RegexOptions.Compiled | RegexOptions.IgnoreCase;

        public MainBotService(
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
            BattleNetClientId = GetSecret("BATTLE_NET_CLIENT_ID");
            BattleNetSecret = GetSecret("BATTLE_NET_SECRET");
        }

        private static void Log<T>(LogLevel level, string answer)
        {
            var context = typeof(T).Name;
            _logger.Log(level, "[{Context}] {Answer}", context, answer);
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger = LoggerProvider.CreateLogger(GetType().Name);
            HearthstoneApiClient = new HearthstoneApiClient(BattleNetClientId, BattleNetSecret);

            using var scope = ScopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<NortagesTwitchBotContext>();

            RefreshAccessTokens(dbContext);
            var botAccessToken = dbContext.Credentials.Single(c => c.Id == 1).AccessToken;
            BotTwitchHelpers = new TwitchHelpers(BotClientId, botAccessToken);
            LoadBotTwitchClient(botAccessToken);
            
            
            _streamMonitorService = new LiveStreamMonitorService(BotTwitchHelpers.TwitchApi);
            
            var channelBotInfos = await dbContext.ChannelBots
                .Where(c => c.IsEnabled)
                .ToListAsync(cancellationToken);
            
            foreach (var channelBotInfo in channelBotInfos)
            {
                var channelBot = new ChannelBot(channelBotInfo);
                ChannelBots.Add(channelBot);
                _streamMonitorService.OnStreamOnline += channelBot.OnStreamOnline;
                _streamMonitorService.OnStreamOffline += channelBot.OnStreamOffline;
            }
            
            _streamMonitorService.SetChannelsById(channelBotInfos.Select(c => c.ChannelUserId).ToList());
            _streamMonitorService.Start();
        }
        
        private static void AddHandlerToEvent<T>(
            Action<EventHandler<T>> addHandlerToEventAction, 
            string serviceCallbackId) where T : EventArgs
        {
            addHandlerToEventAction((s, e) =>
            {
                try
                {
                    using var scope = ScopeFactory.CreateScope();
                    var dbContext = scope.ServiceProvider.GetRequiredService<NortagesTwitchBotContext>();
                    var args = new CallbackArgs
                    {
                        DbContext = dbContext,
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

        private static void LoadBotTwitchClient(string accessToken)
        {
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1,
            };
            
            var customClient = new WebSocketClient(clientOptions);
            BotTwitchClient = new TwitchClient(customClient);
            var credentials = new ConnectionCredentials(BotUsername, accessToken);
            BotTwitchClient.Initialize(credentials);
            BotTwitchClient.AddChatCommandIdentifier('?');
            
            AddHandlerToEvent<OnWhisperCommandReceivedArgs>(
                eventHandler => BotTwitchClient.OnWhisperCommandReceived += eventHandler, 
                "GeneralOnWhisperCommandReceivedCallback");
            
            BotTwitchClient.OnConnectionError += (_, e) =>
            {
                if (e == null) throw new ArgumentNullException(nameof(e));
                Log<TwitchClient>(LogLevel.Error, (e.Error.Message));
            };
            BotTwitchClient.OnNoPermissionError += (_, _) => Log<TwitchClient>(LogLevel.Warning, "No permission.");
            BotTwitchClient.OnJoinedChannel += (_, _) => { Log<TwitchClient>(LogLevel.Information, "Joined to the channel."); };
            BotTwitchClient.OnDisconnected += (_, _) => Log<TwitchClient>(LogLevel.Warning, "Disconnected.");
            BotTwitchClient.OnError += (_, e) => Log<TwitchClient>(LogLevel.Error, $"{e.Exception.Message}\n{e.Exception.StackTrace}");
            BotTwitchClient.OnConnected += (_, _) =>
            {
                Log<TwitchClient>(LogLevel.Information, "Connected.");
            };
            BotTwitchClient.FullConnect();
        }
        
        private static void RefreshAccessTokens(NortagesTwitchBotContext dbContextCredentials)
        {
            var client = new RestClient("https://twitchtokengenerator.com");
            foreach (var credentials in dbContextCredentials.Credentials)
            {
                if (credentials.ExpirationDate - DateTime.Now > TimeSpan.FromDays(30))
                    continue;

                var request = new RestRequest($"/api/refresh/{credentials.RefreshToken}");
                var response = client.Execute(request, Method.GET);
                var jObjectResponse = JObject.Parse(response.Content);
                if (!jObjectResponse.Value<bool>("success"))
                    continue;
                
                var newAccessToken = jObjectResponse.Value<string>("token");
                credentials.AccessToken = newAccessToken;
                credentials.ExpirationDate = DateTime.Now + TimeSpan.FromDays(60);

                dbContextCredentials.Update(credentials);
            }
            dbContextCredentials.SaveChanges();
        }

        private static string GetSecret(string key)
        {
            return Environment.GetEnvironmentVariable(key) ?? _configuration[key];
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _streamMonitorService.Stop();
            
            if (BotTwitchClient != null && BotTwitchClient.JoinedChannels.Count != 0 && BotTwitchClient.IsConnected)
            {
                BotTwitchClient.Disconnect();
            }
            
            foreach (var channelBot in ChannelBots)
            {
                channelBot.Stop();
            }
            
            return Task.CompletedTask;
        }
    }
}