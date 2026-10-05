namespace ContentCartel.Models
{
    public class QuoteRequest
    {
        public string Id { get; set; } = string.Empty;

        public string BrandName { get; set; } = string.Empty;

        public string SocialMediaHandle { get; set; } = string.Empty;

        public string ContactPersonName { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string MediaType { get; set; } = string.Empty;

        public decimal EstimatedPrice { get; set; }

        public string Budget { get; set; } = string.Empty;

        public string CampaignGoals { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}