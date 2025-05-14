using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Services
{
    public class FriendshipService : IFriendshipService
    {
        private readonly IRepositoryWrapper _repository;

        public FriendshipService(IRepositoryWrapper repository)
        {
            _repository = repository;
        }

        public async Task<bool> SendFriendRequestAsync(string userId, string friendId)
        {
            // Check if a relationship already exists
            var existingFriendship = await _repository.UserFriendship.GetFriendshipAsync(userId, friendId);
            if (existingFriendship != null)
                return false;

            var friendship = new UserFriendship
            {
                UserId = userId,
                FriendId = friendId,
                Status = FriendshipStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            _repository.UserFriendship.Create(friendship);
            await _repository.SaveAsync();
            return true;
        }

        public async Task<bool> AcceptFriendRequestAsync(string userId, string friendRequestId)
        {
            var friendRequest = await _repository.UserFriendship.FindByCondition(
                f => f.FriendId == userId &&
                     f.UserId == friendRequestId &&
                     f.Status == FriendshipStatus.Pending)
                .FirstOrDefaultAsync();

            if (friendRequest == null)
                return false;

            friendRequest.Status = FriendshipStatus.Accepted;
            friendRequest.UpdatedAt = DateTime.UtcNow;

            _repository.UserFriendship.Update(friendRequest);
            await _repository.SaveAsync();
            return true;
        }

        public async Task<bool> DeclineFriendRequestAsync(string userId, string friendRequestId)
        {
            var friendRequest = await _repository.UserFriendship.FindByCondition(
                f => f.FriendId == userId &&
                     f.UserId == friendRequestId &&
                     f.Status == FriendshipStatus.Pending)
                .FirstOrDefaultAsync();

            if (friendRequest == null)
                return false;

            _repository.UserFriendship.Delete(friendRequest);
            await _repository.SaveAsync();
            return true;
        }

        public async Task<bool> RemoveFriendAsync(string userId, string friendId)
        {
            var friendship = await _repository.UserFriendship.GetFriendshipAsync(userId, friendId);
            if (friendship == null || friendship.Status != FriendshipStatus.Accepted)
                return false;

            _repository.UserFriendship.Delete(friendship);
            await _repository.SaveAsync();
            return true;
        }

        public async Task<IEnumerable<User>> GetFriendsAsync(string userId)
        {
            var friendships = await _repository.UserFriendship.GetUserFriendshipsAsync(userId);
            var friends = new List<User>();

            foreach (var friendship in friendships)
            {
                if (friendship.UserId == userId)
                    friends.Add(friendship.Friend);
                else
                    friends.Add(friendship.User);
            }

            return friends;
        }

        public async Task<IEnumerable<User>> GetPendingFriendRequestsAsync(string userId)
        {
            var pendingRequests = await _repository.UserFriendship.GetPendingFriendRequestsAsync(userId);
            return pendingRequests.Select(r => r.User).ToList();
        }

        public async Task<bool> IsFriendAsync(string userId, string potentialFriendId)
        {
            var friendship = await _repository.UserFriendship.GetFriendshipAsync(userId, potentialFriendId);
            return friendship != null && friendship.Status == FriendshipStatus.Accepted;
        }

        public async Task<FriendshipStatus?> GetFriendshipStatusAsync(string userId, string otherUserId)
        {
            var friendship = await _repository.UserFriendship.GetFriendshipAsync(userId, otherUserId);
            return friendship?.Status;
        }
    }
}