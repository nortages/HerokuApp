using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TwitchBot.Main;
using TwitchBot.Models;

namespace TwitchBot.Controllers
{
    public class HomeController : Controller
    {
        private NortagesTwitchBotContext _dbContext;
        
        public HomeController(NortagesTwitchBotContext dbContext)
        {
            _dbContext = dbContext;
        }
        
        public async Task<IActionResult> Index()
        {
            ViewData["BotUsername"] = MainBotService.BotUsername;
            return View(await _dbContext.ChannelBots.ToListAsync());
        }
    }
}