using System.ComponentModel.DataAnnotations;

namespace PWALMS.ViewModels
{
    public class QuestionCreateModel
    {
        [Required]
        public int QuizID { get; set; }

        [Required]
        public string QuestionText { get; set; } = "";

        [Required]
        public string QuestionType { get; set; } = "MCQ";

        [Required]
        [Range(0.1, 10)]
        public decimal Marks { get; set; } = 1;

        public string Option1 { get; set; } = "";
        public string Option2 { get; set; } = "";
        public string Option3 { get; set; } = "";
        public string Option4 { get; set; } = "";

        [Required]
        [Range(1, 4)]
        [Display(Name = "Correct Option")]
        public int CorrectOption { get; set; } = 1;
    }
}