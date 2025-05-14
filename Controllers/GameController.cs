using junimo_v3.Models;
using junimo_v3.Services;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using System.Security.Claims;

namespace junimo_v3.Controllers
{
    public class GameController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IGameGenreV2Service _gameGenreV2Service;
        private readonly IUserService _userService;

        public GameController(IGameService gameService, IGameGenreV2Service gameGenreV2Service, IUserService userService)
        {
            _gameService = gameService;
            _gameGenreV2Service = gameGenreV2Service;
            _userService = userService;
        }

        [HttpGet("/game/create")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("/game/create")]
        public async Task<IActionResult> Create(Game game)
        {
            await _gameService.CreateGame(game);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/games")]
        public async Task<IActionResult> Index()
        {
            var games = await _gameService.GetAllGames();
            return View(games);
        }

        public async Task<IActionResult> Search(string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return RedirectToAction("Index");
            }

            var games = await _gameService.SearchGamesAsync(searchTerm);
            ViewData["SearchTerm"] = searchTerm;
            return View("Index", games);
        }

        [HttpGet("/game/details/{id:int}")]
        public async Task<IActionResult> GameDetails(int id)
        {
            var game = await _gameService.GetGameById(id);
            if (game == null)
            {
                return NotFound();
            }

            // Get the genres for this game
            var genres = await _gameGenreV2Service.GetGenresByGameIdAsync(id);

            // Pass the genres to the view using ViewBag
            ViewBag.Genres = genres;

            return View(game);
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> UserGames()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            // Get the user with their games included
            var user = await _userService.GetUserWithGamesAsync(userId);

            if (user == null || user.Games == null)
                return View(new List<Game>());

            return View(user.Games);
        }

    }
}
