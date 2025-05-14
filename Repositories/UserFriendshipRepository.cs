using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace junimo_v3.Repositories
{
    public class UserFriendshipRepository : RepositoryBase<UserFriendship>, IUserFriendshipRepository
    {
        public UserFriendshipRepository(RepositoryContext repositoryContext)
            : base(repositoryContext)
        {
        }

        public async Task<IEnumerable<UserFriendship>> GetUserFriendshipsAsync(string userId)
        {
            return await FindByCondition(f =>
                    (f.UserId == userId || f.FriendId == userId) &&
                    f.Status == FriendshipStatus.Accepted)
                .Include(f => f.User)
                .Include(f => f.Friend)
                .ToListAsync();
        }

        public async Task<IEnumerable<UserFriendship>> GetPendingFriendRequestsAsync(string userId)
        {
            return await FindByCondition(f =>
                    f.FriendId == userId &&
                    f.Status == FriendshipStatus.Pending)
                .Include(f => f.User)
                .ToListAsync();
        }

        public async Task<UserFriendship> GetFriendshipAsync(string userId, string friendId)
        {
            return await FindByCondition(f =>
                    (f.UserId == userId && f.FriendId == friendId) ||
                    (f.UserId == friendId && f.FriendId == userId))
                .FirstOrDefaultAsync();
        }
    }
}