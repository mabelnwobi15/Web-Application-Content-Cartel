using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace ContentCartel.API.Services
{
    public class FirebaseService
    {
        private readonly string _databaseUrl;
        private readonly string _serviceAccountJson;

        public FirebaseService(IConfiguration configuration)
        {
            _databaseUrl =
                configuration["Firebase:DatabaseUrl"]
                ?? "https://contentcartel-62294-default-rtdb.firebaseio.com";

            _serviceAccountJson =
                configuration["Firebase:ServiceAccountJson"]
                ?? throw new InvalidOperationException(
                    "Firebase:ServiceAccountJson is not configured."
                );
        }

        // ============================================================
        // FIREBASE ACCESS TOKEN
        // ============================================================

        private async Task<string> GetAccessTokenAsync()
        {
            var credential =
                GoogleCredential
                    .FromJson(_serviceAccountJson)
                    .CreateScoped(
                        "https://www.googleapis.com/auth/userinfo.email",
                        "https://www.googleapis.com/auth/firebase.database"
                    );

            return await credential
                .UnderlyingCredential
                .GetAccessTokenForRequestAsync();
        }

        // ============================================================
        // CREATE
        // ============================================================

        public async Task<string> CreateAsync<T>(
            string collection,
            T data)
        {
            var token =
                await GetAccessTokenAsync();

            using var client =
                new HttpClient();

            var json =
                JsonSerializer.Serialize(data);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            var url =
                $"{_databaseUrl}/{collection}.json";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Post,
                    url
                );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    token
                );

            request.Content = content;

            var response =
                await client.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Firebase CREATE failed: " +
                    $"{(int)response.StatusCode} " +
                    $"{response.StatusCode}: " +
                    responseBody
                );
            }

            using var document =
                JsonDocument.Parse(responseBody);

            return document.RootElement
                .GetProperty("name")
                .GetString()!;
        }

        // ============================================================
        // GET
        // ============================================================

        public async Task<T?> GetAsync<T>(
            string path)
        {
            var token =
                await GetAccessTokenAsync();

            using var client =
                new HttpClient();

            var url =
                $"{_databaseUrl}/{path}.json";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Get,
                    url
                );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    token
                );

            var response =
                await client.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Firebase GET failed: " +
                    $"{(int)response.StatusCode} " +
                    $"{response.StatusCode}: " +
                    responseBody
                );
            }

            if (
                string.IsNullOrWhiteSpace(responseBody) ||
                responseBody == "null"
            )
            {
                return default;
            }

            return JsonSerializer.Deserialize<T>(
                responseBody,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }
            );
        }

        // ============================================================
        // UPDATE
        // ============================================================

        public async Task UpdateAsync<T>(
            string path,
            T data)
        {
            var token =
                await GetAccessTokenAsync();

            using var client =
                new HttpClient();

            var json =
                JsonSerializer.Serialize(data);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            var url =
                $"{_databaseUrl}/{path}.json";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Put,
                    url
                );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    token
                );

            request.Content = content;

            var response =
                await client.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Firebase UPDATE failed: " +
                    $"{(int)response.StatusCode} " +
                    $"{response.StatusCode}: " +
                    responseBody
                );
            }
        }

        // ============================================================
        // DELETE
        // ============================================================

        public async Task DeleteAsync(
            string path)
        {
            var token =
                await GetAccessTokenAsync();

            using var client =
                new HttpClient();

            var url =
                $"{_databaseUrl}/{path}.json";

            using var request =
                new HttpRequestMessage(
                    HttpMethod.Delete,
                    url
                );

            request.Headers.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue(
                    "Bearer",
                    token
                );

            var response =
                await client.SendAsync(request);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Firebase DELETE failed: " +
                    $"{(int)response.StatusCode} " +
                    $"{response.StatusCode}: " +
                    responseBody
                );
            }
        }
    }
}