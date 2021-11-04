using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TwitchBot.Main.DonationAlerts;
using TwitchBot.Main.ExtensionsMethods;
using TwitchBot.Main.Interfaces;
using TwitchBot.Main.MiniGames;
using TwitchBot.Models;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace TwitchBot.Main
{
    public class ChannelBot
    {
        private Timer _timer;
        private const int TimerPeriod = -1;
        private const int TimerDueTime = 1 * 60 * 1000;
        private TwitchClient _channelTwitchClient;
        public ChannelInfo ChannelInfo { get; set; }

        public ChannelBot(ChannelInfo channelInfo, ILoggerProvider loggerProvider)
        {
            ChannelInfo = channelInfo;
            Logger = loggerProvider.CreateLogger(channelInfo.ChannelUsername);
            ChannelUsername = channelInfo.ChannelUsername;
            ChannelUserId = channelInfo.ChannelUserId;
        }

        public string ChannelUsername { get; }
        public string ChannelUserId { get; }
        public TwitchHelpers ChannelTwitchHelpers { get; private set; }
        public DonationAlertsClient DonationAlertsClient { get; private set; }
        public Dictionary<int, IMiniGame> MiniGameNameToInstance { get; set; } = new();

        public TwitchClient ChannelTwitchClient
        {
            get
            {
                switch (_channelTwitchClient)
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
                        _channelTwitchClient.JoinChannel(ChannelInfo.ChannelUsername);
                        _timer.Change(TimerDueTime, TimerPeriod);
                        break;
                }

                return _channelTwitchClient;
            }
        }

        public ILogger Logger { get; }
        public static string LogFormat => BotService.LogFormat;

        public void Init(NortagesTwitchBotDbContext dbContext)
        {
            if (ChannelInfo.ChannelCredentials is {AccessToken: { } accessToken})
                ChannelTwitchHelpers = new TwitchHelpers(BotService.BotClientId, accessToken);
            LoadDonationAlertsClient(dbContext);

            foreach (var channelMiniGame in ChannelInfo.ChannelMiniGames)
            {
                var miniGameInfo = channelMiniGame.MiniGame;
                // Getting the full namespace as a string using one stored
                // in the Namespace property of any MiniGame type referenced directly.
                var miniGamesFullNamespace = typeof(BattlegroundsDuel).Namespace;
                var miniGameType = Type.GetType($"{miniGamesFullNamespace}.{miniGameInfo.Name}");
                var miniGameInstance = (IMiniGame) Activator.CreateInstance(miniGameType);
                MiniGameNameToInstance.Add(miniGameInfo.Id, miniGameInstance);
            }
        }

        private void LoadChannelTwitchClient()
        {
            void TwitchClientLog(LogLevel level, string s)
            {
                Logger.Log(level, LogFormat, nameof(TwitchClient), s);
            }

            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 100,
                ThrottlingPeriod = TimeSpan.FromSeconds(30),
                SendDelay = 1
            };

            var customClient = new WebSocketClient(clientOptions);
            _channelTwitchClient = new TwitchClient(customClient);
            var credentials =
                new ConnectionCredentials(BotService.BotUsername, ChannelInfo.ChannelCredentials.AccessToken);
            _channelTwitchClient.Initialize(credentials, ChannelInfo.ChannelUsername);
            _channelTwitchClient.OnConnectionError += (_, e) =>
            {
                if (e == null) throw new ArgumentNullException(nameof(e));
                TwitchClientLog(LogLevel.Error, e.Error.Message);
            };
            _channelTwitchClient.OnNoPermissionError += (_, _) => TwitchClientLog(LogLevel.Warning, "No permission.");
            _channelTwitchClient.OnJoinedChannel += (_, _) =>
            {
                TwitchClientLog(LogLevel.Information, "Joined to the channel.");
            };
            _channelTwitchClient.OnConnected += (_, _) => { TwitchClientLog(LogLevel.Information, "Connected."); };
            _channelTwitchClient.OnDisconnected += (_, _) => TwitchClientLog(LogLevel.Warning, "Disconnected.");
            _channelTwitchClient.OnError += (_, e) =>
                TwitchClientLog(LogLevel.Error, $"{e.Exception.Message}\n{e.Exception.StackTrace}");

            _channelTwitchClient.FullConnect();
        }

        private void LoadDonationAlertsClient(NortagesTwitchBotDbContext dbContext)
        {
            var donationAlertsClientService = dbContext.Services.Single(s => s.Name == nameof(DonationAlerts.DonationAlertsClient));
            var donAlertsChannelService = ChannelInfo.ChannelServices.SingleOrDefault(cs => cs.Service.Name == nameof(DonationAlertsClient));
            if (donAlertsChannelService is null)
                return;
            DonationAlertsClient = new DonationAlertsClient(donAlertsChannelService.Credentials.AccessToken, ChannelUsername, Logger);
            BotService.AddEventHandlersToService(DonationAlertsClient, donationAlertsClientService.ServiceEvents.ToArray());
            DonationAlertsClient.ListenToDonationAlerts();
            DonationAlertsClient.Connect();
        }

        public void Stop()
        {
            if (_channelTwitchClient is {IsConnected: true})
                _channelTwitchClient.Disconnect();

            DonationAlertsClient?.Close();
        }

        public void OnStreamOnline(object sender, OnStreamOnlineArgs e)
        {
            Logger.Log(LogLevel.Information, LogFormat, nameof(LiveStreamMonitorService),
                $"{ChannelInfo.ChannelUsername} is online!");
        }

        public void OnStreamOffline(object sender, OnStreamOfflineArgs e)
        {
            Logger.Log(LogLevel.Information, LogFormat, nameof(LiveStreamMonitorService),
                $"{ChannelInfo.ChannelUsername} is offline!");
        }
    }
}