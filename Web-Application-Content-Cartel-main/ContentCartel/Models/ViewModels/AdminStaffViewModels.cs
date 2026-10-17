namespace ContentCartel.Models.ViewModels
{
    public class AdminStaffViewModel
    {
        public string Id { get; set; } =
            string.Empty;

        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            string.Empty;

        public string Status { get; set; } =
            "Active";

        public bool BiometricRegistered { get; set; }
    }
}