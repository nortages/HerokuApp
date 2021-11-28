using System;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Npgsql;
using TwitchBot.Main;
using TwitchBot.Main.Enums;
using TwitchBot.Models;
using TwitchBot.Models.AssociativeEntities;

#nullable disable

namespace TwitchBot
{
    public class NortagesTwitchBotDbContext : DbContext
    {
        private static readonly Regex UnformattedConnectionStringRegex = new("postgres://(.+):(.+)@(.+)/(.+)");

        static NortagesTwitchBotDbContext()
        {
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Lang>("Language");
            NpgsqlConnection.GlobalTypeMapper.MapEnum<Scope>("Scope");
        }

        public NortagesTwitchBotDbContext()
        {
        }

        public NortagesTwitchBotDbContext(DbContextOptions<NortagesTwitchBotDbContext> options,
            IConfiguration configuration)
            : base(options)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public virtual DbSet<ChannelInfo> ChannelInfos { get; set; }
        public virtual DbSet<Credentials> Credentials { get; set; }
        public virtual DbSet<Command> Commands { get; set; }
        public virtual DbSet<ServiceInfo> Services { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<UserChannelCommand> UserChannelCommands { get; set; }
        public virtual DbSet<ChannelCommand> ChannelCommands { get; set; }
        public virtual DbSet<MultiLangAnswer> MultiLangAnswers { get; set; }

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
            if (optionsBuilder.IsConfigured)
                return;
            optionsBuilder.UseLazyLoadingProxies();
            var connectionString = BotService.GetSecret("DATABASE_URL");
            connectionString = FormatConnectionString(connectionString);
            optionsBuilder.UseNpgsql(connectionString);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ChannelMessageCommand>().HasKey(u => new
            {
                u.MessageCommandId,
                u.ChannelId
            });
        }
    }
}