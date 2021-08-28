using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TwitchBot.Main;

namespace TwitchBot
{
    public static class Program
    {
        public static Random Rand { get; } = new Random();
        public static event EventHandler OnProcessExit;
        private static MainTwitchBot _mainTwitchBot;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (o, e) =>
            {
                Console.WriteLine("The app have got SIGTERM. Perform the graceful shutdown...");
                OnProcessExit?.Invoke(o, e);
            };
           
            var host = CreateHostBuilder(args).Build();
            var configuration = host.Services.GetService<IConfiguration>(); 
            var loggerProvider = host.Services.GetService<ILoggerProvider>(); 

            _mainTwitchBot = new MainTwitchBot(configuration, loggerProvider);
            _mainTwitchBot.Connect();

            host.Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var port = Environment.GetEnvironmentVariable("PORT");
            return Host.CreateDefaultBuilder(args)
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    if (port != null)
                    {
                        webBuilder.UseUrls("http://*:" + port);
                    }                    
                });
        }
    }
}
