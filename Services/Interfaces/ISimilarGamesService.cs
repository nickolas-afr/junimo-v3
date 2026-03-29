using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface ISimilarGamesService
    {
        Task<IEnumerable<GameRecommendation>> GetSimilarGamesAsync(int gameId, int topN = 4);
    }
}
