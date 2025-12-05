using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;
using PWALMS.Services;
using PWALMS.ViewModels;

namespace PWALMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;

        public AccountController(AuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        // GET: /Account/Login
        public IActionResult Login()
        {
            if (_authService.GetCurrentUser() != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(string username, string password)
        {
            var user = _authService.Authenticate(username, password);

            if (user == null)
            {
                ViewBag.Error = "Invalid username or password";
                return View();
            }

            _authService.SetUserSession(user);
            return RedirectToAction("Dashboard", "Home");
        }

        // GET: /Account/Register (Admin/Uploader only)
        public IActionResult Register()
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null || (currentUser.Role?.RoleName != "Admin" && currentUser.Role?.RoleName != "Uploader"))
                return RedirectToAction("Login", "Account");

            ViewBag.Departments = _context.Departments.ToList();
            ViewBag.Roles = _context.Roles.Where(r => r.RoleName != "Admin").ToList();
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Register(UserRegistrationModel model)
        {
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null || (currentUser.Role?.RoleName != "Admin" && currentUser.Role?.RoleName != "Uploader"))
                return RedirectToAction("Login", "Account");

            if (!ModelState.IsValid)
            {
                ViewBag.Departments = _context.Departments.ToList();
                ViewBag.Roles = _context.Roles.Where(r => r.RoleName != "Admin").ToList();
                return View(model);
            }

            // Check if username exists
            if (_context.Users.Any(u => u.Username == model.Username))
            {
                ModelState.AddModelError("Username", "Username already exists");
                ViewBag.Departments = _context.Departments.ToList();
                ViewBag.Roles = _context.Roles.Where(r => r.RoleName != "Admin").ToList();
                return View(model);
            }

            var user = new User
            {
                Username = model.Username,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                FullName = model.FullName,
                Email = model.Email,
                DepartmentID = model.DepartmentID,
                RoleID = model.RoleID,
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            _context.Users.Add(user);
            _context.SaveChanges();

            TempData["Success"] = $"User '{model.Username}' created successfully!";
            return RedirectToAction("Register");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            _authService.ClearSession();
            return RedirectToAction("Login", "Account");
        }
    }
}