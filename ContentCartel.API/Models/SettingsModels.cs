namespace ContentCartel.API.Models
{
    public class UpdateProfileRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; }

        // Only changed when sent (admin security setting).
        public bool? BiometricEnabled { get; set; }
    }

    public class UserPreferences
    {
        public string Language { get; set; } = "en-ZA";
        public string TimeZone { get; set; } = "Africa/Johannesburg";
        public string DateFormat { get; set; } = "dd MMM yyyy";

        public bool NotifyBookings { get; set; } = true;
        public bool NotifyQuotes { get; set; } = true;
        public bool NotifyPayments { get; set; } = true;
        public bool WeeklySummary { get; set; } = false;

        public string TextSize { get; set; } = "normal"; // small, normal, large, xlarge
        public bool HighContrast { get; set; } = false;
        public bool ReduceMotion { get; set; } = false;
    }

    public class BusinessSettings
    {
        public string BusinessName { get; set; } = "Content Cartel";
        public string SupportEmail { get; set; } = string.Empty;
        public string SupportPhone { get; set; } = string.Empty;
        public string WebsiteUrl { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }
}