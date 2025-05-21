// Controllers/AccountController.cs
using junimo_v3.Models;
using junimo_v3.Services;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NuGet.Protocol.Core.Types;
using System.Security.Claims;

namespace junimo_v3.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IUserService _userService;
        private readonly IGameService _gameService;
        private readonly IFriendshipService _friendshipService;
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        public AccountController(
            IUserService userService,
            IGameService gameService,
            IFriendshipService friendshipService)
        {
            _userService = userService;
            _gameService = gameService;
            _friendshipService = friendshipService;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            // Get user with games and other related data included
            var user = await _userService.GetUserWithGamesAsync(userId);
            var friends = await _friendshipService.GetFriendsAsync(userId);

            // Get user's last played game (assuming first game in collection is most recent)
            var lastGame = user.Games?.OrderByDescending(g => g.GameId).FirstOrDefault();

            ViewBag.FriendsCount = friends.Count();
            ViewBag.LastGame = lastGame;

            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var user = await _userService.GetUserByIdAsync(userId);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User model, IFormFile profileImage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var user = await _userService.GetUserByIdAsync(userId);

            // Update user fields that are editable
            user.UserName = model.UserName;
            user.Email = model.Email;

            // Handle profile image upload if provided
            if (profileImage != null && profileImage.Length > 0)
            {
                // Validate file size
                if (profileImage.Length > _maxFileSize)
                {
                    ModelState.AddModelError("profileImage", "Image size cannot exceed 5MB");
                    return View(model);
                }

                // Validate file type
                string[] allowedTypes = { "image/jpeg", "image/png", "image/gif", "image/bmp" };
                if (!allowedTypes.Contains(profileImage.ContentType.ToLower()))
                {
                    ModelState.AddModelError("profileImage", "Only image files (JPEG, PNG, GIF, BMP) are allowed");
                    return View(model);
                }

                // Read the image file into a byte array
                using (var memoryStream = new MemoryStream())
                {
                    await profileImage.CopyToAsync(memoryStream);
                    byte[] imageData = memoryStream.ToArray();
                    
                    // Update the user's profile picture
                    await _userService.UpdateProfilePictureAsync(userId, imageData, profileImage.ContentType);
                }
            }

            var result = await _userService.UpdateUserAsync(user);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePassword model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userService.GetCurrentUserAsync(User);
            if (user == null)
                return RedirectToAction("Login", "Home");

            var result = await _userService.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Password changed successfully!";
                return RedirectToAction(nameof(Index));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Friends()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var friends = await _friendshipService.GetFriendsAsync(userId);
            var pendingRequests = await _friendshipService.GetPendingFriendRequestsAsync(userId);

            ViewBag.PendingRequests = pendingRequests;

            return View(friends);
        }
    }
}
