// Services/OrderService.cs
using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface IOrderService
    {
        Task<Order> GetCurrentCartAsync(string userId);
        Task<OrderItems> AddToCartAsync(string userId, int gameId, int quantity = 1);
        Task RemoveFromCartAsync(string userId, int gameId);
        Task UpdateCartItemQuantityAsync(string userId, int gameId, int quantity);
        Task<Order> CheckoutAsync(string userId);
        Task<IEnumerable<Order>> GetUserOrdersAsync(string userId);
        Task<Order> GetOrderDetailsAsync(int orderId, string userId);
        Task UpdateOrderStatusAsync(int orderId, OrderStatus status);
        Task ClearCartAsync(string userId);
    }
}