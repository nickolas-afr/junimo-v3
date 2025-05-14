using junimo_v3.Data;
using junimo_v3.Models;
using junimo_v3.Repositories;
using junimo_v3.Repositories.Interfaces;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace junimo_v3.Services;

public class UserService : IUserService
{
    private readonly SignInManager<User> _signInManager;
    private readonly UserManager<User> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IRepositoryWrapper _repositoryWrapper;

    public UserService(SignInManager<User> signInManager, UserManager<User> userManager, RoleManager<IdentityRole> roleManager, IRepositoryWrapper userRepository)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _roleManager = roleManager;
        _repositoryWrapper = userRepository;
    }

    public async Task<IdentityResult> Register(User user, string password, List<string> roles)
    {
        var result = await _userManager.CreateAsync(user, password);

        if (result.Succeeded)
        {
            foreach (var role in roles)
            {
                await _userManager.AddToRoleAsync(user, role);
            }
        }

        return result;
    }

    public async Task<SignInResult> Login(string username, string password, bool isPersistent,
        bool lockoutOnFailure)
    {
        return await _signInManager.PasswordSignInAsync(username, password, isPersistent, lockoutOnFailure);
    }

    public async Task Logout()
    {
        await _signInManager.SignOutAsync();
    }

    public async Task<User> GetUserByIdAsync(string userId)
    {
        return await _userManager.FindByIdAsync(userId);
    }

    public async Task<User> GetCurrentUserAsync(ClaimsPrincipal principal)
    {
        return await _userManager.GetUserAsync(principal);
    }

    public async Task<IdentityResult> UpdateUserAsync(User user)
    {
        return await _userManager.UpdateAsync(user);
    }

    public async Task<IdentityResult> ChangePasswordAsync(User user, string currentPassword, string newPassword)
    {
        return await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
    }

    public async Task<bool> IsUserInRoleAsync(User user, string role)
    {
        return await _userManager.IsInRoleAsync(user, role);
    }

    public async Task UpdateLastOnlineAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user != null)
        {
            user.LastOnlineAt = DateTime.UtcNow.AddHours(3);
            await _userManager.UpdateAsync(user);
        }
    }
    public async Task<User> FindByUsernameAsync(string username)
    {
        return await _userManager.FindByNameAsync(username);
    }

    public async Task<User> GetUserWithGamesAsync(string userId)
    {
        return await _repositoryWrapper.User.FindByCondition(u => u.Id == userId)
            .Include(u => u.Games)
                .ThenInclude(g => g.GameGenresV2)
            .FirstOrDefaultAsync();
    }
}