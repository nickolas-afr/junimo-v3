// Controllers/GameGenreV2Controller.cs
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    public class GameGenreV2Controller : Controller
    {
        private readonly IRepositoryWrapper _repository;

        public GameGenreV2Controller(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        [HttpGet]
        public IActionResult Create(int gameId)
        {
            // Verify the game exists
            var game = _repository.Game.GetByIdAsync(gameId).Result;
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
                ViewBag.GameId = gameId;
                return View();
            }

            // Get the game
            var game = await _repository.Game.GetByIdAsync(gameId);
            if (game == null)
            {
                return NotFound();
            }

            // Create the new genre
            var gameGenre = new GameGenreV2
            {
                GameId = gameId,
                genre = genre,
                Game = game  // Set the navigation property
            };

            // Add it to the repository
            _repository.GameGenreV2.Create(gameGenre);
            await _repository.SaveAsync();

            return RedirectToAction("GameDetails", "Game", new { id = gameId });
        }
    }
}
