namespace PWALMS.ViewModels
{
    public class QuizStatistics
    {
        public int TotalAttempts { get; set; }
        public decimal AverageScore { get; set; }
        public decimal AveragePercentage { get; set; }
        public decimal HighestScore { get; set; }
        public decimal LowestScore { get; set; }
        public double AverageTime { get; set; }
    }

    public class QuestionStat
    {
        public string QuestionText { get; set; } = "";
        public int CorrectAnswers { get; set; }
        public int TotalAnswers { get; set; }
        public double SuccessRate { get; set; }
    }

    public class DepartmentReport
    {
        public string DepartmentName { get; set; } = "";
        public int TotalUsers { get; set; }
        public int TotalQuizzes { get; set; }
        public int TotalAttempts { get; set; }
        public double AverageScore { get; set; }
    }
}