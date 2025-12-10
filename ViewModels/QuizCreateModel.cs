using System.ComponentModel.DataAnnotations;

namespace PWALMS.ViewModels
{
    public class QuizCreateModel
    {
        [Required]
        [StringLength(200)]
        public string QuizTitle { get; set; } = "";

        public string Description { get; set; } = "";

        [Required]
        [Display(Name = "Department")]
        public int DepartmentID { get; set; }

        // For multi-select UI (not saved to database yet)
        public List<int> SelectedDepartmentIds { get; set; } = new List<int>();

        [Display(Name = "Category (optional)")]
        public int? CategoryID { get; set; }

        [Required]
        [Range(1, 300)]
        [Display(Name = "Time Limit (minutes)")]
        public int TimeLimitMinutes { get; set; } = 30;

        [Display(Name = "Start Date (optional)")]
        public DateTime? StartDate { get; set; }

        [Display(Name = "End Date (optional)")]
        public DateTime? EndDate { get; set; }
    }
}