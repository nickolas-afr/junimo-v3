using junimo_v3.Models;

namespace junimo_v3.Services.Interfaces
{
    public interface IFriendshipService
    {
        Task<bool> SendFriendRequestAsync(string userId, string friendId);
        Task<bool> AcceptFriendRequestAsync(string userId, string friendRequestId);
        Task<bool> DeclineFriendRequestAsync(string userId, string friendRequestId);
        Task<bool> RemoveFriendAsync(string userId, string friendId);
        Task<IEnumerable<User>> GetFriendsAsync(string userId);
        Task<IEnumerable<User>> GetPendingFriendRequestsAsync(string userId);
        Task<bool> IsFriendAsync(string userId, string potentialFriendId);
        Task<FriendshipStatus?> GetFriendshipStatusAsync(string userId, string otherUserId);
    }
}