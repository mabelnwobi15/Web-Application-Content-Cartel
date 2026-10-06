using System.Text.Json.Serialization;

namespace ContentCartel.Models
{
    /// <summary>
    /// MVC-side quote model. Every property is optional at binding level so
    /// ASP.NET Core does not silently reject the form for fields the form
    /// never posts (Email, ServiceName, Status, UserId ...). The controller
    /// validates the genuinely required fields itself.
    /// </summary>
    public class QuoteRequest
    {
        // Firebase quote ID (string GUID) returned by the API
        public string? Id { get; set; }

        // Used by Views/Quote/QuoteSubmitted.cshtml ("#@Model.QuoteRequestId")
        [JsonIgnore]
        public string? QuoteRequestId => Id;

        public string? UserId { get; set; }

        // ----- form fields -----
        public string? BrandName { get; set; }
        public string? SocialMediaHandle { get; set; }

        // "Contact Person Name" input on the form (sent to the API as ContactPersonName)
        public string? CustomerName { get; set; }

        public string? Phone { get; set; }
        public string? MediaType { get; set; }
        public string? Budget { get; set; }
        public string? CampaignGoals { get; set; }
        public decimal? EstimatedAmount { get; set; }

        // ----- shown on the confirmation page / filled from the API -----
        public string? Email { get; set; }
        public string? ServiceName { get; set; }
        public string? Description { get; set; }

        // Names used by the API model, so API lists deserialise cleanly
        public string? ContactPersonName { get; set; }
        public decimal EstimatedPrice { get; set; }

        public string Status { get; set; } = "Pending";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}