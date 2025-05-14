using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;

namespace junimo_v3.Repositories
{
    public class GameGenreRepository : RepositoryBase<GameGenre>, IGameGenreRepository
    {
        public GameGenreRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
