using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ContentCartel.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        public bool IsBiometricEnrolled { get; set; } = false;

        public string? BiometricPublicKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public ICollection<Booking> Bookings { get; set; } = new List<Booking>();
        public ICollection<DiscountRecommendation> DiscountRecommendations { get; set; } = new List<DiscountRecommendation>();
    }
}