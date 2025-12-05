using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PWALMS.Models
{
    public class Quiz
    {
        [Key]
        public int QuizID { get; set; }

        [StringLength(20)]
        public string QuizCode { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string QuizTitle { get; set; } = "";

        public string Description { get; set; } = "";

        [ForeignKey("Department")]
        public int DepartmentID { get; set; }
        public virtual Department Department { get; set; }

        [ForeignKey("CreatedByUser")]
        public int CreatedBy { get; set; }
        public virtual User CreatedByUser { get; set; }

        [Required]
        [Range(1, 300)]
        public int TimeLimitMinutes { get; set; } = 30;

        public int TotalQuestions { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalMarks { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public bool IsPublished { get; set; } = false;

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual ICollection<Question> Questions { get; set; }
        public virtual ICollection<QuizAttempt> Attempts { get; set; }
    }

    public class Question
    {
        [Key]
        public int QuestionID { get; set; }

        [ForeignKey("Quiz")]
        public int QuizID { get; set; }
        public virtual Quiz Quiz { get; set; }

        [Required]
        public string QuestionText { get; set; } = "";

        [StringLength(20)]
        public string QuestionType { get; set; } = "MCQ";

        [Column(TypeName = "decimal(10,2)")]
        [Range(0.1, 10)]
        public decimal Marks { get; set; } = 1;

        public int SortOrder { get; set; } = 0;

        // Navigation property
        public virtual ICollection<Option> Options { get; set; }
    }

    public class Option
    {
        [Key]
        public int OptionID { get; set; }

        [ForeignKey("Question")]
        public int QuestionID { get; set; }
        public virtual Question Question { get; set; }

        [Required]
        public string OptionText { get; set; } = "";

        public bool IsCorrect { get; set; } = false;
        public int SortOrder { get; set; } = 0;
    }

    public class QuizAttempt
    {
        [Key]
        public int AttemptID { get; set; }

        [ForeignKey("Quiz")]
        public int QuizID { get; set; }
        public virtual Quiz Quiz { get; set; }

        [ForeignKey("User")]
        public int UserID { get; set; }
        public virtual User User { get; set; }

        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime? EndTime { get; set; }
        public int? TimeTakenMinutes { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Score { get; set; } = 0;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MaxScore { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Percentage { get; set; }

        [StringLength(20)]
        public string Status { get; set; } = "InProgress";

        // Navigation property
        public virtual ICollection<UserAnswer> Answers { get; set; }
    }

    public class UserAnswer
    {
        [Key]
        public int AnswerID { get; set; }

        [ForeignKey("QuizAttempt")]
        public int AttemptID { get; set; }
        public virtual QuizAttempt QuizAttempt { get; set; }

        [ForeignKey("Question")]
        public int QuestionID { get; set; }
        public virtual Question Question { get; set; }

        [ForeignKey("SelectedOption")]
        public int? SelectedOptionID { get; set; }
        public virtual Option SelectedOption { get; set; }

        public bool IsCorrect { get; set; } = false;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MarksObtained { get; set; } = 0;

        public DateTime AnsweredTime { get; set; } = DateTime.Now;
    }
}