using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace junimo_v3.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly IRecommendationService _recommendationService;
        private readonly IGameService _gameService;
        private readonly ILogger<RecommendationController> _logger;

        public RecommendationController(
            IRecommendationService recommendationService,
            IGameService gameService,
            ILogger<RecommendationController> logger)
        {
            _recommendationService = recommendationService;
            _gameService = gameService;
            _logger = logger;
        }

        /// <summary>
        /// Gets personalized game recommendations for the logged-in user.
        /// </summary>
        [HttpGet("/recommendations")]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Home");
            }

            var recommendations = await _recommendationService.GetRecommendationsForUserAsync(userId, 6);
            ViewBag.IsModelReady = _recommendationService.IsModelReady;

            return View(recommendations);
        }

        /// <summary>
        /// Gets recommendations as a partial view (for AJAX loading).
        /// </summary>
        [HttpGet("/recommendations/partial")]
        [Authorize]
        public async Task<IActionResult> GetRecommendationsPartial(int count = 4)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return PartialView("_RecommendationsPartial", Enumerable.Empty<Models.Game>());
            }

            var recommendations = await _recommendationService.GetRecommendationsForUserAsync(userId, count);
            return PartialView("_RecommendationsPartial", recommendations);
        }

        /// <summary>
        /// API endpoint to get recommendation scores for specific games.
        /// </summary>
        [HttpGet("/api/recommendations/scores")]
        [Authorize]
        public IActionResult GetScores([FromQuery] int[] gameIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var scores = gameIds.Select(gameId => new
            {
                GameId = gameId,
                Score = _recommendationService.PredictRating(userId, gameId)
            }).ToList();

            return Json(scores);
        }

        /// <summary>
        /// Admin endpoint to trigger model retraining.
        /// </summary>
        [HttpPost("/api/recommendations/retrain")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> RetrainModel()
        {
            _logger.LogInformation("Admin triggered model retraining");
            await _recommendationService.RetrainModelAsync();
            return Json(new { success = true, message = "Model retraining initiated" });
        }

        /// <summary>
        /// Gets model status.
        /// </summary>
        [HttpGet("/api/recommendations/status")]
        public IActionResult GetStatus()
        {
            return Json(new
            {
                isModelReady = _recommendationService.IsModelReady
            });
        }
    }
}
