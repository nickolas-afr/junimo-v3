using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;

namespace junimo_v3.Repositories
{
    public class GameGenreV2Repository : RepositoryBase<GameGenreV2>, IGameGenreV2Repository
    {
        public GameGenreV2Repository(RepositoryContext repositoryContext)
            : base(repositoryContext)
    {
    }
}
}
