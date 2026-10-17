using System.ComponentModel.DataAnnotations;

namespace ContentCartel.Models.ViewModels
{
    public class AddStaffViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } =
            string.Empty;

        [Required]
        [EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; } =
            string.Empty;

        [Display(Name = "Phone Number")]
        public string Phone { get; set; } =
            string.Empty;

        [Required]
        [Display(Name = "Role")]
        public string Role { get; set; } =
            "Staff";

        [Required]
        [MinLength(6)]
        [Display(Name = "Password")]
        public string Password { get; set; } =
            string.Empty;

        [Display(Name = "Enable Biometric Login")]
        public bool BiometricEnabled { get; set; }
    }
}