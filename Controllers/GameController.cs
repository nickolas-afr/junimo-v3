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
        private readonly IReviewService _reviewService;

        public GameController(
            IGameService gameService, 
            IGameGenreV2Service gameGenreV2Service, 
            IUserService userService,
            IReviewService reviewService)
        {
            _gameService = gameService;
            _gameGenreV2Service = gameGenreV2Service;
            _userService = userService;
            _reviewService = reviewService;  // Initialize the review service
        }

        [HttpGet("/game/create")]
        [Authorize(Roles = "Admin")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("/game/create")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Game game, IFormFile gamePicture)
        {
            if (!ModelState.IsValid)
            {
                return View(game);
            }

            // Handle game picture upload if provided
            if (gamePicture != null && gamePicture.Length > 0)
            {
                // Define max file size (5MB)
                long maxFileSize = 5 * 1024 * 1024;

                // Validate file size
                if (gamePicture.Length > maxFileSize)
                {
                    ModelState.AddModelError("gamePicture", "Image size cannot exceed 5MB");
                    return View(game);
                }

                // Validate file type
                string[] allowedTypes = { "image/jpeg", "image/png", "image/gif", "image/bmp" };
                if (!allowedTypes.Contains(gamePicture.ContentType.ToLower()))
                {
                    ModelState.AddModelError("gamePicture", "Only image files (JPEG, PNG, GIF, BMP) are allowed");
                    return View(game);
                }

                // Read the image file into a byte array
                using (var memoryStream = new MemoryStream())
                {
                    await gamePicture.CopyToAsync(memoryStream);
                    game.GamePicture = memoryStream.ToArray();
                    game.GamePictureContentType = gamePicture.ContentType;
                }
            }
            
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

            // Check if user is logged in
            var isLoggedIn = User.Identity.IsAuthenticated;
            ViewBag.IsLoggedIn = isLoggedIn;

            if (isLoggedIn)
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                
                // Check if user owns the game
                var user = await _userService.GetUserWithGamesAsync(userId);
                var userOwnsGame = user?.Games?.Any(g => g.GameId == id) ?? false;
                ViewBag.UserOwnsGame = userOwnsGame;
                
                // Check if user has already reviewed the game
                var userReview = await _reviewService.GetUserReviewForGameAsync(userId, id);
                ViewBag.UserReview = userReview;
                ViewBag.HasReviewed = userReview != null;
            }
            else
            {
                ViewBag.UserOwnsGame = false;
                ViewBag.HasReviewed = false;
            }

            // Get reviews for the game
            var reviews = await _reviewService.GetReviewsByGameIdAsync(id);
            ViewBag.Reviews = reviews;

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
