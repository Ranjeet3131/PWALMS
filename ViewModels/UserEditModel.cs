using System.ComponentModel.DataAnnotations;

namespace PWALMS.ViewModels
{
    public class UserEditModel
    {
        public int UserID { get; set; }

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = "";

        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "New Password (leave blank to keep current)")]
        public string? NewPassword { get; set; }

        [Required]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        [Display(Name = "Department")]
        public int? DepartmentID { get; set; }

        [Required]
        [Display(Name = "Role")]
        public int RoleID { get; set; }

        [Display(Name = "Active")]
        public bool IsActive { get; set; } = true;
    }
}