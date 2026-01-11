using Microsoft.ML;
using Microsoft.ML.Trainers;
using junimo_v3.Models;
using junimo_v3.Models.ML;
using junimo_v3.Services.Interfaces;
using junimo_v3.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    /// <summary>
    /// ML.NET-based recommendation service using Matrix Factorization.
    /// Supports pre-trained models and incremental learning.
    /// </summary>
    public class RecommendationService : IRecommendationService
    {
        private readonly MLContext _mlContext;
        private readonly IRepositoryWrapper _repository;
        private readonly ILogger<RecommendationService> _logger;
        private readonly string _modelPath;
        private readonly string _trainingDataPath;
        private ITransformer? _model;
        private PredictionEngine<GameRating, GameRatingPrediction>? _predictionEngine;
        private readonly object _lock = new();
        private List<GameRating> _trainingData = new();

        public bool IsModelReady => _model != null;

        public RecommendationService(
            IRepositoryWrapper repository,
            ILogger<RecommendationService> logger,
            IWebHostEnvironment environment)
        {
            _mlContext = new MLContext(seed: 42);
            _repository = repository;
            _logger = logger;

            // Set up paths for model and training data persistence
            var mlDirectory = Path.Combine(environment.ContentRootPath, "ML");
            Directory.CreateDirectory(mlDirectory);
            _modelPath = Path.Combine(mlDirectory, "recommendation-model.zip");
            _trainingDataPath = Path.Combine(mlDirectory, "training-data.csv");

            // Load existing model or initialize with sample data
            InitializeModel();
        }

        private void InitializeModel()
        {
            try
            {
                // Try to load existing model
                if (File.Exists(_modelPath))
                {
                    _logger.LogInformation("Loading existing recommendation model from {Path}", _modelPath);
                    _model = _mlContext.Model.Load(_modelPath, out _);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<GameRating, GameRatingPrediction>(_model);
                    LoadTrainingData();
                    _logger.LogInformation("Recommendation model loaded successfully");
                }
                else
                {
                    // Initialize with sample data for demo
                    _logger.LogInformation("No existing model found. Initializing with sample data...");
                    InitializeWithSampleData();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error initializing recommendation model");
                // Initialize with sample data as fallback
                InitializeWithSampleData();
            }
        }

        private void InitializeWithSampleData()
        {
            // Sample training data for demo purposes
            // This simulates user ratings to bootstrap the model
            _trainingData = new List<GameRating>
            {
                // Sample users rating various games (GameId values will be actual game IDs)
                new GameRating { UserId = "sample-user-1", GameId = 1, Label = 0.9f },
                new GameRating { UserId = "sample-user-1", GameId = 2, Label = 0.8f },
                new GameRating { UserId = "sample-user-1", GameId = 3, Label = 0.7f },
                new GameRating { UserId = "sample-user-2", GameId = 1, Label = 0.8f },
                new GameRating { UserId = "sample-user-2", GameId = 4, Label = 0.9f },
                new GameRating { UserId = "sample-user-2", GameId = 5, Label = 0.6f },
                new GameRating { UserId = "sample-user-3", GameId = 2, Label = 0.7f },
                new GameRating { UserId = "sample-user-3", GameId = 3, Label = 0.9f },
                new GameRating { UserId = "sample-user-3", GameId = 6, Label = 0.8f },
                new GameRating { UserId = "sample-user-4", GameId = 1, Label = 0.6f },
                new GameRating { UserId = "sample-user-4", GameId = 5, Label = 0.8f },
                new GameRating { UserId = "sample-user-4", GameId = 7, Label = 0.9f },
                new GameRating { UserId = "sample-user-5", GameId = 2, Label = 0.9f },
                new GameRating { UserId = "sample-user-5", GameId = 4, Label = 0.7f },
                new GameRating { UserId = "sample-user-5", GameId = 8, Label = 0.8f },
            };

            TrainModel();
            SaveTrainingData();
        }

        private void LoadTrainingData()
        {
            if (File.Exists(_trainingDataPath))
            {
                try
                {
                    var lines = File.ReadAllLines(_trainingDataPath);
                    _trainingData = lines.Skip(1) // Skip header
                        .Select(line =>
                        {
                            var parts = line.Split(',');
                            return new GameRating
                            {
                                UserId = parts[0],
                                GameId = uint.Parse(parts[1]),
                                Label = float.Parse(parts[2])
                            };
                        })
                        .ToList();
                    _logger.LogInformation("Loaded {Count} training data records", _trainingData.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not load training data, starting fresh");
                    _trainingData = new List<GameRating>();
                }
            }
        }

        private void SaveTrainingData()
        {
            try
            {
                var lines = new List<string> { "UserId,GameId,Label" };
                lines.AddRange(_trainingData.Select(r => $"{r.UserId},{r.GameId},{r.Label}"));
                File.WriteAllLines(_trainingDataPath, lines);
                _logger.LogInformation("Saved {Count} training data records", _trainingData.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving training data");
            }
        }

        private void TrainModel()
        {
            lock (_lock)
            {
                try
                {
                    if (_trainingData.Count < 5)
                    {
                        _logger.LogWarning("Not enough training data to train the model ({Count} records)", _trainingData.Count);
                        return;
                    }

                    _logger.LogInformation("Training recommendation model with {Count} records", _trainingData.Count);

                    var dataView = _mlContext.Data.LoadFromEnumerable(_trainingData);

                    // Build the ML pipeline
                    var pipeline = _mlContext.Transforms.Conversion
                        .MapValueToKey(inputColumnName: "UserId", outputColumnName: "UserIdEncoded")
                        .Append(_mlContext.Transforms.Conversion
                            .MapValueToKey(inputColumnName: "GameId", outputColumnName: "GameIdEncoded"))
                        .Append(_mlContext.Recommendation().Trainers.MatrixFactorization(
                            new MatrixFactorizationTrainer.Options
                            {
                                MatrixColumnIndexColumnName = "UserIdEncoded",
                                MatrixRowIndexColumnName = "GameIdEncoded",
                                LabelColumnName = "Label",
                                NumberOfIterations = 20,
                                ApproximationRank = 8,
                                Quiet = true
                            }));

                    // Train the model
                    _model = pipeline.Fit(dataView);
                    _predictionEngine = _mlContext.Model.CreatePredictionEngine<GameRating, GameRatingPrediction>(_model);

                    // Save the model
                    _mlContext.Model.Save(_model, dataView.Schema, _modelPath);
                    _logger.LogInformation("Recommendation model trained and saved successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error training recommendation model");
                }
            }
        }

        public async Task<IEnumerable<Game>> GetRecommendationsForUserAsync(string userId, int count = 5)
        {
            if (!IsModelReady)
            {
                _logger.LogWarning("Model not ready, returning featured games as fallback");
                return await GetFallbackRecommendationsAsync(count);
            }

            try
            {
                // Get all games
                var allGames = await _repository.Game.FindAll()
                    .Include(g => g.GameGenresV2)
                    .ToListAsync();

                // Get games the user already owns or has reviewed
                var userReviews = await _repository.Review
                    .FindByCondition(r => r.UserId == userId)
                    .Select(r => r.GameId)
                    .ToListAsync();

                var user = await _repository.User.FindByCondition(u => u.Id == userId)
                    .Include(u => u.Games)
                    .FirstOrDefaultAsync();

                var ownedGameIds = user?.Games?.Select(g => g.GameId).ToHashSet() ?? new HashSet<int>();

                // Filter out games the user already owns or has reviewed
                var candidateGames = allGames
                    .Where(g => !ownedGameIds.Contains(g.GameId) && !userReviews.Contains(g.GameId))
                    .ToList();

                if (!candidateGames.Any())
                {
                    return await GetFallbackRecommendationsAsync(count);
                }

                // Score each candidate game
                var scoredGames = candidateGames
                    .Select(game => new
                    {
                        Game = game,
                        Score = PredictRating(userId, game.GameId)
                    })
                    .OrderByDescending(x => x.Score)
                    .Take(count)
                    .Select(x => x.Game)
                    .ToList();

                return scoredGames;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recommendations for user {UserId}", userId);
                return await GetFallbackRecommendationsAsync(count);
            }
        }

        private async Task<IEnumerable<Game>> GetFallbackRecommendationsAsync(int count)
        {
            // Return featured games as fallback
            var games = await _repository.Game.FindAll()
                .Include(g => g.GameGenresV2)
                .Where(g => g.IsFeatureRecommended)
                .Take(count)
                .ToListAsync();

            // If not enough featured games, get highest rated
            if (games.Count < count)
            {
                var additionalGames = await _repository.Game.FindAll()
                    .Include(g => g.GameGenresV2)
                    .Where(g => !g.IsFeatureRecommended)
                    .Take(count - games.Count)
                    .ToListAsync();

                games.AddRange(additionalGames);
            }

            return games;
        }

        public async Task AddRatingAsync(string userId, int gameId, float rating, bool retrain = false)
        {
            // Normalize rating from 1-10 scale to 0-1 scale
            var normalizedRating = rating / 10f;

            lock (_lock)
            {
                // Check if this user-game pair already exists
                var existingRating = _trainingData
                    .FirstOrDefault(r => r.UserId == userId && r.GameId == (uint)gameId);

                if (existingRating != null)
                {
                    // Update existing rating
                    existingRating.Label = normalizedRating;
                }
                else
                {
                    // Add new rating
                    _trainingData.Add(new GameRating
                    {
                        UserId = userId,
                        GameId = (uint)gameId,
                        Label = normalizedRating
                    });
                }

                SaveTrainingData();
            }

            if (retrain)
            {
                await RetrainModelAsync();
            }
        }

        public Task RetrainModelAsync()
        {
            return Task.Run(() => TrainModel());
        }

        public float PredictRating(string userId, int gameId)
        {
            if (_predictionEngine == null)
            {
                return 0.5f; // Default neutral score
            }

            try
            {
                var prediction = _predictionEngine.Predict(new GameRating
                {
                    UserId = userId,
                    GameId = (uint)gameId
                });

                // Clamp the score between 0 and 1
                return Math.Clamp(prediction.Score, 0f, 1f);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error predicting rating for user {UserId} and game {GameId}", userId, gameId);
                return 0.5f;
            }
        }
    }
}
