using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;

namespace junimo_v3.Repositories
{
    public class GenreRepository : RepositoryBase<Genre>, IGenreRepository
    {
        public GenreRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
