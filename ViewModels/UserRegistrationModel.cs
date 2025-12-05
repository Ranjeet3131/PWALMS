using System.ComponentModel.DataAnnotations;

namespace PWALMS.ViewModels
{
    public class UserRegistrationModel
    {
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = "";

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = "";

        [EmailAddress]
        public string Email { get; set; } = "";

        [Required]
        public int DepartmentID { get; set; }

        [Required]
        public int RoleID { get; set; }
    }
}