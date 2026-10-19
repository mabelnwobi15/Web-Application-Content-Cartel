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
                // and only once the admin has approved it.
                if (!string.Equals(
                        gallery.UserId,
                        clientId,
                        StringComparison.OrdinalIgnoreCase) ||
                    !gallery.IsApprovedByAdmin)
                {
                    return NotFound();
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
    }
}