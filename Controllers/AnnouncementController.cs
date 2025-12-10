using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;
using PWALMS.Services;
using PWALMS.ViewModels;

namespace PWALMS.Controllers
{
    public class AnnouncementController : Controller
    {
        private readonly AuthService _authService;
        private readonly ApplicationDbContext _context;

        public AnnouncementController(AuthService authService, ApplicationDbContext context)
        {
            _authService = authService;
            _context = context;
        }

        // GET: /Announcement
        public IActionResult Index()
        {
            var announcements = _context.Announcements
                .Include(a => a.CreatedBy)
                .Where(a => a.IsActive && (!a.ExpiryDate.HasValue || a.ExpiryDate >= DateTime.Now))
                .OrderByDescending(a => a.CreatedDate)
                .ToList();

            return View(announcements);
        }

        // GET: /Announcement/Create
        public IActionResult Create()
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: /Announcement/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(AnnouncementCreateModel model)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var currentUser = _authService.GetCurrentUser();
                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var announcement = new Announcement
                {
                    Title = model.Title,
                    Content = model.Content,
                    CreatedByUserID = currentUser.UserID,
                    CreatedDate = DateTime.Now,
                    ExpiryDate = model.ExpiryDate,
                    Priority = model.Priority,
                    IsActive = true
                };

                _context.Announcements.Add(announcement);
                _context.SaveChanges();

                TempData["Success"] = "Announcement created successfully!";
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // GET: /Announcement/Delete/{id}
        public IActionResult Delete(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var announcement = _context.Announcements.Find(id);
            if (announcement == null)
                return NotFound();

            return View(announcement);
        }

        // POST: /Announcement/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var announcement = _context.Announcements.Find(id);
            if (announcement != null)
            {
                announcement.IsActive = false;
                _context.SaveChanges();
                TempData["Success"] = "Announcement deleted successfully!";
            }

            return RedirectToAction("Index");
        }
    }
}