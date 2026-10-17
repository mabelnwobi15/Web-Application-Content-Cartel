namespace ContentCartel.API.Models
{
    public class UserProfile
    {
        public string Uid { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Phone { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            "Client";

        public bool BiometricEnabled { get; set; }

        public string Status { get; set; } =
            "Active";

        public DateTime CreatedAt { get; set; } =
            DateTime.UtcNow;
    }
}