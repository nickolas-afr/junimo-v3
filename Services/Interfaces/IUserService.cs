using junimo_v3.Models;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace junimo_v3.Services.Interfaces
{
    public interface IUserService
    {
        Task<IdentityResult> Register(User user, string password, List<string> roles);
        Task<SignInResult> Login(string username, string password, bool isPersistent,
            bool lockoutOnFailure);
        Task Logout();
        Task<User> GetUserByIdAsync(string userId);
        Task<User> GetCurrentUserAsync(ClaimsPrincipal principal);
        Task<IdentityResult> UpdateUserAsync(User user);
        Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword);
        Task<bool> IsUserInRoleAsync(User user, string role);
        Task UpdateLastOnlineAsync(string userId);
        Task<User> FindByUsernameAsync(string username);
        Task<User> GetUserWithGamesAsync(string userId);
    }
}
