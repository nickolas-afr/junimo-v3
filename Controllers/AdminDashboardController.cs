using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly IGameService _gameService;

        public AdminDashboardController(IGameService gameService)
        {
            _gameService = gameService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ManageGames()
        {
            var games = await _gameService.GetAllGames();
            return View(games);
        }
    }
}