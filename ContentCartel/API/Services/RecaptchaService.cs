using System.Text.Json;

namespace ContentCartel.API.Services
{
    public class RecaptchaService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;
        private readonly ILogger<RecaptchaService> _logger;

        public RecaptchaService(
            IConfiguration configuration,
            IHttpClientFactory httpClientFactory,
            ILogger<RecaptchaService> logger)
        {
            _configuration = configuration;
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<bool> VerifyAsync(
            string token,
            string expectedAction)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogWarning("reCAPTCHA token is empty.");
                return false;
            }

            var secretKey = _configuration["Recaptcha:SecretKey"];

            if (string.IsNullOrWhiteSpace(secretKey))
            {
                _logger.LogError("Recaptcha:SecretKey is not configured.");
                return false;
            }

            var values = new Dictionary<string, string>
            {
                ["secret"] = secretKey,
                ["response"] = token
            };

            using var content = new FormUrlEncodedContent(values);

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.PostAsync(
                    "https://www.google.com/recaptcha/api/siteverify",
                    content);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to contact Google reCAPTCHA.");
                return false;
            }

            var json = await response.Content.ReadAsStringAsync();

            _logger.LogInformation(
                "reCAPTCHA response: {Response}",
                json);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "reCAPTCHA HTTP request failed: {StatusCode}",
                    response.StatusCode);

                return false;
            }

            RecaptchaResponse? result;

            try
            {
                result = JsonSerializer.Deserialize<RecaptchaResponse>(
                    json,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unable to parse reCAPTCHA response.");

                return false;
            }

            if (result == null)
            {
                _logger.LogWarning(
                    "reCAPTCHA returned an empty response.");

                return false;
            }

            _logger.LogInformation(
                "reCAPTCHA Success: {Success}, Score: {Score}, Action: {Action}, Hostname: {Hostname}",
                result.Success,
                result.Score,
                result.Action,
                result.Hostname);

            if (!result.Success)
            {
                _logger.LogWarning(
                    "reCAPTCHA verification failed. Error codes: {Errors}",
                    result.ErrorCodes == null
                        ? "none"
                        : string.Join(", ", result.ErrorCodes));

                return false;
            }

            if (!string.Equals(
                    result.Action,
                    expectedAction,
                    StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning(
                    "reCAPTCHA action mismatch. Expected: {ExpectedAction}, Actual: {ActualAction}",
                    expectedAction,
                    result.Action);

                return false;
            }

            var minimumScore =
                _configuration.GetValue<double?>(
                    "Recaptcha:MinimumScore")
                ?? 0.5;

            if (result.Score < minimumScore)
            {
                _logger.LogWarning(
                    "reCAPTCHA score too low. Score: {Score}, Required: {MinimumScore}",
                    result.Score,
                    minimumScore);

                return false;
            }

            return true;
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