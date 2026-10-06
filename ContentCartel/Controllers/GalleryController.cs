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
                        $"api/galleries/client/{clientId}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load your galleries.";

                    return View(new List<GalleryItem>());
                }

                var galleries =
                    await response.Content
                        .ReadFromJsonAsync<List<GalleryItem>>()
                    ?? new List<GalleryItem>();

                return View(galleries);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The gallery service is currently unavailable.";

                return View(new List<GalleryItem>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load galleries: " + ex.Message;

                return View(new List<GalleryItem>());
            }
        }

        // ============================================================
        // GALLERY DETAILS
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
                        $"api/galleries/{id}"
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

                // SECURITY:
                // A client can only view their own gallery item.
                if (!string.Equals(
                        gallery.UserId,
                        clientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                gallery.Id = id;

                return View(gallery);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}