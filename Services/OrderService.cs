// Services/OrderService.cs
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class OrderService : IOrderService
    {
        private readonly IRepositoryWrapper _repository;

        public OrderService(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        // Get user cart
        public async Task<Order> GetCurrentCartAsync(string userId)
        {
            var cart = await _repository.Order.GetUserCartAsync(userId);

            // If no cart exists, create a new one
            if (cart == null)
            {
                cart = new Order
                {
                    UserId = userId,
                    OrderDate = DateTime.Now,
                    Total = 0,
                    Status = OrderStatus.InCart,
                    OrderItems = new List<OrderItems>()
                };

                _repository.Order.Create(cart);
                await _repository.SaveAsync();
            }

            return cart;
        }

        // Add item to cart
        public async Task<OrderItems> AddToCartAsync(string userId, int gameId, int quantity = 1)
        {
            var cart = await GetCurrentCartAsync(userId);
            var game = await _repository.Game.GetByIdAsync(gameId);

            if (game == null)
                throw new ArgumentException("Game not found");

            // Check if item already exists in cart
            var existingItem = cart.OrderItems?.FirstOrDefault(oi => oi.GameId == gameId);

            if (existingItem != null)
            {
                // Update quantity if item already exists
                existingItem.Quantity += quantity;
                _repository.OrderItems.Update(existingItem);
            }
            else
            {
                // Add new item
                var orderItem = new OrderItems
                {
                    OrderId = cart.OrderId,
                    GameId = gameId,
                    Price = (decimal)game.Price,
                    Quantity = quantity
                };

                if (cart.OrderItems == null)
                    cart.OrderItems = new List<OrderItems>();

                cart.OrderItems.Add(orderItem);
                _repository.OrderItems.Create(orderItem);
            }

            // Update order total
            UpdateCartTotal(cart);
            await _repository.SaveAsync();

            return existingItem ?? cart.OrderItems.Last();
        }

        // Remove item from cart
        public async Task RemoveFromCartAsync(string userId, int gameId)
        {
            var cart = await GetCurrentCartAsync(userId);
            var item = cart.OrderItems?.FirstOrDefault(oi => oi.GameId == gameId);

            if (item != null)
            {
                cart.OrderItems.Remove(item);
                _repository.OrderItems.Delete(item);

                UpdateCartTotal(cart);
                await _repository.SaveAsync();
            }
        }

        // Update cart item quantity
        public async Task UpdateCartItemQuantityAsync(string userId, int gameId, int quantity)
        {
            var cart = await GetCurrentCartAsync(userId);
            var item = cart.OrderItems?.FirstOrDefault(oi => oi.GameId == gameId);

            if (item != null)
            {
                if (quantity <= 0)
                {
                    cart.OrderItems.Remove(item);
                    _repository.OrderItems.Delete(item);
                }
                else
                {
                    item.Quantity = quantity;
                    _repository.OrderItems.Update(item);
                }

                UpdateCartTotal(cart);
                await _repository.SaveAsync();
            }
        }

        // Checkout - convert cart to order and update user's game list
        public async Task<Order> CheckoutAsync(string userId)
        {
            var cart = await GetCurrentCartAsync(userId);

            if (cart.OrderItems == null || !cart.OrderItems.Any())
                throw new InvalidOperationException("Cannot checkout an empty cart");

            // Update order status and date
            cart.Status = OrderStatus.Completed;
            cart.OrderDate = DateTime.Now;

            // Update user's game list
            var user = await _repository.User.GetByIdAsync(userId);
            if (user == null)
                throw new InvalidOperationException("User not found");

            if (user.Games == null)
                user.Games = new List<Game>();

            foreach (var orderItem in cart.OrderItems)
            {
                var game = await _repository.Game.GetByIdAsync(orderItem.GameId);
                if (game != null && !user.Games.Any(g => g.GameId == game.GameId))
                {
                    user.Games.Add(game);
                }
            }

            // Save changes to the user
            _repository.User.Update(user);

            // Save the updated order
            _repository.Order.Update(cart);
            await _repository.SaveAsync();

            return cart;
        }


        // Get user orders (not including cart)
        public async Task<IEnumerable<Order>> GetUserOrdersAsync(string userId)
        {
            var orders = await _repository.Order.GetUserOrdersAsync(userId);
            return orders.Where(o => o.Status != OrderStatus.InCart);
        }

        // Get specific order with items
        public async Task<Order> GetOrderDetailsAsync(int orderId, string userId)
        {
            var order = await _repository.Order.GetOrderWithItemsAsync(orderId);

            if (order == null || order.UserId != userId)
                return null;

            return order;
        }

        // Update order status
        public async Task UpdateOrderStatusAsync(int orderId, OrderStatus status)
        {
            var order = await _repository.Order.GetByIdAsync(orderId);

            if (order != null)
            {
                order.Status = status;
                _repository.Order.Update(order);
                await _repository.SaveAsync();
            }
        }

        // Clear cart
        public async Task ClearCartAsync(string userId)
        {
            var cart = await GetCurrentCartAsync(userId);

            if (cart.OrderItems != null && cart.OrderItems.Any())
            {
                foreach (var item in cart.OrderItems.ToList())
                {
                    _repository.OrderItems.Delete(item);
                }

                cart.OrderItems.Clear();
                cart.Total = 0;

                _repository.Order.Update(cart);
                await _repository.SaveAsync();
            }
        }

        // Helper method to calculate total
        private void UpdateCartTotal(Order cart)
        {
            cart.Total = cart.OrderItems?.Sum(item => (float)((decimal)item.Price * item.Quantity)) ?? 0;
        }

    }
}
