using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;

namespace junimo_v3.Repositories
{
    public class ReviewRepository : RepositoryBase<Review>, IReviewRepository
    {
        public ReviewRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
