using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentCartel.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int InvoiceId { get; set; }

        [ForeignKey(nameof(InvoiceId))]
        public Invoice? Invoice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Unknown";

        public string Status { get; set; } = "Pending";

        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

        public string? Reference { get; set; }
    }
}