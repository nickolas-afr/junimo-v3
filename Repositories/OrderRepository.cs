using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Repositories
{
    public class OrderRepository : RepositoryBase<Order>, IOrderRepository
    {
        public OrderRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<Order> GetOrderWithItemsAsync(int orderId)
        {
            return await FindByCondition(o => o.OrderId == orderId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Game)
                .FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        {
            return await FindByCondition(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Game)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

        public async Task<Order> GetUserCartAsync(string userId)
        {
            return await FindByCondition(o => o.UserId == userId && o.Status == OrderStatus.InCart)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Game)
                .FirstOrDefaultAsync();
        }
    }
}
