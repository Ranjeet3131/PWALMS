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

        // GET: /Account/Users
        public IActionResult Users()
        {
            // Only Admin can access
            var currentUser = _authService.GetCurrentUser();
            if (currentUser == null || currentUser.Role?.RoleName != "Admin")
                return RedirectToAction("Login", "Account");

            var users = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .OrderBy(u => u.FullName)
                .ToList();

            return View(users);
        }

        // GET: /Account/Edit/{id}
        // GET: /Account/Edit/{id}
        public IActionResult Edit(int id)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .FirstOrDefault(u => u.UserID == id);

            if (user == null)
                return NotFound();

            ViewBag.Departments = _context.Departments.ToList();
            ViewBag.Roles = _context.Roles.ToList();

            var model = new UserEditModel
            {
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                DepartmentID = user.DepartmentID,
                RoleID = user.RoleID,
                IsActive = user.IsActive
            };

            return View(model);
        }

        // POST: /Account/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(UserEditModel model)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var user = _context.Users.Find(model.UserID);
                if (user == null)
                    return NotFound();

                // Don't allow editing admin username
                if (user.Username == "admin")
                {
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.IsActive = model.IsActive;
                }
                else
                {
                    user.Username = model.Username;
                    user.FullName = model.FullName;
                    user.Email = model.Email;
                    user.DepartmentID = model.DepartmentID;
                    user.RoleID = model.RoleID;
                    user.IsActive = model.IsActive;
                }

                // Update password if provided
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                }

                _context.SaveChanges();
                TempData["Success"] = "User updated successfully!";
                return RedirectToAction("Users");
            }

            ViewBag.Departments = _context.Departments.ToList();
            ViewBag.Roles = _context.Roles.ToList();
            return View(model);
        }

        // GET: /Account/ResetPassword/{id}
        public IActionResult ResetPassword(int id)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            ViewBag.User = user;
            return View();
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ResetPassword(int userId, string newPassword)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(newPassword) || newPassword.Length < 6)
            {
                TempData["Error"] = "Password must be at least 6 characters!";
                return RedirectToAction("ResetPassword", new { id = userId });
            }

            var user = _context.Users.Find(userId);
            if (user == null)
                return NotFound();

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            _context.SaveChanges();

            TempData["Success"] = $"Password reset successfully for '{user.Username}'!";
            return RedirectToAction("Users");
        }

        // GET: /Account/Delete/{id}
        public IActionResult Delete(int id)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .FirstOrDefault(u => u.UserID == id);

            if (user == null)
                return NotFound();

            // Don't allow deleting admin user
            if (user.Username == "admin")
            {
                TempData["Error"] = "Cannot delete the primary administrator!";
                return RedirectToAction("Users");
            }

            return View(user);
        }

        // POST: /Account/DeleteConfirmed/{id}
        [HttpPost]
        [ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = _context.Users.Find(id);
            if (user == null)
                return NotFound();

            // Don't allow deleting admin user
            if (user.Username == "admin")
            {
                TempData["Error"] = "Cannot delete the primary administrator!";
                return RedirectToAction("Users");
            }

            // Check if user has any created quizzes
            var userQuizzes = _context.Quizzes.Where(q => q.CreatedBy == id).ToList();
            if (userQuizzes.Any())
            {
                // Reassign quizzes to admin or delete them
                var adminUser = _context.Users.FirstOrDefault(u => u.Username == "admin");
                if (adminUser != null)
                {
                    foreach (var quiz in userQuizzes)
                    {
                        quiz.CreatedBy = adminUser.UserID;
                    }
                }
            }

            // Check if user has any quiz attempts
            var userAttempts = _context.QuizAttempts.Where(a => a.UserID == id).ToList();
            if (userAttempts.Any())
            {
                // Delete user answers for these attempts first
                foreach (var attempt in userAttempts)
                {
                    var answers = _context.UserAnswers.Where(a => a.AttemptID == attempt.AttemptID).ToList();
                    _context.UserAnswers.RemoveRange(answers);
                }
                // Then delete the attempts
                _context.QuizAttempts.RemoveRange(userAttempts);
            }

            // Now delete the user
            _context.Users.Remove(user);
            _context.SaveChanges();

            TempData["Success"] = $"User '{user.Username}' deleted successfully!";
            return RedirectToAction("Users");
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            _authService.ClearSession();
            return RedirectToAction("Login", "Account");
        }
    }
}