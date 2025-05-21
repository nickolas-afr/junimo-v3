using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class ReviewService : IReviewService
    {
        private readonly IRepositoryWrapper _repository;

        public ReviewService(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<IEnumerable<Review>> GetReviewsByGameIdAsync(int gameId)
        {
            return await _repository.Review
                .FindByCondition(r => r.GameId == gameId)
                .Include(r => r.User)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<Review> GetReviewByIdAsync(int reviewId)
        {
            return await _repository.Review
                .FindByCondition(r => r.ReviewId == reviewId)
                .Include(r => r.User)
                .Include(r => r.Game)
                .FirstOrDefaultAsync();
        }

        public async Task<Review> GetUserReviewForGameAsync(string userId, int gameId)
        {
            return await _repository.Review
                .FindByCondition(r => r.UserId == userId && r.GameId == gameId)
                .FirstOrDefaultAsync();
        }

        public async Task<Review> CreateReviewAsync(Review review)
        {
            _repository.Review.Create(review);
            await _repository.SaveAsync();
            return review;
        }

        public async Task UpdateReviewAsync(Review review)
        {
            _repository.Review.Update(review);
            await _repository.SaveAsync();
        }

        public async Task DeleteReviewAsync(int reviewId)
        {
            var review = await GetReviewByIdAsync(reviewId);
            if (review != null)
            {
                _repository.Review.Delete(review);
                await _repository.SaveAsync();
            }
        }

        public async Task<IEnumerable<Review>> GetUserReviewsAsync(string userId)
        {
            return await _repository.Review
                .FindByCondition(r => r.UserId == userId)
                .Include(r => r.Game)
                .OrderByDescending(r => r.CreatedDate)
                .ToListAsync();
        }

        public async Task<bool> UserHasReviewedGameAsync(string userId, int gameId)
        {
            var review = await GetUserReviewForGameAsync(userId, gameId);
            return review != null;
        }
    }
}