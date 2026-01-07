using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface IRecommendationService
    {
        /// <summary>
        /// Gets recommended games for a user based on their order history.
        /// </summary>
        /// <param name="userId">The user ID to get recommendations for.</param>
        /// <param name="maxRecommendations">Maximum number of recommendations to return.</param>
        /// <returns>A list of recommended games.</returns>
        Task<IEnumerable<Game>> GetRecommendationsAsync(string userId, int maxRecommendations = 10);
    }
}
