using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface IReviewService
    {
        Task<IEnumerable<Review>> GetReviewsByGameIdAsync(int gameId);
        Task<Review> GetReviewByIdAsync(int reviewId);
        Task<Review> GetUserReviewForGameAsync(string userId, int gameId);
        Task<Review> CreateReviewAsync(Review review);
        Task UpdateReviewAsync(Review review);
        Task DeleteReviewAsync(int reviewId);
        Task<IEnumerable<Review>> GetUserReviewsAsync(string userId);
        Task<bool> UserHasReviewedGameAsync(string userId, int gameId);
    }
}