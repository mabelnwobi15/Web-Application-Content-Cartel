using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace ContentCartel.API.Services
{
    public class FirebaseService
    {
        private readonly string _databaseUrl;
        private readonly string _credentialPath;

        public FirebaseService()
        {
            _databaseUrl =
                "https://contentcartel-62294-default-rtdb.firebaseio.com";

            _credentialPath = Path.Combine(
                AppContext.BaseDirectory,
                "firebase-service-account.json"
            );

            if (!File.Exists(_credentialPath))
            {
                throw new FileNotFoundException(
                    $"Firebase credential not found at: {_credentialPath}"
                );
            }
        }


        // ==========================================
        // GET FIREBASE ACCESS TOKEN
        // ==========================================

        private async Task<string> GetAccessTokenAsync()
        {
            var credential = GoogleCredential
                .FromFile(_credentialPath)
                .CreateScoped(
                    "https://www.googleapis.com/auth/userinfo.email",
                    "https://www.googleapis.com/auth/firebase.database"
                );

            return await credential
                .UnderlyingCredential
                .GetAccessTokenForRequestAsync();
        }


        // ==========================================
        // CREATE
        // ==========================================

        public async Task<string> CreateAsync<T>(
            string collection,
            T data)
        {
            var token = await GetAccessTokenAsync();

            using var client = new HttpClient();

            var json = JsonSerializer.Serialize(data);

            using var content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json"
            );

            var url =
                $"{_databaseUrl}/{collection}.json?access_token={Uri.EscapeDataString(token)}";

            var response = await client.PostAsync(
                url,
                content
            );

            var responseBody =
                await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Firebase returned {(int)response.StatusCode} " +
                    $"{response.StatusCode}: {responseBody}"
                );
            }

            using var document =
                JsonDocument.Parse(responseBody);

            return document
                .RootElement
                .GetProperty("name")
                .GetString()!;
        }


        // ==========================================
        // GET
        // ==========================================

        public async Task<T?> GetAsync<T>(
            string path)
        {
            var token = await GetAccessTokenAsync();

            using var client = new HttpClient();

            var url =
                $"{_databaseUrl}/{path}.json?access_token={Uri.EscapeDataString(token)}";

            var response =
                await client.GetAsync(url);

            var responseBody =
                await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return JsonSerializer.Deserialize<T>(
                responseBody
            );
        }


        // ==========================================
        // UPDATE
        // ==========================================

        public async Task UpdateAsync<T>(
            string path,
            T data)
        {
            var token = await GetAccessTokenAsync();

            using var client = new HttpClient();

            var json =
                JsonSerializer.Serialize(data);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            var url =
                $"{_databaseUrl}/{path}.json?access_token={Uri.EscapeDataString(token)}";

            var response =
                await client.PutAsync(
                    url,
                    content
                );

            response.EnsureSuccessStatusCode();
        }


        // ==========================================
        // DELETE
        // ==========================================

        public async Task DeleteAsync(
            string path)
        {
            var token =
                await GetAccessTokenAsync();

            using var client =
                new HttpClient();

            var url =
                $"{_databaseUrl}/{path}.json?access_token={Uri.EscapeDataString(token)}";

            var response =
                await client.DeleteAsync(url);

            response.EnsureSuccessStatusCode();
        }
    }
}