using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentCartel.Models
{
    public class Booking
    {
        // ============================================================
        // IDENTIFIERS
        // ============================================================

        [Key]
        public int BookingId { get; set; }

        // Firebase/API booking ID
        public string Id { get; set; } = string.Empty;


        // ============================================================
        // SERVICE
        // ============================================================

        [Required]
        public string ServiceId { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string ServiceName { get; set; } = string.Empty;


        // ============================================================
        // CLIENT
        // ============================================================

        [Required]
        public string UserId { get; set; } = string.Empty;

        [ForeignKey(nameof(UserId))]
        public ApplicationUser? User { get; set; }

        [Required]
        [StringLength(150)]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Phone]
        public string? Phone { get; set; }


        // ============================================================
        // BOOKING DATE & TIME
        // ============================================================

        [Required]
        public DateTime BookingDate { get; set; }

        public string BookingTime { get; set; } = string.Empty;


        // ============================================================
        // PAYMENT
        // ============================================================

        [Column(TypeName = "decimal(18,2)")]
        public decimal TotalAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal DepositAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance { get; set; }


        // ============================================================
        // STATUS
        // ============================================================

        public string Status { get; set; } = "Pending";

        public string PaymentStatus { get; set; } = "Pending";


        // ============================================================
        // ADDITIONAL INFORMATION
        // ============================================================

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
    }
}