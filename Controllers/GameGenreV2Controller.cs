// Controllers/GameGenreV2Controller.cs
using junimo_v3.Models;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    public class GameGenreV2Controller : Controller
    {
        private readonly IGameGenreV2Service _gameGenreService;
        private readonly IGameService _gameService;

        public GameGenreV2Controller(IGameGenreV2Service gameGenreService, IGameService gameService)
        {
            _gameGenreService = gameGenreService;
            _gameService = gameService;
        }

        [HttpGet]
        public async Task<IActionResult> Create(int gameId)
        {
            // Verify the game exists
            var game = await _gameService.GetGameByIdAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            ViewBag.GameId = gameId;
            ViewBag.GameTitle = game.Title;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(int gameId, string genre)
        {
            if (string.IsNullOrWhiteSpace(genre))
            {
                ModelState.AddModelError("genre", "Genre cannot be empty");

                // Get game title for the view
                var game = await _gameService.GetGameByIdAsync(gameId);
                ViewBag.GameId = gameId;
                ViewBag.GameTitle = game?.Title;

                return View();
            }

            // Get the game
            var gameEntity = await _gameService.GetGameByIdAsync(gameId);
            if (gameEntity == null)
            {
                return NotFound();
            }

            // Create the new genre
            var gameGenre = new GameGenreV2
            {
                GameId = gameId,
                genre = genre,
                Game = gameEntity  // Set the navigation property
            };

            // Add it using the service
            await _gameGenreService.AddGenreToGameAsync(gameGenre);

            return RedirectToAction("GameDetails", "Game", new { id = gameId });
        }
    }
}