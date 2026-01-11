using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class RecommendationService : IRecommendationService
    {
        private readonly IRepositoryWrapper _repository;

        public RecommendationService(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// Gets game recommendations for a user based on their purchase history and genre preferences
        /// </summary>
        public async Task<IEnumerable<GameRecommendation>> GetRecommendationsForUserAsync(string userId, int topN = 10)
        {
            // Get user's genre preferences based on purchase history
            var genrePreferences = await GetUserGenrePreferencesAsync(userId);
            
            if (genrePreferences.Count == 0)
            {
                // If user has no purchase history, return featured games
                var featuredGames = await _repository.Game
                    .FindByCondition(g => g.IsFeatureRecommended)
                    .Include(g => g.GameGenresV2)
                    .Take(topN)
                    .ToListAsync();
                    
                return featuredGames.Select(g => new GameRecommendation
                {
                    GameId = g.GameId,
                    GameTitle = g.Title,
                    Price = g.Price,
                    ImageUrl = g.GamePictureURL,
                    Score = 0.5f, // Default score for featured games
                    Genres = g.GameGenresV2?.Select(gg => gg.genre).ToList() ?? new List<string>()
                });
            }

            // Get user's owned games to exclude from recommendations
            var user = await _repository.User
                .FindByCondition(u => u.Id == userId)
                .Include(u => u.Games)
                .FirstOrDefaultAsync();
                
            var ownedGameIds = user?.Games?.Select(g => g.GameId).ToHashSet() ?? new HashSet<int>();

            // Get all games with their genres
            var allGames = await _repository.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .Where(g => !ownedGameIds.Contains(g.GameId))
                .ToListAsync();

            // Calculate recommendation scores based on genre preferences
            var recommendations = new List<GameRecommendation>();
            
            foreach (var game in allGames)
            {
                float score = 0f;
                var gameGenres = game.GameGenresV2?.Select(gg => gg.genre).ToList() ?? new List<string>();
                
                // Calculate score based on matching genres
                foreach (var genre in gameGenres)
                {
                    if (genrePreferences.ContainsKey(genre))
                    {
                        score += genrePreferences[genre];
                    }
                }
                
                // Normalize score by number of genres to avoid bias toward multi-genre games
                if (gameGenres.Count > 0)
                {
                    score /= gameGenres.Count;
                }
                
                recommendations.Add(new GameRecommendation
                {
                    GameId = game.GameId,
                    GameTitle = game.Title,
                    Price = game.Price,
                    ImageUrl = game.GamePictureURL,
                    Score = score,
                    Genres = gameGenres
                });
            }

            // Return top N recommendations sorted by score
            return recommendations
                .OrderByDescending(r => r.Score)
                .Take(topN)
                .ToList();
        }

        /// <summary>
        /// Calculates genre preferences for a user based on their purchase history
        /// Returns a dictionary with genre names as keys and preference scores as values
        /// </summary>
        public async Task<Dictionary<string, float>> GetUserGenrePreferencesAsync(string userId)
        {
            // Get user's purchase history (completed orders)
            var userOrders = await _repository.Order
                .FindByCondition(o => o.UserId == userId && o.Status == OrderStatus.Completed)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Game)
                        .ThenInclude(g => g.GameGenresV2)
                .ToListAsync();

            var genrePreferences = new Dictionary<string, float>();

            // Calculate genre preferences based on purchased games
            foreach (var order in userOrders)
            {
                if (order.OrderItems == null) continue;

                foreach (var orderItem in order.OrderItems)
                {
                    var game = orderItem.Game;
                    if (game?.GameGenresV2 == null) continue;

                    // Increase score for each genre in purchased games
                    foreach (var gameGenre in game.GameGenresV2)
                    {
                        var genre = gameGenre.genre;
                        
                        if (!genrePreferences.ContainsKey(genre))
                        {
                            genrePreferences[genre] = 0;
                        }
                        
                        // Increment by quantity to give more weight to multiple purchases of the same genre
                        genrePreferences[genre] += orderItem.Quantity;
                    }
                }
            }

            // Normalize scores to be between 0 and 1
            if (genrePreferences.Count > 0)
            {
                float maxScore = genrePreferences.Values.Max();
                if (maxScore > 0)
                {
                    var normalizedPreferences = genrePreferences.ToDictionary(
                        kvp => kvp.Key,
                        kvp => kvp.Value / maxScore
                    );
                    return normalizedPreferences;
                }
            }

            return genrePreferences;
        }
    }
}
