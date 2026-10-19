using ContentCartel.Helpers;
using ContentCartel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContentCartel.Controllers
{
    [Authorize(Roles = "Client")]
    public class GalleryController : Controller
    {
        // Clients upload pictures: 10 MB each, up to 100 MB per upload.
        private const long MaxPictureBytes = 10_485_760;
        private const long MaxRequestBytes = 104_857_600;

        private static readonly string[] PictureExtensions =
            { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".heic", ".heif", ".avif", ".bmp" };

        private readonly HttpClient _httpClient;

        public GalleryController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ContentCartelAPI");
        }

        private string? GetClientId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // ============================================================
        // MY GALLERIES
        // Uses the same page as Client > Galleries.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientId = GetClientId();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            // Bookings for the upload form.
            ViewBag.Bookings = await LoadBookingsAsync(clientId);

            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"api/galleries/client/{Uri.EscapeDataString(clientId)}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load your galleries.";

                    return View("~/Views/Client/Galleries.cshtml",
                        new List<GalleryItem>());
                }

                var galleries =
                    await response.Content
                        .ReadFromJsonAsync<List<GalleryItem>>()
                    ?? new List<GalleryItem>();

                return View("~/Views/Client/Galleries.cshtml", galleries);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The gallery service is currently unavailable.";

                return View("~/Views/Client/Galleries.cshtml",
                    new List<GalleryItem>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load galleries: " + ex.Message;

                return View("~/Views/Client/Galleries.cshtml",
                    new List<GalleryItem>());
            }
        }

        // ============================================================
        // UPLOAD PICTURES
        // POST: /Gallery/Upload
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<IActionResult> Upload(
            string? bookingId,
            string? title,
            string? description,
            List<IFormFile> files)
        {
            var clientId = GetClientId();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            if (files == null || !files.Any(f => f.Length > 0))
            {
                TempData["ErrorMessage"] = "Choose at least one picture to upload.";
                return RedirectToAction(nameof(Index));
            }

            // Pictures only, and not too big.
            foreach (var file in files.Where(f => f.Length > 0))
            {
                var extension =
                    Path.GetExtension(file.FileName).ToLowerInvariant();

                // Must look like a picture by its type AND its extension
                // (SVG is not allowed: it can carry scripts).
                var looksLikePicture =
                    (file.ContentType ?? string.Empty)
                        .StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                    && !(file.ContentType ?? string.Empty)
                        .Contains("svg", StringComparison.OrdinalIgnoreCase)
                    && PictureExtensions.Contains(extension);

                if (!looksLikePicture)
                {
                    TempData["ErrorMessage"] =
                        $"\"{file.FileName}\" is not a picture. Please upload image files (JPG, PNG, WEBP, HEIC ...).";

                    return RedirectToAction(nameof(Index));
                }

                if (file.Length > MaxPictureBytes)
                {
                    TempData["ErrorMessage"] =
                        $"\"{file.FileName}\" is larger than 10 MB.";

                    return RedirectToAction(nameof(Index));
                }
            }

            // A client can only attach pictures to their own bookings.
            if (!string.IsNullOrWhiteSpace(bookingId))
            {
                var owned = await LoadBookingsAsync(clientId);

                if (!owned.Any(b => string.Equals(
                        b.Id, bookingId, StringComparison.OrdinalIgnoreCase)))
                {
                    TempData["ErrorMessage"] = "That booking was not found.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var (uploaded, errors) =
                await GalleryUploader.UploadAsync(
                    _httpClient, files, bookingId, clientId,
                    title, description, "Client", clientId);

            if (uploaded > 0)
            {
                TempData["SuccessMessage"] = $"{uploaded} picture(s) uploaded.";
            }

            if (errors.Any())
            {
                TempData["ErrorMessage"] =
                    "Some pictures could not be uploaded: " + string.Join(" | ", errors);
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // OPEN ONE ITEM
        // There is no separate details page: this opens the file itself.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var clientId = GetClientId();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"api/galleries/{Uri.EscapeDataString(id)}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var gallery =
                    await response.Content
                        .ReadFromJsonAsync<GalleryItem>();

                if (gallery == null)
                {
                    return NotFound();
                }

                // SECURITY: a client can only open their own item,
                // and only once it is approved.
                if (!string.Equals(
                        gallery.UserId,
                        clientId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !gallery.IsApprovedByAdmin)
                {
                    return NotFound();
                }

                // Uploaded files are served by this site at /media/{id}.
                if (!string.IsNullOrWhiteSpace(gallery.FileUrl) &&
                    gallery.FileUrl.StartsWith("/media/", StringComparison.Ordinal))
                {
                    return LocalRedirect(gallery.FileUrl);
                }

                if (Uri.TryCreate(gallery.FileUrl, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttps ||
                     uri.Scheme == Uri.UriSchemeHttp))
                {
                    return Redirect(uri.ToString());
                }

                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }

        // ============================================================
        // HELPER
        // ============================================================

        private async Task<List<Booking>> LoadBookingsAsync(string clientId)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<List<Booking>>(
                           $"api/bookings/client/{Uri.EscapeDataString(clientId)}")
                       ?? new List<Booking>();
            }
            catch
            {
                return new List<Booking>();
            }
        }
    }
}