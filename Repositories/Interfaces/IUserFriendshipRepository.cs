using junimo_v3.Models;

namespace junimo_v3.Repositories.Interfaces
{
    public interface IUserFriendshipRepository : IRepositoryBase<UserFriendship>
    {
        Task<IEnumerable<UserFriendship>> GetUserFriendshipsAsync(string userId);
        Task<IEnumerable<UserFriendship>> GetPendingFriendRequestsAsync(string userId);
        Task<UserFriendship> GetFriendshipAsync(string userId, string friendId);
    }
}