using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Google.Apis.Auth.OAuth2;

namespace ContentCartel.API.Services
{
    public class FirebaseService
    {
        private readonly string _databaseUrl;
        private readonly string _credentialPath;
        private readonly string _storageBucket;

        public FirebaseService(IConfiguration configuration)
        {
            // Set Firebase__StorageBucket on Render (or Firebase:StorageBucket
            // in appsettings) to your bucket name, shown in Firebase > Storage.
            _storageBucket =
                configuration["Firebase:StorageBucket"]
                ?? "contentcartel-62294.firebasestorage.app";

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
        // PATCH (merge: only the given fields change)
        // ==========================================

        public async Task PatchAsync<T>(
            string path,
            T data)
        {
            var token = await GetAccessTokenAsync();

            using var client = new HttpClient();

            var json = JsonSerializer.Serialize(data);

            using var content =
                new StringContent(
                    json,
                    Encoding.UTF8,
                    "application/json"
                );

            var url =
                $"{_databaseUrl}/{path}.json?access_token={Uri.EscapeDataString(token)}";

            using var request =
                new HttpRequestMessage(HttpMethod.Patch, url)
                {
                    Content = content
                };

            var response = await client.SendAsync(request);

            response.EnsureSuccessStatusCode();
        }


        // ==========================================
        // FILE UPLOAD (Firebase Storage) - any file type
        // Returns a public download link.
        // ==========================================

        public async Task<string> UploadFileAsync(
            string objectName,
            string? contentType,
            Stream content)
        {
            var credential = GoogleCredential
                .FromFile(_credentialPath)
                .CreateScoped(
                    "https://www.googleapis.com/auth/devstorage.full_control");

            var token = await credential
                .UnderlyingCredential
                .GetAccessTokenForRequestAsync();

            using var client = new HttpClient
            {
                Timeout = TimeSpan.FromMinutes(10)
            };

            // 1. Upload the bytes.
            var uploadUrl =
                $"https://storage.googleapis.com/upload/storage/v1/b/{_storageBucket}/o" +
                $"?uploadType=media&name={Uri.EscapeDataString(objectName)}";

            using var upload = new HttpRequestMessage(HttpMethod.Post, uploadUrl);

            upload.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            upload.Content = new StreamContent(content);

            upload.Content.Headers.ContentType =
                MediaTypeHeaderValue.TryParse(contentType, out var parsed)
                    ? parsed
                    : new MediaTypeHeaderValue("application/octet-stream");

            var uploadResponse = await client.SendAsync(upload);
            var uploadBody = await uploadResponse.Content.ReadAsStringAsync();

            if (!uploadResponse.IsSuccessStatusCode)
            {
                throw new Exception(
                    $"Storage upload failed ({(int)uploadResponse.StatusCode}): {uploadBody}");
            }

            // 2. Give it a download token so the link works without signing in.
            var downloadToken = Guid.NewGuid().ToString();

            var patchUrl =
                $"https://storage.googleapis.com/storage/v1/b/{_storageBucket}/o/" +
                Uri.EscapeDataString(objectName);

            using var patch = new HttpRequestMessage(HttpMethod.Patch, patchUrl)
            {
                Content = new StringContent(
                    JsonSerializer.Serialize(new
                    {
                        metadata = new Dictionary<string, string>
                        {
                            ["firebaseStorageDownloadTokens"] = downloadToken
                        }
                    }),
                    Encoding.UTF8,
                    "application/json")
            };

            patch.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var patchResponse = await client.SendAsync(patch);

            if (!patchResponse.IsSuccessStatusCode)
            {
                var body = await patchResponse.Content.ReadAsStringAsync();

                throw new Exception(
                    $"Storage token failed ({(int)patchResponse.StatusCode}): {body}");
            }

            return
                $"https://firebasestorage.googleapis.com/v0/b/{_storageBucket}/o/" +
                $"{Uri.EscapeDataString(objectName)}?alt=media&token={downloadToken}";
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