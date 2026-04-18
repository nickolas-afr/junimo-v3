using junimo_v3.Models.DocumentSearch;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    public class DocumentSearchController : Controller
    {
        private readonly IDocumentSearchService _documentSearchService;

        public DocumentSearchController(IDocumentSearchService documentSearchService)
        {
            _documentSearchService = documentSearchService;
        }

        [HttpGet("/documents/search")]
        public async Task<IActionResult> Index(
            [FromQuery(Name = "q")] string q = "",
            [FromQuery] string sort = DocumentSearchSort.ScoreDesc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var model = await _documentSearchService.SearchAsync(q, sort, page, pageSize);
            return View(model);
        }
    }
}
