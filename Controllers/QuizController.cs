using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PWALMS.Data;
using PWALMS.Models;
using PWALMS.Services;
using PWALMS.ViewModels;

namespace PWALMS.Controllers
{
    public class QuizController : Controller
    {
        private readonly AuthService _authService;
        private readonly QuizService _quizService;
        private readonly ExportService _exportService;
        private readonly ApplicationDbContext _context;

        public QuizController(AuthService authService, QuizService quizService,
                            ExportService exportService, ApplicationDbContext context)
        {
            _authService = authService;
            _quizService = quizService;
            _exportService = exportService;
            _context = context;
        }

        // GET: /Quiz/Create
        public IActionResult Create()
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.Departments = _context.Departments.ToList();
            return View();
        }

        // POST: /Quiz/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(QuizCreateModel model)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                var currentUser = _authService.GetCurrentUser();
                if (currentUser == null)
                    return RedirectToAction("Login", "Account");

                var quiz = new Quiz
                {
                    QuizTitle = model.QuizTitle,
                    Description = model.Description,
                    DepartmentID = model.DepartmentID,
                    CreatedBy = currentUser.UserID,
                    TimeLimitMinutes = model.TimeLimitMinutes,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    IsActive = true,
                    IsPublished = false,
                    CreatedDate = DateTime.Now
                };

                _context.Quizzes.Add(quiz);
                _context.SaveChanges();

