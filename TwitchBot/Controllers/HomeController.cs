using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TwitchBot.Main;

namespace TwitchBot.Controllers
{
    public class HomeController : Controller
    {
        private readonly NortagesTwitchBotDbContext _dbDbContext;

        public HomeController(NortagesTwitchBotDbContext dbDbContext)
        {
            _dbDbContext = dbDbContext;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["BotUsername"] = BotService.BotUsername;
            return View(await _dbDbContext.ChannelInfos.ToListAsync());
        }
    }
}