using junimo_v3.Models;
using junimo_v3.Models.ML;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.ML;

namespace junimo_v3.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IRepositoryWrapper _repositoryWrapper;
        private readonly MLContext _mlContext;

        public RecommendationService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
            _mlContext = new MLContext(seed: 0);
        }

        public async Task<IEnumerable<Game>> GetRecommendationsAsync(string userId, int maxRecommendations = 10)
        {
            // Get user's completed orders with games and genres
            var userOrders = await _repositoryWrapper.Order
                .FindByCondition(o => o.UserId == userId && o.Status == OrderStatus.Completed)
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Game!)
                        .ThenInclude(g => g.GameGenresV2)
                .ToListAsync();

            // Get all games the user already owns
            var ownedGameIds = userOrders
                .SelectMany(o => o.OrderItems ?? Enumerable.Empty<OrderItems>())
                .Select(oi => oi.GameId)
                .Distinct()
                .ToHashSet();

            // If user has no order history, return featured games they don't own
            if (!userOrders.Any() || !ownedGameIds.Any())
            {
                return await GetFallbackRecommendationsAsync(ownedGameIds, maxRecommendations);
            }

            // Extract genres from user's purchased games and count occurrences
            var userGenrePreferences = userOrders
                .SelectMany(o => o.OrderItems ?? Enumerable.Empty<OrderItems>())
                .Where(oi => oi.Game?.GameGenresV2 != null)
                .SelectMany(oi => oi.Game!.GameGenresV2!)
                .GroupBy(gg => gg.genre)
                .ToDictionary(g => g.Key, g => (float)g.Count());

            if (!userGenrePreferences.Any())
            {
                return await GetFallbackRecommendationsAsync(ownedGameIds, maxRecommendations);
            }

            // Build training data from all users' order history
            var trainingData = await BuildTrainingDataAsync();

            if (trainingData.Count < 2)
            {
                // Not enough data to train, use genre-based fallback
                return await GetGenreBasedRecommendationsAsync(userId, userGenrePreferences, ownedGameIds, maxRecommendations);
            }

            try
            {
                // Train the model and get predictions
                var predictedGenreScores = TrainAndPredict(trainingData, userId, userGenrePreferences.Keys);

                // Get recommended games based on predicted genre scores
                return await GetGamesFromPredictedGenresAsync(predictedGenreScores, ownedGameIds, maxRecommendations);
            }
            catch
            {
                // If ML training fails, fall back to genre-based recommendations
                return await GetGenreBasedRecommendationsAsync(userId, userGenrePreferences, ownedGameIds, maxRecommendations);
            }
        }

        private async Task<List<GameRating>> BuildTrainingDataAsync()
        {
            var allOrders = await _repositoryWrapper.Order
                .FindByCondition(o => o.Status == OrderStatus.Completed)
                .Include(o => o.OrderItems!)
                    .ThenInclude(oi => oi.Game!)
                        .ThenInclude(g => g.GameGenresV2)
                .ToListAsync();

            var trainingData = new List<GameRating>();

            // Group orders by user
            var userOrders = allOrders.GroupBy(o => o.UserId);

            foreach (var userGroup in userOrders)
            {
                var genreCounts = userGroup
                    .SelectMany(o => o.OrderItems ?? Enumerable.Empty<OrderItems>())
                    .Where(oi => oi.Game?.GameGenresV2 != null)
                    .SelectMany(oi => oi.Game!.GameGenresV2!)
                    .GroupBy(gg => gg.genre)
                    .ToDictionary(g => g.Key, g => (float)g.Count());

                foreach (var genreCount in genreCounts)
                {
                    trainingData.Add(new GameRating
                    {
                        UserId = userGroup.Key,
                        Genre = genreCount.Key,
                        Rating = genreCount.Value
                    });
                }
            }

            return trainingData;
        }

        private Dictionary<string, float> TrainAndPredict(List<GameRating> trainingData, string userId, IEnumerable<string> userGenres)
        {
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

            // Build the recommendation pipeline using Matrix Factorization
            var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("userIdEncoded", nameof(GameRating.UserId))
                .Append(_mlContext.Transforms.Conversion.MapValueToKey("genreEncoded", nameof(GameRating.Genre)))
                .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                    labelColumnName: nameof(GameRating.Rating),
                    matrixColumnIndexColumnName: "userIdEncoded",
                    matrixRowIndexColumnName: "genreEncoded",
                    numberOfIterations: 20,
                    approximationRank: 100));

            var model = pipeline.Fit(dataView);
            var predictionEngine = _mlContext.Model.CreatePredictionEngine<GameRating, GameRatingPrediction>(model);

            // Get all unique genres from training data
            var allGenres = trainingData.Select(td => td.Genre).Distinct().ToList();

            // Predict scores for all genres for this user
            var predictions = new Dictionary<string, float>();
            foreach (var genre in allGenres)
            {
                var prediction = predictionEngine.Predict(new GameRating
                {
                    UserId = userId,
                    Genre = genre,
                    Rating = 0
                });
                predictions[genre] = prediction.Score;
            }

            return predictions;
        }

        private async Task<IEnumerable<Game>> GetGamesFromPredictedGenresAsync(
            Dictionary<string, float> predictedGenreScores,
            HashSet<int> ownedGameIds,
            int maxRecommendations)
        {
            // Get top genres by predicted score
            var topGenres = predictedGenreScores
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();

            // Get games that match these genres and user doesn't own
            var recommendedGames = await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .Where(g => g.GameGenresV2 != null && 
                           g.GameGenresV2.Any(gg => topGenres.Contains(gg.genre)) &&
                           !ownedGameIds.Contains(g.GameId))
                .ToListAsync();

            // Score games by how many top genres they match
            var scoredGames = recommendedGames
                .Select(g => new
                {
                    Game = g,
                    Score = g.GameGenresV2!.Sum(gg => 
                        predictedGenreScores.TryGetValue(gg.genre, out var score) ? score : 0)
                })
                .OrderByDescending(x => x.Score)
                .Take(maxRecommendations)
                .Select(x => x.Game);

            return scoredGames;
        }

        private async Task<IEnumerable<Game>> GetGenreBasedRecommendationsAsync(
            string userId,
            Dictionary<string, float> userGenrePreferences,
            HashSet<int> ownedGameIds,
            int maxRecommendations)
        {
            // Get top genres based on purchase count
            var topGenres = userGenrePreferences
                .OrderByDescending(kvp => kvp.Value)
                .Take(5)
                .Select(kvp => kvp.Key)
                .ToList();

            // Get games matching user's preferred genres that they don't own
            var recommendedGames = await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .Where(g => g.GameGenresV2 != null && 
                           g.GameGenresV2.Any(gg => topGenres.Contains(gg.genre)) &&
                           !ownedGameIds.Contains(g.GameId))
                .ToListAsync();

            // Score games by genre match count
            var scoredGames = recommendedGames
                .Select(g => new
                {
                    Game = g,
                    Score = g.GameGenresV2!.Count(gg => topGenres.Contains(gg.genre))
                })
                .OrderByDescending(x => x.Score)
                .Take(maxRecommendations)
                .Select(x => x.Game);

            return scoredGames;
        }

        private async Task<IEnumerable<Game>> GetFallbackRecommendationsAsync(HashSet<int> ownedGameIds, int maxRecommendations)
        {
            // Return featured games that user doesn't own
            var featuredGames = await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .Where(g => g.IsFeatureRecommended && !ownedGameIds.Contains(g.GameId))
                .Take(maxRecommendations)
                .ToListAsync();

            // If not enough featured games, add more games
            if (featuredGames.Count < maxRecommendations)
            {
                var additionalGames = await _repositoryWrapper.Game
                    .FindAll()
                    .Include(g => g.GameGenresV2)
                    .Where(g => !ownedGameIds.Contains(g.GameId) && !featuredGames.Select(fg => fg.GameId).Contains(g.GameId))
                    .Take(maxRecommendations - featuredGames.Count)
                    .ToListAsync();

                featuredGames.AddRange(additionalGames);
            }

            return featuredGames;
        }
    }
}
