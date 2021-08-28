using Microsoft.AspNetCore.Mvc;
using TwitchBot.Main;

namespace TwitchBot.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var botsInfo = MainTwitchBot.ChannelsBots;
            ViewData["BotsInfo"] = botsInfo;
            ViewData["BotUsername"] = Config.BotUsername;

            return View();
        }
    }
}