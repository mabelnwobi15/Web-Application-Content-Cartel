using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentCartel.Models
{
    public class DiscountRecommendation
    {
        [Key]
        public int RecommendationId { get; set; }

        [Required]
        public string ClientUserId { get; set; } = string.Empty;

        [ForeignKey("ClientUserId")]
        public ApplicationUser? ClientUser { get; set; }

        [Range(1, 100)]
        public int SuggestedDiscountPercent { get; set; }

        public int FinalApprovedPercent { get; set; }

        public int ConfidenceScore { get; set; } // like 94 for 94%

        public string SignalTrigger { get; set; } = string.Empty; //  "Seasonal Re-engagement", "Inactivity Risk"

        public string AIReasoningSummary { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending"; // Pending, Applied, Rejected

        public string? ApprovedByAdminId { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        public DateTime? DecidedAt { get; set; }
    }
}