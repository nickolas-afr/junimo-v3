using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminDashboardController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IRecommendationService _recommendationService;

        public AdminDashboardController(IGameService gameService, IRecommendationService recommendationService)
        {
            _gameService = gameService;
            _recommendationService = recommendationService;
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

        [HttpPost]
        public async Task<IActionResult> TrainRecommendationModel(bool useDemoData = true)
        {
            var success = await _recommendationService.TrainAndSaveModelAsync(useDemoData);
            
            if (success)
            {
                TempData["SuccessMessage"] = "Recommendation model trained successfully!";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to train the recommendation model. Please check if there is enough data.";
            }
            
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult RecommendationModelStatus()
        {
            var isTrained = _recommendationService.IsModelTrained();
            return Json(new { isTrained });
        }
    }
}