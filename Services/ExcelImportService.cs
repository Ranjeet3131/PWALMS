using ClosedXML.Excel;
using PWALMS.Models;
using PWALMS.Data;
using System.Data;

namespace PWALMS.Services
{
    public class ExcelImportService
    {
        private readonly ApplicationDbContext _context;

        public ExcelImportService(ApplicationDbContext context)
        {
            _context = context;
        }

        public (int imported, int errors) ImportQuestionsFromExcel(int quizId, Stream excelStream)
        {
            int importedCount = 0;
            int errorCount = 0;

            try
            {
                using (var workbook = new XLWorkbook(excelStream))
                {
                    var worksheet = workbook.Worksheet(1);
                    var rows = worksheet.RowsUsed().Skip(1); // Skip header row

                    foreach (var row in rows)
                    {
                        try
                        {
                            // Get values from Excel (1-based index)
                            var questionText = row.Cell(1).GetValue<string>()?.Trim();
                            var marks = row.Cell(2).GetValue<decimal>();
                            var option1 = row.Cell(3).GetValue<string>()?.Trim();
                            var option2 = row.Cell(4).GetValue<string>()?.Trim();
                            var option3 = row.Cell(5).GetValue<string>()?.Trim();
                            var option4 = row.Cell(6).GetValue<string>()?.Trim();
                            var correctOption = row.Cell(7).GetValue<int>();

                            // Validate required fields
                            if (string.IsNullOrEmpty(questionText) || marks <= 0)
                            {
                                errorCount++;
                                continue;
                            }

                            // Create question
                            var question = new Question
                            {
                                QuizID = quizId,
                                QuestionText = questionText,
                                QuestionType = "MCQ",
                                Marks = marks,
                                SortOrder = _context.Questions.Count(q => q.QuizID == quizId) + 1,
                                //CreatedDate = DateTime.Now
                            };

                            _context.Questions.Add(question);
                            _context.SaveChanges();

                            // Create options list
                            var options = new List<(string text, bool isCorrect)>
                            {
                                (option1, correctOption == 1),
                                (option2, correctOption == 2),
                                (option3, correctOption == 3),
                                (option4, correctOption == 4)
                            };

                            int optionOrder = 1;
                            foreach (var (text, isCorrect) in options)
                            {
                                if (!string.IsNullOrEmpty(text))
                                {
                                    var option = new Option
                                    {
                                        QuestionID = question.QuestionID,
                                        OptionText = text,
                                        IsCorrect = isCorrect,
                                        SortOrder = optionOrder++
                                    };
                                    _context.Options.Add(option);
                                }
                            }

                            // Update quiz totals
                            var quiz = _context.Quizzes.Find(quizId);
                            if (quiz != null)
                            {
                                quiz.TotalQuestions = _context.Questions.Count(q => q.QuizID == quizId);
                                quiz.TotalMarks = _context.Questions.Where(q => q.QuizID == quizId).Sum(q => q.Marks);
                            }

                            importedCount++;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error importing row {row.RowNumber()}: {ex.Message}");
                            errorCount++;
                        }
                    }

                    _context.SaveChanges();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Excel import failed: {ex.Message}");
            }

            return (importedCount, errorCount);
        }

        public byte[] GenerateTemplate()
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Template");

                // Header row
                worksheet.Cell(1, 1).Value = "Question Text";
                worksheet.Cell(1, 2).Value = "Marks";
                worksheet.Cell(1, 3).Value = "Option 1";
                worksheet.Cell(1, 4).Value = "Option 2";
                worksheet.Cell(1, 5).Value = "Option 3";
                worksheet.Cell(1, 6).Value = "Option 4";
                worksheet.Cell(1, 7).Value = "Correct Option (1-4)";

                // Style header
                var headerRange = worksheet.Range(1, 1, 1, 7);
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // Example data
                worksheet.Cell(2, 1).Value = "What is blood pressure?";
                worksheet.Cell(2, 2).Value = 1;
                worksheet.Cell(2, 3).Value = "120/80 mmHg";
                worksheet.Cell(2, 4).Value = "140/90 mmHg";
                worksheet.Cell(2, 5).Value = "100/60 mmHg";
                worksheet.Cell(2, 6).Value = "160/100 mmHg";
                worksheet.Cell(2, 7).Value = 1;

                worksheet.Cell(3, 1).Value = "Normal body temperature is?";
                worksheet.Cell(3, 2).Value = 1;
                worksheet.Cell(3, 3).Value = "36.5°C";
                worksheet.Cell(3, 4).Value = "37°C";
                worksheet.Cell(3, 5).Value = "37.5°C";
                worksheet.Cell(3, 6).Value = "38°C";
                worksheet.Cell(3, 7).Value = 2;

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