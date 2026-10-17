namespace ContentCartel.Models
{
    public class Service
    {
        public string ServiceId { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public decimal BasePrice { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}