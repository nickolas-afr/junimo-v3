using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    /// <summary>
    /// Service interface for game recommendations using ML.NET.
    /// </summary>
    public interface IRecommendationService
    {
        /// <summary>
        /// Gets personalized game recommendations for a specific user.
        /// </summary>
        /// <param name="userId">The user ID to get recommendations for.</param>
        /// <param name="count">Number of recommendations to return.</param>
        /// <returns>List of recommended games.</returns>
        Task<IEnumerable<Game>> GetRecommendationsForUserAsync(string userId, int count = 5);

        /// <summary>
        /// Adds a new rating to the training data and optionally retrains the model.
        /// Supports incremental learning.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="gameId">Game ID.</param>
        /// <param name="rating">Rating value (1-10).</param>
        /// <param name="retrain">Whether to retrain the model immediately.</param>
        Task AddRatingAsync(string userId, int gameId, float rating, bool retrain = false);

        /// <summary>
        /// Retrains the recommendation model with all available data.
        /// </summary>
        Task RetrainModelAsync();

        /// <summary>
        /// Gets the predicted rating for a user-game pair.
        /// </summary>
        /// <param name="userId">User ID.</param>
        /// <param name="gameId">Game ID.</param>
        /// <returns>Predicted rating score.</returns>
        float PredictRating(string userId, int gameId);

        /// <summary>
        /// Checks if the model is trained and ready for predictions.
        /// </summary>
        bool IsModelReady { get; }
    }
}
