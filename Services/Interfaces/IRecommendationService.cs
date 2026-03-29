using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface IRecommendationService
    {
        Task<IEnumerable<GameRecommendation>> GetRecommendationsForUserAsync(string userId, int topN = 10);
        Task<Dictionary<string, float>> GetUserGenrePreferencesAsync(string userId);
    }
}
