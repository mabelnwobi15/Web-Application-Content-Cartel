using ContentCartel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContentCartel.Controllers
{
    // Serves uploaded gallery files to signed-in users.
    //   Admin  : any file
    //   Client : only their own files (and only once approved,
    //            unless the client uploaded it themselves)
    [Authorize(Roles = "Admin,Client")]
    public class MediaController : Controller
    {
        // Types that are safe to show inside a page.
        // Everything else (SVG, HTML, ...) is sent as a download.
        private static readonly string[] InlineTypes =
        {
            "image/jpeg", "image/png", "image/gif", "image/webp",
            "image/avif", "image/bmp",
            "video/mp4", "video/webm", "video/quicktime",
            "audio/mpeg", "audio/mp4", "audio/wav", "audio/webm", "audio/ogg"
        };

        private readonly HttpClient _httpClient;

        public MediaController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ContentCartelAPI");
        }

        // GET: /media/{id}            (shows in the browser when safe)
        // GET: /media/{id}?download=1 (always downloads)
        [HttpGet("/media/{id}")]
        public async Task<IActionResult> Get(string id, bool download = false)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var itemResponse = await _httpClient.GetAsync(
                    $"api/galleries/{Uri.EscapeDataString(id)}");

                if (!itemResponse.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var item = await itemResponse.Content
                    .ReadFromJsonAsync<GalleryItem>();

                if (item == null)
                {
                    return NotFound();
                }

                if (!User.IsInRole("Admin"))
                {
                    var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                    var ownedByClient =
                        !string.IsNullOrWhiteSpace(clientId) &&
                        string.Equals(item.UserId, clientId,
                            StringComparison.OrdinalIgnoreCase);

                    var visible =
                        item.IsApprovedByAdmin ||
                        string.Equals(item.UploadedByRole, "Client",
                            StringComparison.OrdinalIgnoreCase);

                    if (!ownedByClient || !visible)
                    {
                        return NotFound();
                    }
                }

                var fileResponse = await _httpClient.GetAsync(
                    $"api/galleries/{Uri.EscapeDataString(id)}/file",
                    HttpCompletionOption.ResponseHeadersRead);

                if (!fileResponse.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var contentType = string.IsNullOrWhiteSpace(item.ContentType)
                    ? "application/octet-stream"
                    : item.ContentType;

                var safeInline = InlineTypes.Contains(
                    contentType.ToLowerInvariant());

                // Uploaded files must never run scripts on this site.
                Response.Headers["X-Content-Type-Options"] = "nosniff";
                Response.Headers["Content-Security-Policy"] =
                    "default-src 'none'; img-src 'self'; media-src 'self'; style-src 'unsafe-inline'; sandbox";

                var stream = await fileResponse.Content.ReadAsStreamAsync();

                if (download || !safeInline)
                {
                    var name = string.IsNullOrWhiteSpace(item.FileName)
                        ? "download"
                        : item.FileName;

                    return File(stream, contentType, name);
                }

                return File(stream, contentType);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}