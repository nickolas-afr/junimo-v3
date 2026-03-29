namespace junimo_v3.Services.Interfaces
{
    public interface ISearchService
    {
        Task<IEnumerable<string>> GetTitleSuggestionsAsync(string query, int limit = 8);
    }
}
