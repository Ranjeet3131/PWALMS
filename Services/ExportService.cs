using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;

namespace PWALMS.Services
{
    public class ExportService
    {
        private readonly ApplicationDbContext _context;

        public ExportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public byte[] ExportQuizResultsToExcel(int quizId)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Department)
                .FirstOrDefault(q => q.QuizID == quizId);

            // FIX: Add null check
            if (quiz == null)
                return Array.Empty<byte>();

            var results = _context.QuizAttempts
                .Include(a => a.User)
                .ThenInclude(u => u.Department)
                .Where(a => a.QuizID == quizId && a.Status == "Completed")
                .OrderByDescending(a => a.Score)
                .ToList();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Quiz Results");

                // Title
                worksheet.Cell(1, 1).Value = $"Quiz Results: {quiz.QuizTitle}";
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 14;
                worksheet.Range(1, 1, 1, 7).Merge();

                // Headers
                worksheet.Cell(3, 1).Value = "Rank";
                worksheet.Cell(3, 2).Value = "Full Name";
                worksheet.Cell(3, 3).Value = "Username";
                worksheet.Cell(3, 4).Value = "Department";
                worksheet.Cell(3, 5).Value = "Score";
                worksheet.Cell(3, 6).Value = "Max Score";
                worksheet.Cell(3, 7).Value = "Percentage";
                worksheet.Cell(3, 8).Value = "Time Taken (min)";

                // Style headers
                var headerRange = worksheet.Range(3, 1, 3, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;

                // Data
                int row = 4;
                int rank = 1;
                foreach (var result in results)
                {
                    worksheet.Cell(row, 1).Value = rank++;
                    worksheet.Cell(row, 2).Value = result.User?.FullName ?? "Unknown";
                    worksheet.Cell(row, 3).Value = result.User?.Username ?? "Unknown";
                    worksheet.Cell(row, 4).Value = result.User?.Department?.DepartmentName ?? "Unknown";
                    worksheet.Cell(row, 5).Value = result.Score;
                    worksheet.Cell(row, 6).Value = result.MaxScore;
                    worksheet.Cell(row, 7).Value = result.Percentage / 100;
                    worksheet.Cell(row, 7).Style.NumberFormat.Format = "0.00%";
                    worksheet.Cell(row, 8).Value = result.TimeTakenMinutes;

                    row++;
                }

                // Auto-fit columns
                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return stream.ToArray();
                }
            }
        }
    }
}