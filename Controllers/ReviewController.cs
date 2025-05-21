using junimo_v3.Models;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace junimo_v3.Controllers
{
    public class ReviewController : Controller
    {
        private readonly IReviewService _reviewService;
        private readonly IGameService _gameService;
        private readonly IUserService _userService;

        public ReviewController(IReviewService reviewService, IGameService gameService, IUserService userService)
        {
            _reviewService = reviewService;
            _gameService = gameService;
            _userService = userService;
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int gameId)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            // Check if user owns the game
            var user = await _userService.GetUserWithGamesAsync(userId);
            if (user == null || user.Games == null || !user.Games.Any(g => g.GameId == gameId))
            {
                TempData["ErrorMessage"] = "You must own this game to leave a review.";
                return RedirectToAction("GameDetails", "Game", new { id = gameId });
            }

            // Check if user has already reviewed this game
            if (await _reviewService.UserHasReviewedGameAsync(userId, gameId))
            {
                TempData["ErrorMessage"] = "You have already reviewed this game. You can edit your existing review.";
                return RedirectToAction("GameDetails", "Game", new { id = gameId });
            }

            var game = await _gameService.GetGameById(gameId);
            if (game == null)
                return NotFound();

            var review = new Review
            {
                GameId = gameId,
                UserId = userId,
                CreatedDate = DateOnly.FromDateTime(DateTime.Now)
            };

            ViewData["GameTitle"] = game.Title;
            return View(review);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Create(Review review)
        {
            if (!ModelState.IsValid)
            {
                var game = await _gameService.GetGameById(review.GameId);
                ViewData["GameTitle"] = game?.Title;
                return View(review);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || userId != review.UserId)
            {
                return Forbid();
            }

            // Ensure the user owns the game
            var user = await _userService.GetUserWithGamesAsync(userId);
            if (user == null || user.Games == null || !user.Games.Any(g => g.GameId == review.GameId))
            {
                TempData["ErrorMessage"] = "You must own this game to leave a review.";
                return RedirectToAction("GameDetails", "Game", new { id = review.GameId });
            }

            // Set created date to today
            review.CreatedDate = DateOnly.FromDateTime(DateTime.Now);
            
            await _reviewService.CreateReviewAsync(review);
            
            return RedirectToAction("GameDetails", "Game", new { id = review.GameId });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || userId != review.UserId)
            {
                return Forbid();
            }

            ViewData["GameTitle"] = review.Game?.Title;
            return View(review);
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Edit(Review review)
        {
            if (!ModelState.IsValid)
            {
                var game = await _gameService.GetGameById(review.GameId);
                ViewData["GameTitle"] = game?.Title;
                return View(review);
            }

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || userId != review.UserId)
            {
                return Forbid();
            }

            var existingReview = await _reviewService.GetReviewByIdAsync(review.ReviewId);
            if (existingReview == null)
                return NotFound();

            // Update only allowed properties
            existingReview.Rating = review.Rating;
            existingReview.Comment = review.Comment;
            existingReview.IsRecommended = review.IsRecommended;
            
            await _reviewService.UpdateReviewAsync(existingReview);
            
            return RedirectToAction("GameDetails", "Game", new { id = review.GameId });
        }

        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            var review = await _reviewService.GetReviewByIdAsync(id);
            if (review == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId) || userId != review.UserId)
            {
                return Forbid();
            }

            int gameId = review.GameId;
            await _reviewService.DeleteReviewAsync(id);
            
            return RedirectToAction("GameDetails", "Game", new { id = gameId });
        }

        [Authorize]
        public async Task<IActionResult> UserReviews()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var reviews = await _reviewService.GetUserReviewsAsync(userId);
            return View(reviews);
        }
    }
}