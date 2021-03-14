using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using HerokuApp.Main;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HerokuApp
{
    public static class Program
    {
        public static event EventHandler OnProcessExit;

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.ProcessExit += (o, e) =>
            {
                Console.WriteLine("The app have got SIGTERM. Perform the graceful shutdown...");
                OnProcessExit?.Invoke(o, e);
            };

            Task.Run(NortagesTwitchBot.Connect);
            CreateHostBuilder(args).Build().Run();
        }

        private static IHostBuilder CreateHostBuilder(string[] args)
        {
            var port = Environment.GetEnvironmentVariable("PORT");
            return Host.CreateDefaultBuilder(args)
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
