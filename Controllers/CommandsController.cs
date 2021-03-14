using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HerokuApp.Controllers
{
    public class CommandsController : Controller
    {
        public IActionResult Index(string channelName)
        {
            var channelBotInfo = Config.MainConfig.BotsInfo.Single(n => string.Equals(n.ChannelName, channelName, StringComparison.OrdinalIgnoreCase));
            
            ViewData["ChannelName"] = channelName;
            ViewData["CommandsInfo"] = channelBotInfo.Commands;

            return View();
        }
    }
}
