// Services/Interfaces/IGameGenreV2Service.cs
using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface IGameGenreV2Service
    {
        Task<IEnumerable<GameGenreV2>> GetGenresByGameIdAsync(int gameId);
        Task AddGenreToGameAsync(GameGenreV2 gameGenre);
        Task RemoveGenreFromGameAsync(int gameId, string genre);
    }
}
