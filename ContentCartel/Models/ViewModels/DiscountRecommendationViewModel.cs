namespace ContentCartel.Models.ViewModels
{
    public class DiscountRecommendationViewModel
    {
        public int RecommendationId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public decimal LifetimeValue { get; set; }
        public int CompletedBookingsCount { get; set; }
        public string SignalTrigger { get; set; } = string.Empty;
        public int ConfidenceScore { get; set; }
        public int SuggestedDiscountPercent { get; set; }
        public string PaymentHistoryStatus { get; set; } = "100% On-Time";
    }
}
