using System.Text.Json;

namespace ContentCartel.API.Services
{
    public class RecaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public RecaptchaService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;

            _httpClient =
                httpClientFactory.CreateClient();
        }

        public async Task<bool> VerifyAsync(
            string token,
            string expectedAction)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var secretKey =
                _configuration["Recaptcha:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return false;
            }

            var values =
                new Dictionary<string, string>
                {
                    ["secret"] = secretKey,
                    ["response"] = token
                };

            using var content =
                new FormUrlEncodedContent(values);

            var response =
                await _httpClient.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify",
                    content
                );

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var json =
                await response.Content.ReadAsStringAsync();

            var result =
                JsonSerializer.Deserialize<RecaptchaResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );

            if (result == null)
            {
                return false;
            }

            if (!result.Success)
            {
                return false;
            }

            if (!string.Equals(
                    result.Action,
                    expectedAction,
                    StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return result.Score >= 0.5;
        }
    }

    public class RecaptchaResponse
    {
        public bool Success { get; set; }

        public double Score { get; set; }

        public string? Action { get; set; }

        public string? Hostname { get; set; }

        public string[]? ErrorCodes { get; set; }
    }
}