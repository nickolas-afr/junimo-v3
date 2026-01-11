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
        private readonly string _modelPath;
        private ITransformer? _trainedModel;
        private static readonly object _modelLock = new object();
        
        // ML training configuration constants
        private const int MatrixFactorizationIterations = 20;
        private const int MatrixFactorizationApproximationRank = 100;

        public RecommendationService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
            _mlContext = new MLContext(seed: 0);
            _modelPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ML", "recommendation_model.zip");
            
            // Ensure the ML directory exists
            var mlDirectory = Path.GetDirectoryName(_modelPath);
            if (!string.IsNullOrEmpty(mlDirectory) && !Directory.Exists(mlDirectory))
            {
                Directory.CreateDirectory(mlDirectory);
            }

            // Try to load an existing model
            LoadModelIfExists();
        }

        private void LoadModelIfExists()
        {
            if (File.Exists(_modelPath))
            {
                try
                {
                    lock (_modelLock)
                    {
                        _trainedModel = _mlContext.Model.Load(_modelPath, out _);
                    }
                }
                catch (FormatException)
                {
                    // Model file is corrupted or in wrong format
                    _trainedModel = null;
                }
                catch (IOException)
                {
                    // File access issue
                    _trainedModel = null;
                }
                catch (InvalidOperationException)
                {
                    // Model loading operation failed
                    _trainedModel = null;
                }
            }
        }

        public bool IsModelTrained()
        {
            // Check if model is loaded in memory first
            if (_trainedModel != null)
            {
                return true;
            }
            
            // Only check file if model is not in memory, and try to load it
            if (File.Exists(_modelPath))
            {
                LoadModelIfExists();
                return _trainedModel != null;
            }
            
            return false;
        }

        public async Task<bool> TrainAndSaveModelAsync(bool useDemoData = false)
        {
            try
            {
                List<GameRating> trainingData;
                
                if (useDemoData)
                {
                    trainingData = await BuildDemoTrainingDataAsync();
                }
                else
                {
                    trainingData = await BuildTrainingDataAsync();
                }

                if (trainingData.Count < 2)
                {
                    // Not enough data, use demo data as fallback
                    trainingData = await BuildDemoTrainingDataAsync();
                }

                if (trainingData.Count < 2)
                {
                    return false;
                }

                var dataView = _mlContext.Data.LoadFromEnumerable(trainingData);

                var pipeline = _mlContext.Transforms.Conversion.MapValueToKey("userIdEncoded", nameof(GameRating.UserId))
                    .Append(_mlContext.Transforms.Conversion.MapValueToKey("genreEncoded", nameof(GameRating.Genre)))
                    .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                        labelColumnName: nameof(GameRating.Rating),
                        matrixColumnIndexColumnName: "userIdEncoded",
                        matrixRowIndexColumnName: "genreEncoded",
                        numberOfIterations: MatrixFactorizationIterations,
                        approximationRank: MatrixFactorizationApproximationRank));

                lock (_modelLock)
                {
                    _trainedModel = pipeline.Fit(dataView);
                    _mlContext.Model.Save(_trainedModel, dataView.Schema, _modelPath);
                }

                return true;
            }
            catch (InvalidOperationException)
            {
                // ML.NET training operation failed
                return false;
            }
            catch (ArgumentException)
            {
                // Invalid training data or parameters
                return false;
            }
            catch (IOException)
            {
                // Failed to save model to disk
                return false;
            }
        }

        private async Task<List<GameRating>> BuildDemoTrainingDataAsync()
        {
            // Get all available genres from the database
            var allGames = await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .ToListAsync();

            var allGenres = allGames
                .Where(g => g.GameGenresV2 != null)
                .SelectMany(g => g.GameGenresV2!)
                .Select(gg => gg.genre)
                .Distinct()
                .ToList();

            if (!allGenres.Any())
            {
                // Fallback demo genres if no games exist
                allGenres = new List<string>
                {
                    "Action", "Adventure", "RPG", "Strategy", "Simulation",
                    "Sports", "Puzzle", "Horror", "Racing", "Fighting"
                };
            }

            // Create demo user preference patterns
            var demoData = new List<GameRating>();
            var demoUsers = new[]
            {
                ("demo_action_fan", new[] { ("Action", 5f), ("Adventure", 3f), ("RPG", 2f), ("Fighting", 4f) }),
                ("demo_rpg_fan", new[] { ("RPG", 5f), ("Adventure", 4f), ("Strategy", 3f), ("Action", 2f) }),
                ("demo_strategy_fan", new[] { ("Strategy", 5f), ("Simulation", 4f), ("Puzzle", 3f), ("RPG", 2f) }),
                ("demo_casual_fan", new[] { ("Puzzle", 5f), ("Simulation", 4f), ("Sports", 3f), ("Racing", 2f) }),
                ("demo_horror_fan", new[] { ("Horror", 5f), ("Adventure", 4f), ("Action", 3f), ("Puzzle", 2f) }),
                ("demo_sports_fan", new[] { ("Sports", 5f), ("Racing", 4f), ("Fighting", 3f), ("Simulation", 2f) }),
                ("demo_adventure_fan", new[] { ("Adventure", 5f), ("RPG", 4f), ("Action", 3f), ("Puzzle", 2f) }),
                ("demo_simulation_fan", new[] { ("Simulation", 5f), ("Strategy", 4f), ("Puzzle", 3f), ("Sports", 2f) })
            };

            foreach (var (userId, preferences) in demoUsers)
            {
                foreach (var (genre, rating) in preferences)
                {
                    // Only add if the genre exists in our actual game data
                    if (allGenres.Contains(genre))
                    {
                        demoData.Add(new GameRating
                        {
                            UserId = userId,
                            Genre = genre,
                            Rating = rating
                        });
                    }
                }
            }

            // Also add some ratings for all genres to ensure coverage
            foreach (var genre in allGenres)
            {
                if (!demoData.Any(d => d.Genre == genre))
                {
                    demoData.Add(new GameRating
                    {
                        UserId = "demo_general_user",
                        Genre = genre,
                        Rating = 1f
                    });
                }
            }

            return demoData;
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

            // Extract genres from user's purchased games and count occurrences
            var userGenrePreferences = userOrders
                .SelectMany(o => o.OrderItems ?? Enumerable.Empty<OrderItems>())
                .Where(oi => oi.Game?.GameGenresV2 != null)
                .SelectMany(oi => oi.Game!.GameGenresV2!)
                .GroupBy(gg => gg.genre)
                .ToDictionary(g => g.Key, g => (float)g.Count());

            // If user has no order history but we have a pre-trained model, use it with all available genres
            if (!userGenrePreferences.Any())
            {
                if (_trainedModel != null)
                {
                    var allGenres = await GetAllGenresAsync();
                    if (allGenres.Any())
                    {
                        try
                        {
                            var predictedGenreScores = PredictWithModel(userId, allGenres);
                            var results = await GetGamesFromPredictedGenresAsync(predictedGenreScores, ownedGameIds, maxRecommendations);
                            if (results.Any())
                            {
                                return results;
                            }
                        }
                        catch (InvalidOperationException)
                        {
                            // Model prediction failed, fall through to fallback
                        }
                    }
                }
                return await GetFallbackRecommendationsAsync(ownedGameIds, maxRecommendations);
            }

            // Try to use the pre-trained model first
            if (_trainedModel != null)
            {
                try
                {
                    var allGenres = await GetAllGenresAsync();
                    var predictedGenreScores = PredictWithModel(userId, allGenres);
                    return await GetGamesFromPredictedGenresAsync(predictedGenreScores, ownedGameIds, maxRecommendations);
                }
                catch (InvalidOperationException)
                {
                    // Model prediction failed, fall through to training on-demand
                }
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
            catch (InvalidOperationException)
            {
                // If ML training fails due to invalid data, fall back to genre-based recommendations
                return await GetGenreBasedRecommendationsAsync(userId, userGenrePreferences, ownedGameIds, maxRecommendations);
            }
            catch (ArgumentException)
            {
                // If ML training fails due to invalid arguments, fall back to genre-based recommendations
                return await GetGenreBasedRecommendationsAsync(userId, userGenrePreferences, ownedGameIds, maxRecommendations);
            }
        }

        private async Task<List<string>> GetAllGenresAsync()
        {
            var allGames = await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .ToListAsync();

            return allGames
                .Where(g => g.GameGenresV2 != null)
                .SelectMany(g => g.GameGenresV2!)
                .Select(gg => gg.genre)
                .Distinct()
                .ToList();
        }

        private Dictionary<string, float> PredictWithModel(string userId, IEnumerable<string> genres)
        {
            if (_trainedModel == null)
            {
                throw new InvalidOperationException("Model is not trained");
            }

            var predictionEngine = _mlContext.Model.CreatePredictionEngine<GameRating, GameRatingPrediction>(_trainedModel);

            var predictions = new Dictionary<string, float>();
            foreach (var genre in genres)
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
                    numberOfIterations: MatrixFactorizationIterations,
                    approximationRank: MatrixFactorizationApproximationRank));

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
