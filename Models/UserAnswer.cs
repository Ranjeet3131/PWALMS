using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PWALMS.Models
{
    public class UserAnswer
    {
        [Key]
        public int AnswerID { get; set; }

        [ForeignKey("QuizAttempt")]
        public int AttemptID { get; set; }
        public virtual QuizAttempt? QuizAttempt { get; set; }

        [ForeignKey("Question")]
        public int QuestionID { get; set; }
        public virtual Question? Question { get; set; }

        [ForeignKey("SelectedOption")]
        public int? SelectedOptionID { get; set; }
        public virtual Option? SelectedOption { get; set; }

        public bool IsCorrect { get; set; } = false;

        [Column(TypeName = "decimal(10,2)")]
        public decimal MarksObtained { get; set; } = 0;

        public DateTime AnsweredTime { get; set; } = DateTime.Now;
    }
}