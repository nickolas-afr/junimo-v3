using junimo_v3.Models.DocumentSearch;

namespace junimo_v3.Services.Interfaces
{
    public interface IDocumentSearchService
    {
        Task RebuildIndexAsync();
        Task UpsertGameAsync(int gameId);
        Task DeleteGameAsync(int gameId);
        Task<DocumentSearchResultPage> SearchAsync(string query, string sort, int page, int pageSize);
    }
}
