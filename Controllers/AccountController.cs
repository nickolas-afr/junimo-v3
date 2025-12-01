// Controllers/AccountController.cs
using junimo_v3.Models;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace junimo_v3.Controllers
{
    [Authorize]
    public class AccountController(
        IUserService userService,
        IFriendshipService friendshipService)
        : Controller
    {
        private readonly long _maxFileSize = 5 * 1024 * 1024; // 5MB

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            // Get user with games and other related data included
            var user = await userService.GetUserWithGamesAsync(userId);
            var friends = await friendshipService.GetFriendsAsync(userId);

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

            var user = await userService.GetUserByIdAsync(userId);
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(User model, IFormFile profileImage)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var user = await userService.GetUserByIdAsync(userId);

            // Update user fields that are editable
            user.UserName = model.UserName;
            user.Email = model.Email;

            // Handle profile image upload if provided
            if (profileImage.Length > 0)
            {
                // Validate file size
                if (profileImage.Length > _maxFileSize)
                {
                    ModelState.AddModelError("profileImage", "Image size cannot exceed 5MB");
                    return View(model);
                }

                // Validate file type
                string[] allowedTypes = ["image/jpeg", "image/png", "image/gif", "image/bmp"];
                if (!allowedTypes.Contains(profileImage.ContentType.ToLower()))
                {
                    ModelState.AddModelError("profileImage", "Only image files (JPEG, PNG, GIF, BMP) are allowed");
                    return View(model);
                }

                // Read the image file into a byte array
                using var memoryStream = new MemoryStream();
                await profileImage.CopyToAsync(memoryStream);
                byte[] imageData = memoryStream.ToArray();
                    
                // Update the user's profile picture
                await userService.UpdateProfilePictureAsync(userId, imageData, profileImage.ContentType);
            }

            var result = await userService.UpdateUserAsync(user);

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

            var user = await userService.GetCurrentUserAsync(User);
            
            var result = await userService.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

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

            var friends = await friendshipService.GetFriendsAsync(userId);
            var pendingRequests = await friendshipService.GetPendingFriendRequestsAsync(userId);

            ViewBag.PendingRequests = pendingRequests;

            return View(friends);
        }
    }
}
