using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TwitchBot.Main;
using TwitchBot.Models;

namespace TwitchBot.Controllers
{
    public class CommandsController : Controller
    {
        private readonly NortagesTwitchBotContext _dbContext;
        
        public CommandsController(NortagesTwitchBotContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IActionResult Index(string channelName)
        {
            var channelBotInfo = _dbContext.ChannelBots.SingleOrDefault(n => EF.Functions.ILike(n.ChannelUsername, channelName));
            if (channelBotInfo == null) return new NotFoundResult();
            
            ViewData["ChannelName"] = channelName;

            return View(channelBotInfo.ChannelCommands.Where(c => c.IsEnabled).ToList());
        }
    }
}
