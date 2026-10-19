using System.Net.Http.Headers;

namespace ContentCartel.Helpers
{
    // Sends picked files to the API, one request per file.
    public static class GalleryUploader
    {
        // Files are kept in the database: 10 MB each.
        public const long MaxFileBytes = 10_485_760;

        public static async Task<(int Uploaded, List<string> Errors)> UploadAsync(
            HttpClient apiClient,
            IList<IFormFile> files,
            string? bookingId,
            string? userId,
            string? title,
            string? description,
            string role,
            string uploaderId)
        {
            var errors = new List<string>();
            var uploaded = 0;

            // Long timeout: large files take a while to pass through.
            using var client = new HttpClient
            {
                BaseAddress = apiClient.BaseAddress,
                Timeout = TimeSpan.FromMinutes(10)
            };

            for (var i = 0; i < files.Count; i++)
            {
                var file = files[i];

                if (file == null || file.Length == 0)
                {
                    continue;
                }

                if (file.Length > MaxFileBytes)
                {
                    errors.Add($"{file.FileName}: files can be up to 10 MB.");
                    continue;
                }

                var itemTitle =
                    string.IsNullOrWhiteSpace(title)
                        ? Path.GetFileNameWithoutExtension(file.FileName)
                        : files.Count > 1
                            ? $"{title.Trim()} ({i + 1})"
                            : title.Trim();

                try
                {
                    using var form = new MultipartFormDataContent();

                    form.Add(new StringContent(bookingId ?? string.Empty), "BookingId");
                    form.Add(new StringContent(userId ?? string.Empty), "UserId");
                    form.Add(new StringContent(itemTitle), "Title");
                    form.Add(new StringContent(description ?? string.Empty), "Description");
                    form.Add(new StringContent(role), "UploadedByRole");
                    form.Add(new StringContent(uploaderId), "UploadedByStaffId");

                    await using var stream = file.OpenReadStream();

                    var content = new StreamContent(stream);

                    content.Headers.ContentType =
                        MediaTypeHeaderValue.TryParse(file.ContentType, out var parsed)
                            ? parsed
                            : new MediaTypeHeaderValue("application/octet-stream");

                    form.Add(content, "File", file.FileName);

                    var response = await client.PostAsync("api/galleries/upload", form);

                    if (response.IsSuccessStatusCode)
                    {
                        uploaded++;
                    }
                    else
                    {
                        errors.Add($"{file.FileName}: " + await ReadMessageAsync(response));
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"{file.FileName}: {ex.Message}");
                }
            }

            return (uploaded, errors);
        }

        private static async Task<string> ReadMessageAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty("message", out var message))
                {
                    var text = message.GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
            catch { }

            return "Upload failed.";
        }
    }
}