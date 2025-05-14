// Services/GameGenreV2Service.cs
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class GameGenreV2Service : IGameGenreV2Service
    {
        private readonly IRepositoryWrapper _repositoryWrapper;

        public GameGenreV2Service(IRepositoryWrapper repositoryWrapper)
        {
            _repositoryWrapper = repositoryWrapper;
        }

        public async Task<IEnumerable<GameGenreV2>> GetGenresByGameIdAsync(int gameId)
        {
            return await _repositoryWrapper.GameGenreV2
                .FindByCondition(gg => gg.GameId == gameId)
                .ToListAsync();
        }

        public async Task AddGenreToGameAsync(GameGenreV2 gameGenre)
        {
            _repositoryWrapper.GameGenreV2.Create(gameGenre);
            await _repositoryWrapper.SaveAsync();
        }

        public async Task RemoveGenreFromGameAsync(int gameId, string genre)
        {
            var gameGenre = await _repositoryWrapper.GameGenreV2
                .FindByCondition(gg => gg.GameId == gameId && gg.genre == genre)
                .FirstOrDefaultAsync();

            if (gameGenre != null)
            {
                _repositoryWrapper.GameGenreV2.Delete(gameGenre);
                await _repositoryWrapper.SaveAsync();
            }
        }
    }
}
