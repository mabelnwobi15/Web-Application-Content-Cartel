namespace ContentCartel.API.Models
{
    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;

        public string RecaptchaToken { get; set; } = string.Empty;
    }
}