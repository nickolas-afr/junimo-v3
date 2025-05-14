using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;

namespace junimo_v3.Repositories
{
    public class OrderItemsRepository : RepositoryBase<OrderItems>, IOrderItemsRepository
    {
        public OrderItemsRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }
    }
}
