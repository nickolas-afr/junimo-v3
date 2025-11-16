using junimo_v3.Models;
using junimo_v3.Repositories;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class GameService : IGameService
    {
        private readonly IRepositoryWrapper _repositoryWrapper; 

        public GameService(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task CreateGame(Game game)
        {
            _repositoryWrapper.Game.Create(game);
            await _repositoryWrapper.SaveAsync();
        }
        
        public async Task<IEnumerable<Game>> GetAllGames()
        {
            return await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .ToListAsync();
        }
        
        public async Task<IEnumerable<Game>> GetAllGamesFeatured()
        {
            var featuredGames = await _repositoryWrapper.Game.GetAllAsync();
            return featuredGames.Where(game => game.IsFeatureRecommended);
        }

        public async Task<IEnumerable<Game>> SearchGamesAsync(string searchTerm)
        {
            return await _repositoryWrapper.Game
                .FindByCondition(g => g.Title.Contains(searchTerm) ||
                                      g.Description.Contains(searchTerm))
                .Include(g => g.GameGenresV2)
                .ToListAsync();
        }

        public async Task<Game> GetGameById(int id)
        {
            return await _repositoryWrapper.Game
                .FindByCondition(g => g.GameId == id)
                .Include(g => g.GameGenresV2)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateGamePictureAsync(int gameId, byte[] imageData, string contentType)
        {
            var game = await _repositoryWrapper.Game
                .FindByCondition(g => g.GameId == gameId)
                .FirstOrDefaultAsync();

            if (game == null)
                return false;

            game.GamePicture = imageData;
            game.GamePictureContentType = contentType;
            
            _repositoryWrapper.Game.Update(game);
            await _repositoryWrapper.SaveAsync();
            return true;
        }

        public async Task<Game> GetGameByIdAsync(int id)
        {
            return await _repositoryWrapper.Game.GetByIdAsync(id);
        }

        public async Task<bool> UpdateGame(Game game)
        {
            var existingGame = await _repositoryWrapper.Game
                .FindByCondition(g => g.GameId == game.GameId)
                .FirstOrDefaultAsync();

            if (existingGame == null)
                return false;

            existingGame.Title = game.Title;
            existingGame.Description = game.Description;
            existingGame.Price = game.Price;
            existingGame.ReleaseDate = game.ReleaseDate;
            existingGame.IsFeatureRecommended = game.IsFeatureRecommended;
            
            // Only update picture if a new one is provided
            if (game.GamePicture != null && game.GamePicture.Length > 0)
            {
                existingGame.GamePicture = game.GamePicture;
                existingGame.GamePictureContentType = game.GamePictureContentType;
            }

            _repositoryWrapper.Game.Update(existingGame);
            await _repositoryWrapper.SaveAsync();
            return true;
        }

        public async Task<bool> DeleteGame(int id)
        {
            var game = await _repositoryWrapper.Game
                .FindByCondition(g => g.GameId == id)
                .FirstOrDefaultAsync();

            if (game == null)
                return false;

            _repositoryWrapper.Game.Delete(game);
            await _repositoryWrapper.SaveAsync();
            return true;
        }

        public async Task<IEnumerable<Game>> GetGamesByGenreAsync(string genre)
        {
            return await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .Where(g => g.GameGenresV2 != null && g.GameGenresV2.Any(gg => gg.genre == genre))
                .ToListAsync();
        }

        public async Task<IEnumerable<Game>> GetGamesByGenresAsync(List<string> genres)
        {
            if (genres == null || !genres.Any())
            {
                return await GetAllGames();
            }

            return await _repositoryWrapper.Game
                .FindAll()
                .Include(g => g.GameGenresV2)
                .Where(g => g.GameGenresV2 != null && 
                           g.GameGenresV2.Any(gg => genres.Contains(gg.genre)))
                .ToListAsync();
        }
    }
}