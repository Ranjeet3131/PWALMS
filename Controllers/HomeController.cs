using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;
using PWALMS.Services;

namespace PWALMS.Controllers
{
    public class HomeController : Controller
    {
        private readonly AuthService _authService;
        private readonly QuizService _quizService;
        private readonly ApplicationDbContext _context;
  
        public HomeController(AuthService authService, QuizService quizService, ApplicationDbContext context)
        {
            _authService = authService;
            _quizService = quizService;
            _context = context;
        }

        public IActionResult Index()
        {
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Dashboard()
        {
            var user = _authService.GetCurrentUser();
            if (user == null)
                return RedirectToAction("Login", "Account");

            ViewBag.User = user;

            // Get recent announcements
            ViewBag.RecentAnnouncements = _context.Announcements
                .Include(a => a.CreatedBy)
                .Where(a => a.IsActive && (!a.ExpiryDate.HasValue || a.ExpiryDate >= DateTime.Now))
                .OrderByDescending(a => a.CreatedDate)
                .Take(5)
                .ToList();

            if (_authService.IsAdmin() || _authService.IsUploader())
            {
                // Admin/Uploader dashboard
                ViewBag.TotalUsers = _context.Users.Count();
                ViewBag.TotalQuizzes = _context.Quizzes.Count(q => q.IsActive);
                ViewBag.TotalAttempts = _context.QuizAttempts.Count();

                ViewBag.RecentAttempts = _context.QuizAttempts
                    .Include(a => a.User)
                    .Include(a => a.Quiz)
                    .Where(a => a.Status == "Completed")
                    .OrderByDescending(a => a.EndTime)
                    .Take(10)
                    .ToList();

                return View("DashboardAdmin");
            }
            else
            {
                // Quiz Taker dashboard
                var availableQuizzes = _quizService.GetQuizzesByDepartment(user.DepartmentID);
                ViewBag.AvailableQuizzes = availableQuizzes;

                var userAttempts = _context.QuizAttempts
                    .Include(a => a.Quiz)
                    .Where(a => a.UserID == user.UserID)
                    .OrderByDescending(a => a.EndTime)
                    .ToList();

                ViewBag.UserAttempts = userAttempts;

                // User performance (only for display, not shown to user)
                ViewBag.UserStats = new
                {
                    TotalQuizzesTaken = userAttempts.Count(a => a.Status == "Completed"),
                    AverageScore = userAttempts
                        .Where(a => a.Status == "Completed")
                        .Average(a => (decimal?)a.Percentage) ?? 0,
                    BestScore = userAttempts
                        .Where(a => a.Status == "Completed")
                        .Max(a => (decimal?)a.Percentage) ?? 0
                };

                return View("DashboardQuizTaker");
            }
    }
    }
}