using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using HerokuApp;
using System.Threading.Tasks;

namespace HerokuApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var bot = new TwitchChatBot();
            new Task(bot.Connect).Start();
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });        
    }
}
