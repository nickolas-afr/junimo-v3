using junimo_v3.Models;

namespace junimo_v3.Repositories.Interfaces
{
    public interface IOrderRepository : IRepositoryBase<Order>
    {
        Task<Order> GetOrderWithItemsAsync(int orderId);
        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
        Task<Order> GetUserCartAsync(string userId);
    }
}
