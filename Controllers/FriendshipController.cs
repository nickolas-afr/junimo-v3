// Controllers/FriendshipController.cs
using junimo_v3.Models;
using junimo_v3.Services;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace junimo_v3.Controllers
{
    [Authorize]
    public class FriendshipController : Controller
    {
        private readonly IUserService _userService;
        private readonly IFriendshipService _friendshipService;

        public FriendshipController(IUserService userService, IFriendshipService friendshipService)
        {
            _userService = userService;
            _friendshipService = friendshipService;
        }

        [HttpPost]
        public async Task<IActionResult> SendRequest(string friendUsername)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            // Find the friend by username
            var friendUser = await _userService.FindByUsernameAsync(friendUsername);
            if (friendUser == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToAction("Friends", "Account");
            }

            // Don't allow sending friend request to yourself
            if (userId == friendUser.Id)
            {
                TempData["ErrorMessage"] = "You cannot send a friend request to yourself.";
                return RedirectToAction("Friends", "Account");
            }

            // Check if there's already a friendship or pending request
            var status = await _friendshipService.GetFriendshipStatusAsync(userId, friendUser.Id);
            if (status.HasValue)
            {
                TempData["ErrorMessage"] = status switch
                {
                    FriendshipStatus.Accepted => "You are already friends with this user.",
                    FriendshipStatus.Pending => "A friend request is already pending.",
                    _ => "An existing relationship prevents sending a friend request."
                };
                return RedirectToAction("Friends", "Account");
            }

            // Send friend request
            var success = await _friendshipService.SendFriendRequestAsync(userId, friendUser.Id);
            if (success)
            {
                TempData["SuccessMessage"] = $"Friend request sent to {friendUsername}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to send friend request.";
            }

            return RedirectToAction("Friends", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> AcceptRequest(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var success = await _friendshipService.AcceptFriendRequestAsync(userId, friendId);
            if (success)
            {
                TempData["SuccessMessage"] = "Friend request accepted.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to accept friend request.";
            }

            return RedirectToAction("Friends", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> DeclineRequest(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var success = await _friendshipService.DeclineFriendRequestAsync(userId, friendId);
            if (success)
            {
                TempData["SuccessMessage"] = "Friend request declined.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to decline friend request.";
            }

            return RedirectToAction("Friends", "Account");
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFriend(string friendId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return RedirectToAction("Login", "Home");

            var success = await _friendshipService.RemoveFriendAsync(userId, friendId);
            if (success)
            {
                TempData["SuccessMessage"] = "Friend removed successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "Failed to remove friend.";
            }

            return RedirectToAction("Friends", "Account");
        }
    }
}
