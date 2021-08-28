using System.Linq;
using Microsoft.AspNetCore.Mvc;
using TwitchBot.Main;

namespace TwitchBot.Controllers
{
    public class CommandsController : Controller
    {
        public IActionResult Index(string channelName)
        {
            var channelBotInfo = MainTwitchBot.ChannelsBots.SingleOrDefault(n => string.Equals(n.ChannelName, channelName, Config.StringComparison));
            if (channelBotInfo == null) return new NotFoundResult();
            
            ViewData["ChannelName"] = channelName;
            ViewData["CommandsInfo"] = channelBotInfo.Commands;

            return View();
        }
    }
}
