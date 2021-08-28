using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TwitchBot.Main;
using Controller = Microsoft.AspNetCore.Mvc.Controller;

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
