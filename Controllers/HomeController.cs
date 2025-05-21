using System.Diagnostics;
using junimo_v3.Models;
using junimo_v3.Services;
using junimo_v3.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace junimo_v3.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserService _userService;
        private readonly IGameService _gameService;
        private const string AdminPassword = "Admin123@";

        public HomeController(IUserService userService, IGameService gameService)
        {
            _userService = userService;
            _gameService = gameService;
        }

        public async Task<IActionResult> Index()
        {
            var games = await _gameService.GetAllGames();
            return View(games);
        }

        [Route("/login")]
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [Route("/login")]
        [HttpPost]
        public async Task<IActionResult> Login(User user)
        {
            if (!ModelState.IsValid) return View(user);
            var result = await _userService.Login(user.UserName, user.PasswordHash, false, false);
            if (result.Succeeded)
            {
                var loggedInUser = await _userService.FindByUsernameAsync(user.UserName);
                if (loggedInUser != null)
                {
                    // Update LastOnlineAt timestamp
                    await _userService.UpdateLastOnlineAsync(loggedInUser.Id);
                }

                return Redirect("/home");
            }

            return View(user);
        }

        [HttpGet]
        [Route("/register")]
        public IActionResult SignUp()
        {
            return View();
        }

        [HttpPost]
        [Route("/register")]
        public async Task<IActionResult> SignUp(User user, string? Role, string? AdminPassword)
        {
            if (!ModelState.IsValid) return View(user);
            var roles = new List<string> { "User" };

            if (!string.IsNullOrEmpty(Role))
            {
                // Admin role was requested, verify the admin password
                if (string.IsNullOrEmpty(AdminPassword) || AdminPassword != HomeController.AdminPassword)
                {
                    // Invalid admin password
                    ModelState.AddModelError("", "Invalid admin password.");
                    ViewData["AdminPasswordError"] = "The admin password is incorrect.";
                    return View(user);
                }

                // Admin password is correct, add the Admin role
                roles.Add("Admin");
            }

            var result = await _userService.Register(user, user.PasswordHash, roles);

            if (result.Succeeded)
            {
                return Redirect("/login");
            }

            return View(user);
        }

        [Route("/logout")]
        public async Task<IActionResult> Logout()
        {
            await _userService.Logout();
            return Redirect("/login");
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
