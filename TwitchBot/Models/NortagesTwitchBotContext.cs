using System;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;

#nullable disable

namespace TwitchBot.Models
{
    public class NortagesTwitchBotContext : DbContext
    {
        static NortagesTwitchBotContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Lang>("Language");
        }
        
        public NortagesTwitchBotContext()
        {
        }

        public NortagesTwitchBotContext(DbContextOptions<NortagesTwitchBotContext> options, IConfiguration configuration)
            : base(options)
        {
            Configuration = configuration;
        }
        
        public IConfiguration Configuration { get; }

        public virtual DbSet<ChannelBotInfo> ChannelBots { get; set; }
        public virtual DbSet<Credentials> Credentials { get; set; }
        public virtual DbSet<Command> Commands { get; set; }
        // public virtual DbSet<CallbackInfo> Callbacks { get; set; }
        // public virtual DbSet<ChannelCommand> ChannelCommands { get; set; }
        // public virtual DbSet<ChannelMessageCommand> ChannelMessageCommands { get; set; }
        // public virtual DbSet<DonationAlertInfo> DonationAlerts { get; set; }
        // public virtual DbSet<MessageCommand> MessageCommands { get; set; }
        // public virtual DbSet<MultilangAnswer> MultilangStrings { get; set; }
        // public virtual DbSet<Option> Options { get; set; }
        // public virtual DbSet<PubsubInfo> Pubsubs { get; set; }
        // public virtual DbSet<ServiceCallback> ServiceCallbacks { get; set; }
        // public virtual DbSet<ServiceCallbacksList> ServiceCallbacksLists { get; set; }
        // public virtual DbSet<TwitchClientInfo> TwitchClients { get; set; }
        
        private static readonly Regex UnformattedConnectionStringRegex = new("postgres://(.+):(.+)@(.+)/(.+)");
        
        private static string FormatConnectionString(string connectionString)
        {
            var groups = UnformattedConnectionStringRegex.Match(connectionString).Groups;
            var user = groups[1];
            var password = groups[2];
            var host = groups[3].Value.Split(':')[0];
            var port = groups[3].Value.Split(':')[1];
            var database = groups[4];

            return $"Server={host};Port={port};Database={database};User Id={user};Password={password};Sslmode=Require;Trust Server Certificate=true;";
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (optionsBuilder.IsConfigured) return;
            optionsBuilder.UseLazyLoadingProxies();
            var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL");
            connectionString = connectionString == null ? Configuration["DATABASE_URL"] : FormatConnectionString(connectionString);
            optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChannelCommand>().HasKey(u => new 
            { 
                u.CommandId, 
                u.ChannelId,
            });
            modelBuilder.Entity<ChannelMessageCommand>().HasKey(u => new 
            { 
                u.MessageCommandId, 
                u.ChannelId,
            });
        }
    }
}
