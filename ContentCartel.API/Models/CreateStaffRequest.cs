namespace ContentCartel.API.Models
{
    public class CreateStaffRequest
    {
        public string FullName { get; set; } =
            string.Empty;

        public string Email { get; set; } =
            string.Empty;

        public string Phone { get; set; } =
            string.Empty;

        public string Password { get; set; } =
            string.Empty;

        public string Role { get; set; } =
            "Staff";

        public bool BiometricEnabled { get; set; }
    }
}