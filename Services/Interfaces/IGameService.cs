using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface IGameService
    {
        Task CreateGame(Game game);

        Task<IEnumerable<Game>> GetAllGames();
        Task<IEnumerable<Game>> GetAllGamesFeatured();
        Task<Game> GetGameById(int id);
        Task<IEnumerable<Game>> SearchGamesAsync(string searchTerm);
    }
}
