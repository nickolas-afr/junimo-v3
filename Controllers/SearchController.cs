using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    [Route("api/search")]
    public class SearchController : Controller
    {
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet("suggestions")]
        public async Task<IActionResult> Suggestions([FromQuery] string q, [FromQuery] int limit = 8)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Json(Array.Empty<string>());

            var suggestions = await _searchService.GetTitleSuggestionsAsync(q, limit);
            return Json(suggestions);
        }
    }
}
