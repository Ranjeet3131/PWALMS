using System.ComponentModel.DataAnnotations;

namespace PWALMS.Models
{
    public class QuizCategory
    {
        [Key]
        public int CategoryID { get; set; }

        [Required]
        [StringLength(100)]
        public string CategoryName { get; set; } = "";

        public string? Description { get; set; }
    }
}