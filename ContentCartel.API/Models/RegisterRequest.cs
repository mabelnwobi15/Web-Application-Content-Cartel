namespace ContentCartel.API.Models
{
    public class RegisterRequest
    {
        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Password { get; set; } = string.Empty;

        public string RecaptchaToken { get; set; } = string.Empty;
    }
}