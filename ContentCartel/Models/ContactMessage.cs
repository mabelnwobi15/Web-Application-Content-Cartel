using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentCartel.Models
{
    [Table("ContactMessages")]
    public class ContactMessage
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required(ErrorMessage = "Please enter your name.")]
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters.")]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your email address.")]
        [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
        [StringLength(150, ErrorMessage = "Email cannot exceed 150 characters.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter your WhatsApp or phone number.")]
        [Phone(ErrorMessage = "Please enter a valid phone number.")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters.")]
        [Display(Name = "Phone / WhatsApp")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a service interest.")]
        [StringLength(100)]
        [Display(Name = "Service of Interest")]
        public string ServiceInterest { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please enter a message.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Message must be between 10 and 1000 characters.")]
        public string Message { get; set; } = string.Empty;

        [Display(Name = "Date Submitted")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [Display(Name = "Is Read")]
        public bool IsRead { get; set; } = false;
    }
}