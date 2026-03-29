using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace junimo_v3.Controllers
{
    public class RecommendationController : Controller
    {
        private readonly IRecommendationService _recommendationService;

        public RecommendationController(IRecommendationService recommendationService)
        {
            _recommendationService = recommendationService;
        }

        [HttpGet("/recommendations")]
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Account");
            }

            var recommendations = await _recommendationService.GetRecommendationsForUserAsync(userId, 12);
            
            return View(recommendations);
        }

        [HttpGet("/api/recommendations")]
        [Authorize]
        public async Task<IActionResult> GetRecommendations([FromQuery] int count = 10)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var recommendations = await _recommendationService.GetRecommendationsForUserAsync(userId, count);
            
            return Ok(recommendations);
        }

        [HttpGet("/api/recommendations/genre-preferences")]
        [Authorize]
        public async Task<IActionResult> GetGenrePreferences()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var genrePreferences = await _recommendationService.GetUserGenrePreferencesAsync(userId);
            
            return Ok(genrePreferences);
        }
    }
}
