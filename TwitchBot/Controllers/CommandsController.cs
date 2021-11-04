using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace TwitchBot.Controllers
{
    public class CommandsController : Controller
    {
        private readonly NortagesTwitchBotDbContext _dbDbContext;

        public CommandsController(NortagesTwitchBotDbContext dbDbContext)
        {
            _dbDbContext = dbDbContext;
        }

        public IActionResult Index(string channelName)
        {
            var channelBotInfo =
                _dbDbContext.ChannelInfos.SingleOrDefault(n => EF.Functions.ILike(n.ChannelUsername, channelName));
            if (channelBotInfo == null) return new NotFoundResult();

            ViewData["ChannelName"] = channelName;

            return View(channelBotInfo.ChannelCommands.Where(c => c.IsEnabled).ToList());
        }
    }
}