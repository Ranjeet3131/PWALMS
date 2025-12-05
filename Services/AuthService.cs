using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;
using BCrypt.Net;

namespace PWALMS.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public User Authenticate(string username, string password)
        {
            var user = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .FirstOrDefault(u => u.Username == username && u.IsActive);

            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
                return null;

            // Update last login
            user.LastLogin = DateTime.Now;
            _context.SaveChanges();

            return user;
        }

        public void SetUserSession(User user)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            session.SetString("UserId", user.UserID.ToString());
            session.SetString("Username", user.Username);
            session.SetString("FullName", user.FullName);
            session.SetString("RoleId", user.RoleID.ToString());
            session.SetString("RoleName", user.Role.RoleName);
            session.SetString("DepartmentId", user.DepartmentID?.ToString() ?? "0");
            session.SetString("DepartmentName", user.Department?.DepartmentName ?? "");
        }

        public void ClearSession()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            session.Clear();
        }

        public User GetCurrentUser()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var userIdStr = session.GetString("UserId");

            if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out int userId))
                return null;

            return _context.Users
                .Include(u => u.Role)
                .Include(u => u.Department)
                .FirstOrDefault(u => u.UserID == userId);
        }

        public bool IsAdmin()
        {
            var user = GetCurrentUser();
            return user?.Role.RoleName == "Admin";
        }

        public bool IsUploader()
        {
            var user = GetCurrentUser();
            return user?.Role.RoleName == "Uploader" || IsAdmin();
        }

        public bool IsQuizTaker()
        {
            var user = GetCurrentUser();
            return user?.Role.RoleName == "QuizTaker";
        }
    }
}