                return RedirectToAction("AddQuestions", new { id = quiz.QuizID });
            }

            ViewBag.Departments = _context.Departments.ToList();
            return View(model);
        }

        // GET: /Quiz/AddQuestions/{id}
        public IActionResult AddQuestions(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var quiz = _context.Quizzes
                .Include(q => q.Questions!)
                .ThenInclude(q => q.Options!)
                .FirstOrDefault(q => q.QuizID == id);

            if (quiz == null)
                return NotFound();

            ViewBag.Quiz = quiz;
            return View();
        }

        // POST: /Quiz/AddQuestion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddQuestion(QuestionCreateModel model)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            if (ModelState.IsValid)
            {
                // Create question
                var question = new Question
                {
                    QuizID = model.QuizID,
                    QuestionText = model.QuestionText,
                    QuestionType = model.QuestionType,
                    Marks = model.Marks
                };

                _context.Questions.Add(question);
                _context.SaveChanges();

                // Add options
                var options = new List<(string text, bool isCorrect)>
                {
                    (model.Option1 ?? "", model.CorrectOption == 1),
                    (model.Option2 ?? "", model.CorrectOption == 2),
                    (model.Option3 ?? "", model.CorrectOption == 3),
                    (model.Option4 ?? "", model.CorrectOption == 4)
                };

                int order = 1;
                foreach (var opt in options)
                {
                    if (!string.IsNullOrEmpty(opt.text))
                    {
                        var option = new Option
                        {
                            QuestionID = question.QuestionID,
                            OptionText = opt.text,
                            IsCorrect = opt.isCorrect,
                            SortOrder = order++
                        };
                        _context.Options.Add(option);
                    }
                }

                // Update quiz totals
                var quiz = _context.Quizzes.Find(model.QuizID);
                if (quiz != null)
                {
                    quiz.TotalQuestions++;
                    quiz.TotalMarks += model.Marks;
                }

                _context.SaveChanges();

                TempData["Success"] = "Question added successfully!";
                return RedirectToAction("AddQuestions", new { id = model.QuizID });
            }

            return RedirectToAction("AddQuestions", new { id = model.QuizID });
        }

        // GET: /Quiz/Manage
        public IActionResult Manage()
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var user = _authService.GetCurrentUser();
            var quizzes = _context.Quizzes
                .Include(q => q.Department)
                .Include(q => q.CreatedByUser)
                .OrderByDescending(q => q.CreatedDate)
                .ToList();

            if (user?.Role?.RoleName != "Admin")
            {
                quizzes = quizzes.Where(q => q.CreatedBy == user?.UserID).ToList();
            }

            return View(quizzes);
        }

        // GET: /Quiz/Publish/{id}
        public IActionResult Publish(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var quiz = _context.Quizzes.Find(id);
            if (quiz != null)
            {
                quiz.IsPublished = true;
                _context.SaveChanges();
                TempData["Success"] = "Quiz published successfully!";
            }

            return RedirectToAction("Manage");
        }

        // GET: /Quiz/ViewResults/{id}
        public IActionResult ViewResults(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var quiz = _context.Quizzes
                .Include(q => q.Department)
                .FirstOrDefault(q => q.QuizID == id);

            if (quiz == null)
                return NotFound();

            var results = _context.QuizAttempts
                .Include(a => a.User)
                .ThenInclude(u => u.Department)
                .Where(a => a.QuizID == id && a.Status == "Completed")
                .OrderByDescending(a => a.Score)
                .ToList();

            ViewBag.Quiz = quiz;
            return View(results);
        }

        // GET: /Quiz/Export/{id}
        public IActionResult Export(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var excelData = _exportService.ExportQuizResultsToExcel(id);
            if (excelData == null || excelData.Length == 0)
            {
                TempData["Error"] = "No data to export!";
                return RedirectToAction("Manage");
            }

            var quiz = _context.Quizzes.Find(id);
            var fileName = $"QuizResults_{quiz?.QuizTitle?.Replace(" ", "_")}_{DateTime.Now:yyyyMMdd}.xlsx";

            return File(excelData,
                       "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                       fileName);
        }

        // GET: /Quiz/Take/{id}
        public IActionResult Take(int id)
        {
            var user = _authService.GetCurrentUser();
            if (user == null || user.Role?.RoleName != "QuizTaker")
                return RedirectToAction("Login", "Account");

            var quiz = _context.Quizzes
                .Include(q => q.Questions!)
                .ThenInclude(q => q.Options!)
                .FirstOrDefault(q => q.QuizID == id && q.IsActive && q.IsPublished);

            if (quiz == null)
            {
                TempData["Error"] = "Quiz not found or not available!";
                return RedirectToAction("Dashboard", "Home");
            }

            // Check date range
            if (quiz.StartDate.HasValue && DateTime.Now < quiz.StartDate.Value)
            {
                TempData["Error"] = "Quiz has not started yet!";
                return RedirectToAction("Dashboard", "Home");
            }

            if (quiz.EndDate.HasValue && DateTime.Now > quiz.EndDate.Value)
            {
                TempData["Error"] = "Quiz has expired!";
                return RedirectToAction("Dashboard", "Home");
            }

            // Check for existing attempt
            var existingAttempt = _context.QuizAttempts
                .FirstOrDefault(a => a.QuizID == id && a.UserID == user.UserID && a.Status == "InProgress");

            if (existingAttempt != null)
            {
                ViewBag.AttemptId = existingAttempt.AttemptID;
                ViewBag.TimeLimit = quiz.TimeLimitMinutes;
                ViewBag.Questions = quiz.Questions?.OrderBy(q => q.SortOrder).ToList() ?? new List<Question>();
                return View("TakeQuiz", quiz);
            }

            // Start new attempt
            var attempt = new QuizAttempt
            {
                QuizID = id,
                UserID = user.UserID,
                StartTime = DateTime.Now,
                MaxScore = quiz.TotalMarks,
                Status = "InProgress"
            };

            _context.QuizAttempts.Add(attempt);
            _context.SaveChanges();

            ViewBag.AttemptId = attempt.AttemptID;
            ViewBag.TimeLimit = quiz.TimeLimitMinutes;
            ViewBag.Questions = quiz.Questions?.OrderBy(q => q.SortOrder).ToList() ?? new List<Question>();

            return View("TakeQuiz", quiz);
        }

        // POST: /Quiz/SubmitAnswer
        [HttpPost]
        public JsonResult SubmitAnswer(int attemptId, int questionId, int? optionId)
        {
            var attempt = _context.QuizAttempts
                .FirstOrDefault(a => a.AttemptID == attemptId && a.Status == "InProgress");

            if (attempt == null)
                return Json(new { success = false });

            var question = _context.Questions
                .Include(q => q.Options)
                .FirstOrDefault(q => q.QuestionID == questionId);

            if (question == null)
                return Json(new { success = false });

            var isCorrect = false;
            decimal marksObtained = 0;

            if (optionId.HasValue)
            {
                var selectedOption = question.Options?.FirstOrDefault(o => o.OptionID == optionId.Value);
                if (selectedOption != null)
                {
                    isCorrect = selectedOption.IsCorrect;
                    marksObtained = isCorrect ? question.Marks : 0;
                }
            }

            // Check if answer exists
            var existingAnswer = _context.UserAnswers
                .FirstOrDefault(a => a.AttemptID == attemptId && a.QuestionID == questionId);

            if (existingAnswer != null)
            {
                existingAnswer.SelectedOptionID = optionId;
                existingAnswer.IsCorrect = isCorrect;
                existingAnswer.MarksObtained = marksObtained;
                existingAnswer.AnsweredTime = DateTime.Now;
            }
            else
            {
                var answer = new UserAnswer
                {
                    AttemptID = attemptId,
                    QuestionID = questionId,
                    SelectedOptionID = optionId,
                    IsCorrect = isCorrect,
                    MarksObtained = marksObtained,
                    AnsweredTime = DateTime.Now
                };
                _context.UserAnswers.Add(answer);
            }

            _context.SaveChanges();
            return Json(new { success = true });
        }

        // POST: /Quiz/Finish
        [HttpPost]
        public IActionResult Finish(int attemptId)
        {
            var attempt = _context.QuizAttempts
                .Include(a => a.Answers!)
                .FirstOrDefault(a => a.AttemptID == attemptId);

            if (attempt == null || attempt.Status != "InProgress")
                return RedirectToAction("Dashboard", "Home");

            // Calculate score
            decimal totalScore = attempt.Answers?.Sum(a => a.MarksObtained) ?? 0;
            decimal percentage = attempt.MaxScore > 0 ? (totalScore / attempt.MaxScore) * 100 : 0;

            attempt.EndTime = DateTime.Now;

            if (attempt.EndTime.HasValue)
            {
                var timeTaken = attempt.EndTime.Value - attempt.StartTime;
                attempt.TimeTakenMinutes = (int)timeTaken.TotalMinutes;
            }

            attempt.Score = totalScore;
            attempt.Percentage = percentage;
            attempt.Status = "Completed";

            _context.SaveChanges();

            TempData["Success"] = $"Quiz completed! Your score: {attempt.Score}/{attempt.MaxScore} ({attempt.Percentage:0.00}%)";
            return RedirectToAction("Results", new { id = attemptId });
        }

        // GET: /Quiz/Results/{id}
        public IActionResult Results(int id)
        {
            var attempt = _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.Answers!)
                .ThenInclude(a => a.Question!)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(a => a.AttemptID == id);

            if (attempt == null)
                return NotFound();

            var user = _authService.GetCurrentUser();
            if (user == null || (attempt.UserID != user.UserID && !_authService.IsUploader() && !_authService.IsAdmin()))
                return RedirectToAction("Login", "Account");

            return View(attempt);
        }
    }
}