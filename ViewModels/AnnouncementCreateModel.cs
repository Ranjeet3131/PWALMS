using System.ComponentModel.DataAnnotations;

namespace PWALMS.ViewModels
{
    public class AnnouncementCreateModel
    {
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Content { get; set; } = "";

        [Display(Name = "Expiry Date (optional)")]
        public DateTime? ExpiryDate { get; set; }

        [Required]
        [Display(Name = "Priority")]
        public string Priority { get; set; } = "Normal";
    }
}