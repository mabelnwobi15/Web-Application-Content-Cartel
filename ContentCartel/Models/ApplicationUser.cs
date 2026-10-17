namespace ContentCartel.Models
{
    public class ApplicationUser
    {
        public string Id { get; set; } = string.Empty;

        public string Uid { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = "Client";

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}