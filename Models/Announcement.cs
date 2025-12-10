using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PWALMS.Models
{
    public class Announcement
    {
        [Key]
        public int AnnouncementID { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = "";

        [Required]
        public string Content { get; set; } = "";

        [ForeignKey("CreatedBy")]
        public int CreatedByUserID { get; set; }
        public virtual User? CreatedBy { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? ExpiryDate { get; set; }
        public bool IsActive { get; set; } = true;

        [StringLength(20)]
        public string Priority { get; set; } = "Normal";
    }
}