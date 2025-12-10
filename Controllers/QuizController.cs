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
        // Update the Create method in QuizController to include categories
        public IActionResult Create()
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            ViewBag.Departments = _context.Departments.ToList();
            ViewBag.Categories = _context.QuizCategories.ToList(); // Add this line
            return View();
        }

        // Update the POST Create method
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
                    CategoryID = model.CategoryID, // Add this
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
            ViewBag.Categories = _context.QuizCategories.ToList(); // Add this
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

        // GET: /Quiz/Categories
        public IActionResult Categories()
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var categories = _context.QuizCategories.ToList();

            // Get quiz counts per category
            var quizCounts = new Dictionary<int, int>();
            foreach (var category in categories)
            {
                quizCounts[category.CategoryID] = _context.Quizzes
                    .Count(q => q.CategoryID == category.CategoryID);
            }

            ViewBag.QuizCounts = quizCounts;
            return View(categories);
        }

        // GET: /Quiz/CreateCategory
        public IActionResult CreateCategory()
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            return View();
        }

        // POST: /Quiz/CreateCategory
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateCategory(string categoryName, string description)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            if (string.IsNullOrEmpty(categoryName))
            {
                TempData["Error"] = "Category name is required!";
                return View();
            }

            var category = new QuizCategory
            {
                CategoryName = categoryName,
                Description = description
            };

            _context.QuizCategories.Add(category);
            _context.SaveChanges();

            TempData["Success"] = $"Category '{categoryName}' created successfully!";
            return RedirectToAction("Categories");
        }

        // GET: /Quiz/DeleteCategory/{id}
        public IActionResult DeleteCategory(int id)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var category = _context.QuizCategories.Find(id);
            if (category == null)
                return NotFound();

            // Check if any quizzes use this category
            var quizCount = _context.Quizzes.Count(q => q.CategoryID == id);
            ViewBag.QuizCount = quizCount;

            return View(category);
        }

        // POST: /Quiz/DeleteCategoryConfirmed/{id}
        [HttpPost, ActionName("DeleteCategoryConfirmed")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteCategoryConfirmed(int id)
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var category = _context.QuizCategories.Find(id);
            if (category == null)
                return NotFound();

            // Check if any quizzes use this category
            var quizCount = _context.Quizzes.Count(q => q.CategoryID == id);
            if (quizCount > 0)
            {
                TempData["Error"] = $"Cannot delete category! {quizCount} quiz(zes) are using this category.";
                return RedirectToAction("Categories");
            }

            _context.QuizCategories.Remove(category);
            _context.SaveChanges();

            TempData["Success"] = $"Category '{category.CategoryName}' deleted successfully!";
            return RedirectToAction("Categories");
        }

        // GET: /Quiz/Analytics/{id}
        public IActionResult Analytics(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var quiz = _context.Quizzes
                .Include(q => q.Department)
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(q => q.QuizID == id);

            if (quiz == null)
                return NotFound();

            var attempts = _context.QuizAttempts
                .Include(a => a.User)
                .ThenInclude(u => u.Department)
                .Where(a => a.QuizID == id && a.Status == "Completed")
                .ToList();

            // Calculate statistics
            var stats = new QuizStatistics
            {
                TotalAttempts = attempts.Count,
                AverageScore = attempts.Any() ? attempts.Average(a => a.Score) : 0,
                AveragePercentage = attempts.Any() ? attempts.Average(a => a.Percentage) : 0,
                HighestScore = attempts.Any() ? attempts.Max(a => a.Score) : 0,
                LowestScore = attempts.Any() ? attempts.Min(a => a.Score) : 0,
                AverageTime = attempts.Any() ? attempts.Average(a => a.TimeTakenMinutes ?? 0) : 0
            };

            // Question-wise statistics
            var questionStats = new List<QuestionStat>();
            foreach (var question in quiz.Questions ?? new List<Question>())
            {
                var correctCount = 0;
                var totalAnswers = 0;

                foreach (var attempt in attempts)
                {
                    var answer = _context.UserAnswers
                        .FirstOrDefault(a => a.AttemptID == attempt.AttemptID && a.QuestionID == question.QuestionID);

                    if (answer != null)
                    {
                        totalAnswers++;
                        if (answer.IsCorrect) correctCount++;
                    }
                }

                questionStats.Add(new QuestionStat
                {
                    QuestionText = question.QuestionText,
                    CorrectAnswers = correctCount,
                    TotalAnswers = totalAnswers,
                    SuccessRate = totalAnswers > 0 ? (correctCount * 100.0 / totalAnswers) : 0
                });
            }

            ViewBag.Quiz = quiz;
            ViewBag.Stats = stats;
            ViewBag.QuestionStats = questionStats;
            ViewBag.Attempts = attempts;

            return View();
        }

        // GET: /Quiz/DepartmentReport
        public IActionResult DepartmentReport()
        {
            if (!_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            var report = _context.Departments
                .Select(d => new DepartmentReport
                {
                    DepartmentName = d.DepartmentName,
                    TotalUsers = _context.Users.Count(u => u.DepartmentID == d.DepartmentID),
                    TotalQuizzes = _context.Quizzes.Count(q => q.DepartmentID == d.DepartmentID),
                    TotalAttempts = _context.QuizAttempts.Count(a => a.User.DepartmentID == d.DepartmentID),
                    AverageScore = _context.QuizAttempts
                        .Where(a => a.User.DepartmentID == d.DepartmentID && a.Status == "Completed")
                        .Average(a => (double?)a.Percentage) ?? 0
                })
                .OrderByDescending(r => r.AverageScore)
                .ToList();

            return View(report);
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
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(q => q.QuizID == id && q.IsActive && q.IsPublished);

            if (quiz == null)
            {
                TempData["Error"] = "Quiz not found or not available!";
                return RedirectToAction("Dashboard", "Home");
            }

            // Check if user already completed this quiz
            var existingCompletedAttempt = _context.QuizAttempts
                .FirstOrDefault(a => a.QuizID == id && a.UserID == user.UserID && a.Status == "Completed");

            if (existingCompletedAttempt != null)
            {
                TempData["Error"] = "You have already completed this quiz! You can only take it once.";
                return RedirectToAction("Dashboard", "Home");
            }

            // Check for existing attempt in progress
            var existingInProgressAttempt = _context.QuizAttempts
                .FirstOrDefault(a => a.QuizID == id && a.UserID == user.UserID && a.Status == "InProgress");

            if (existingInProgressAttempt != null)
            {
                ViewBag.AttemptId = existingInProgressAttempt.AttemptID;
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
        public JsonResult SubmitAnswer([FromBody] SubmitAnswerRequest request)
        {
            try
            {
                Console.WriteLine("=== SUBMIT ANSWER ===");
                Console.WriteLine($"AttemptId: {request?.AttemptId}, QuestionId: {request?.QuestionId}, OptionId: {request?.OptionId}");

                if (request == null || request.AttemptId <= 0 || request.QuestionId <= 0)
                {
                    return Json(new { success = false, message = "Invalid request" });
                }

                // Check attempt
                var attempt = _context.QuizAttempts.Find(request.AttemptId);
                if (attempt == null || attempt.Status != "InProgress")
                {
                    return Json(new { success = false, message = "Attempt not found or not in progress" });
                }

                // Check question
                var question = _context.Questions.Find(request.QuestionId);
                if (question == null)
                {
                    return Json(new { success = false, message = "Question not found" });
                }

                // Check if option is correct
                bool isCorrect = false;
                if (request.OptionId.HasValue)
                {
                    var option = _context.Options
                        .FirstOrDefault(o => o.OptionID == request.OptionId.Value && o.QuestionID == request.QuestionId);
                    isCorrect = option?.IsCorrect ?? false;
                }

                // Save answer
                var answer = new UserAnswer
                {
                    AttemptID = request.AttemptId,
                    QuestionID = request.QuestionId,
                    SelectedOptionID = request.OptionId,
                    IsCorrect = isCorrect,
                    MarksObtained = isCorrect ? question.Marks : 0,
                    AnsweredTime = DateTime.Now
                };

                _context.UserAnswers.Add(answer);
                _context.SaveChanges();

                Console.WriteLine($"Answer saved. IsCorrect: {isCorrect}");
                return Json(new
                {
                    success = true,
                    isCorrect = isCorrect,
                    marks = isCorrect ? question.Marks : 0
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex.Message}");
                return Json(new { success = false, message = ex.Message });
            }
        }

        public class SubmitAnswerRequest
        {
            public int AttemptId { get; set; }
            public int QuestionId { get; set; }
            public int? OptionId { get; set; }
        }

        // POST: /Quiz/Finish
        [HttpPost]
        public IActionResult Finish(int attemptId)
        {
            try
            {
                var attempt = _context.QuizAttempts
                    .Include(a => a.Quiz)
                    .FirstOrDefault(a => a.AttemptID == attemptId);

                if (attempt == null || attempt.Status != "InProgress")
                    return RedirectToAction("Dashboard", "Home");

                // Get all answers for this attempt with correct answers checked
                var answers = _context.UserAnswers
                    .Where(a => a.AttemptID == attemptId)
                    .Include(a => a.Question)
                    .ThenInclude(q => q.Options)
                    .ToList();

                decimal totalScore = 0;
                decimal maxScore = attempt.Quiz?.TotalMarks ?? 0;

                // Re-calculate scores for each answer
                foreach (var answer in answers)
                {
                    if (answer.SelectedOptionID.HasValue)
                    {
                        // Find the selected option
                        var selectedOption = answer.Question?.Options?
                            .FirstOrDefault(o => o.OptionID == answer.SelectedOptionID.Value);

                        if (selectedOption != null)
                        {
                            // Check if it's correct
                            bool isCorrect = selectedOption.IsCorrect;
                            decimal marks = isCorrect ? (answer.Question?.Marks ?? 0) : 0;

                            // Update the answer record
                            answer.IsCorrect = isCorrect;
                            answer.MarksObtained = marks;

                            // Add to total score
                            totalScore += marks;
                        }
                        else
                        {
                            answer.IsCorrect = false;
                            answer.MarksObtained = 0;
                        }
                    }
                    else
                    {
                        answer.IsCorrect = false;
                        answer.MarksObtained = 0;
                    }
                }

                // Save updated answers
                _context.UserAnswers.UpdateRange(answers);

                // Calculate percentage
                decimal percentage = maxScore > 0 ? (totalScore / maxScore) * 100 : 0;

                attempt.EndTime = DateTime.Now;
                attempt.TimeTakenMinutes = (int)(attempt.EndTime.Value - attempt.StartTime).TotalMinutes;
                attempt.Score = totalScore;
                attempt.Percentage = percentage;
                attempt.Status = "Completed";

                _context.SaveChanges();

                TempData["Success"] = $"Quiz completed! Your score: {attempt.Score}/{maxScore} ({attempt.Percentage:0.00}%)";
                return RedirectToAction("Results", new { id = attemptId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error submitting quiz: {ex.Message}";
                return RedirectToAction("Dashboard", "Home");
            }
        }

        // GET: /Quiz/TestScoring/{quizId}
        public IActionResult TestScoring(int quizId)
        {
            var quiz = _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(q => q.QuizID == quizId);

            if (quiz == null)
                return NotFound();

            ViewBag.Quiz = quiz;

            // Check correct answers
            var questionsWithCorrect = quiz.Questions?.Select(q => new
            {
                Question = q.QuestionText,
                CorrectOption = q.Options?.FirstOrDefault(o => o.IsCorrect)?.OptionText ?? "No correct option set!",
                Marks = q.Marks
            }).ToList();

            ViewBag.QuestionsWithCorrect = questionsWithCorrect;

            return View();
        }

        // GET: /Quiz/Results/{id}
        public IActionResult Results(int id)
        {
            var attempt = _context.QuizAttempts
                .Include(a => a.Quiz)
                .Include(a => a.Answers)
                .ThenInclude(a => a.Question)
                .ThenInclude(q => q.Options)
                .FirstOrDefault(a => a.AttemptID == id);

            if (attempt == null)
                return NotFound();

            var user = _authService.GetCurrentUser();
            if (user == null)
                return RedirectToAction("Login", "Account");

            // Check permissions
            bool canView = false;

            // Uploader/Admin can view any result
            if (user.Role?.RoleName == "Uploader" || user.Role?.RoleName == "Admin")
            {
                canView = true;
            }
            // QuizTaker can only view their own results
            else if (user.Role?.RoleName == "QuizTaker" && attempt.UserID == user.UserID)
            {
                canView = true;
            }

            if (!canView)
            {
                TempData["Error"] = "You don't have permission to view these results.";
                return RedirectToAction("Dashboard", "Home");
            }

            ViewBag.IsQuizTaker = (user.Role?.RoleName == "QuizTaker");
            return View(attempt);
        }

        // POST: /Quiz/DeleteSingle - For single quiz deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteSingle(int id)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            try
            {
                var quiz = _context.Quizzes.Find(id);
                if (quiz == null)
                {
                    TempData["Error"] = "Quiz not found!";
                    return RedirectToAction("Manage");
                }

                string quizTitle = quiz.QuizTitle;

                // Delete in correct order
                // 1. Get all attempts for this quiz
                var attempts = _context.QuizAttempts.Where(a => a.QuizID == id).ToList();

                // 2. Delete all user answers for these attempts
                foreach (var attempt in attempts)
                {
                    var answers = _context.UserAnswers.Where(a => a.AttemptID == attempt.AttemptID).ToList();
                    _context.UserAnswers.RemoveRange(answers);
                }

                // 3. Delete all quiz attempts
                _context.QuizAttempts.RemoveRange(attempts);

                // 4. Get all questions for this quiz
                var questions = _context.Questions.Where(q => q.QuizID == id).ToList();

                // 5. Delete all options for these questions
                foreach (var question in questions)
                {
                    var options = _context.Options.Where(o => o.QuestionID == question.QuestionID).ToList();
                    _context.Options.RemoveRange(options);
                }

                // 6. Delete all questions
                _context.Questions.RemoveRange(questions);

                // 7. Finally delete the quiz
                _context.Quizzes.Remove(quiz);

                _context.SaveChanges();

                TempData["Success"] = $"Quiz '{quizTitle}' deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting quiz: {ex.Message}";
            }

            return RedirectToAction("Manage");
        }

        // POST: /Quiz/DeleteMultiple - For bulk quiz deletion
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteMultiple(List<int> selectedQuizIds)
        {
            if (!_authService.IsUploader() && !_authService.IsAdmin())
                return RedirectToAction("Login", "Account");

            if (selectedQuizIds == null || selectedQuizIds.Count == 0)
            {
                TempData["Error"] = "No quizzes selected for deletion.";
                return RedirectToAction("Manage");
            }

            try
            {
                int deletedCount = 0;

                foreach (var quizId in selectedQuizIds)
                {
                    var quiz = _context.Quizzes.Find(quizId);
                    if (quiz == null) continue;

                    // Delete in correct order
                    // 1. Get all attempts for this quiz
                    var attempts = _context.QuizAttempts.Where(a => a.QuizID == quizId).ToList();

                    // 2. Delete all user answers for these attempts
                    foreach (var attempt in attempts)
                    {
                        var answers = _context.UserAnswers.Where(a => a.AttemptID == attempt.AttemptID).ToList();
                        _context.UserAnswers.RemoveRange(answers);
                    }

                    // 3. Delete all quiz attempts
                    _context.QuizAttempts.RemoveRange(attempts);

                    // 4. Get all questions for this quiz
                    var questions = _context.Questions.Where(q => q.QuizID == quizId).ToList();

                    // 5. Delete all options for these questions
                    foreach (var question in questions)
                    {
                        var options = _context.Options.Where(o => o.QuestionID == question.QuestionID).ToList();
                        _context.Options.RemoveRange(options);
                    }

                    // 6. Delete all questions
                    _context.Questions.RemoveRange(questions);

                    // 7. Delete the quiz
                    _context.Quizzes.Remove(quiz);

                    deletedCount++;
                }

                _context.SaveChanges();

                if (deletedCount > 0)
                {
                    TempData["Success"] = $"Successfully deleted {deletedCount} quiz(zes)!";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting quizzes: {ex.Message}";
            }

            return RedirectToAction("Manage");
        }
    }
    }
