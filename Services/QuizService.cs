using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;

namespace PWALMS.Services
{
    public class QuizService
    {
        private readonly ApplicationDbContext _context;

        public QuizService(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Quiz> GetQuizzesByDepartment(int? departmentId = null)
        {
            var query = _context.Quizzes
                .Include(q => q.Department)
                .Include(q => q.CreatedByUser)
                .Where(q => q.IsActive && q.IsPublished);

            if (departmentId.HasValue && departmentId > 0)
            {
                query = query.Where(q => q.DepartmentID == departmentId);
            }

            return query.OrderByDescending(q => q.CreatedDate).ToList();
        }
    }
}