using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PWALMS.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = "";

        [Required]
        public string PasswordHash { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [EmailAddress]
        [StringLength(100)]
        public string Email { get; set; } = "";

        [ForeignKey("Department")]
        public int? DepartmentID { get; set; }
        public virtual Department Department { get; set; }

        [ForeignKey("Role")]
        public int RoleID { get; set; }
        public virtual Role Role { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedDate { get; set; } = DateTime.Now;
        public DateTime? LastLogin { get; set; }

        // Navigation properties
        public virtual ICollection<Quiz> CreatedQuizzes { get; set; }
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; }
    }

    public class Role
    {
        [Key]
        public int RoleID { get; set; }

        [Required]
        [StringLength(50)]
        public string RoleName { get; set; } = "";

        [StringLength(200)]
        public string Description { get; set; } = "";
    }

    public class Department
    {
        [Key]
        public int DepartmentID { get; set; }

        [Required]
        [StringLength(100)]
        public string DepartmentName { get; set; } = "";

        [StringLength(10)]
        public string DepartmentCode { get; set; } = "";
    }
}