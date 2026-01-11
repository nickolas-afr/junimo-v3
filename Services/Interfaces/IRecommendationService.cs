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

        /// <summary>
        /// Trains the recommendation model using existing order data or demo data.
        /// The trained model is saved to disk for future use.
        /// </summary>
        /// <param name="useDemoData">If true, uses demo training data for pre-training.</param>
        /// <returns>True if training was successful, false otherwise.</returns>
        Task<bool> TrainAndSaveModelAsync(bool useDemoData = false);

        /// <summary>
        /// Checks if a pre-trained model exists.
        /// </summary>
        /// <returns>True if a model file exists, false otherwise.</returns>
        bool IsModelTrained();
    }
}
