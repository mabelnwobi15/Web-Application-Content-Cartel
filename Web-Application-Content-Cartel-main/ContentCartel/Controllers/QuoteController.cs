using System.Globalization;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using ContentCartel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    [Authorize(Roles = "Client")]
    public class QuoteController : Controller
    {
        private readonly HttpClient _httpClient;

        public QuoteController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClient =
                httpClientFactory.CreateClient("ContentCartelAPI");
        }

        // ============================================================
        // QUOTE FORM
        // GET: /Quote
        // ============================================================

        [HttpGet]
        public IActionResult Index()
        {
            var model = new QuoteRequest
            {
                CustomerName =
                    User.FindFirstValue(ClaimTypes.Name)
                    ?? string.Empty
            };

            return View(model);
        }

        // ============================================================
        // SUBMIT QUOTE
        // POST: /Quote/SubmitQuote
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuote(
            QuoteRequest quote)
        {
            var clientId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Challenge();
            }

            // --------------------------------------------------------
            // VALIDATE ONLY WHAT THE CLIENT ACTUALLY ENTERS
            // --------------------------------------------------------

            if (string.IsNullOrWhiteSpace(quote.BrandName))
            {
                ModelState.AddModelError(
                    nameof(quote.BrandName),
                    "Company / brand name is required.");
            }

            if (string.IsNullOrWhiteSpace(quote.CustomerName))
            {
                ModelState.AddModelError(
                    nameof(quote.CustomerName),
                    "Contact person name is required.");
            }

            if (string.IsNullOrWhiteSpace(quote.Phone))
            {
                ModelState.AddModelError(
                    nameof(quote.Phone),
                    "Phone number is required.");
            }

            if (string.IsNullOrWhiteSpace(quote.MediaType))
            {
                ModelState.AddModelError(
                    nameof(quote.MediaType),
                    "Please choose a media focus.");
            }

            // Properties the form never posts must not block the save
            foreach (var key in new[]
            {
                nameof(QuoteRequest.Id),
                nameof(QuoteRequest.UserId),
                nameof(QuoteRequest.Email),
                nameof(QuoteRequest.ServiceName),
                nameof(QuoteRequest.Description),
                nameof(QuoteRequest.ContactPersonName),
                nameof(QuoteRequest.Status),
                nameof(QuoteRequest.CreatedAt)
            })
            {
                ModelState.Remove(key);
            }

            if (!ModelState.IsValid)
            {
                return View("Index", quote);
            }

            // --------------------------------------------------------
            // PAYLOAD USES THE API'S PROPERTY NAMES
            // (CustomerName -> ContactPersonName,
            //  EstimatedAmount -> EstimatedPrice)
            // --------------------------------------------------------

            var payload = new
            {
                UserId = clientId,
                BrandName = (quote.BrandName ?? "").Trim(),
                SocialMediaHandle = (quote.SocialMediaHandle ?? "").Trim(),
                ContactPersonName = (quote.CustomerName ?? "").Trim(),
                Phone = (quote.Phone ?? "").Trim(),
                MediaType = (quote.MediaType ?? "").Trim(),
                Budget = (quote.Budget ?? "").Trim(),
                CampaignGoals = (quote.CampaignGoals ?? "").Trim(),
                EstimatedPrice = quote.EstimatedAmount ?? 0m,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/quotes",
                        payload);

                var body =
                    await response.Content
                        .ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "",
                        ExtractMessage(body)
                        ?? "Unable to submit quote.");

                    return View("Index", quote);
                }

                string? newId = null;

                try
                {
                    using var document = JsonDocument.Parse(body);
                    newId = Str(document.RootElement, "id");
                }
                catch (JsonException)
                {
                    // handled below
                }

                if (string.IsNullOrWhiteSpace(newId))
                {
                    ModelState.AddModelError(
                        "",
                        "Your quote was saved, but the API did not return a reference. " +
                        "You can find it under Quotations.");

                    return View("Index", quote);
                }

                return RedirectToAction(
                    nameof(QuoteSubmitted),
                    new { id = newId });
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "The quote service is currently unavailable. " +
                    "Please make sure the ContentCartel API is running.");

                return View("Index", quote);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An error occurred while submitting your quote: "
                    + ex.Message);

                return View("Index", quote);
            }
        }

        // ============================================================
        // CONFIRMATION
        // GET: /Quote/QuoteSubmitted/{id}
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> QuoteSubmitted(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            var clientId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Challenge();
            }

            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"api/quotes/{Uri.EscapeDataString(id)}");

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var body =
                    await response.Content
                        .ReadAsStringAsync();

                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                // A client may only open their own quote
                if (!string.Equals(
                        Str(root, "userId"),
                        clientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                var contact = Str(root, "contactPersonName");

                var quote = new QuoteRequest
                {
                    Id = id,
                    UserId = clientId,
                    BrandName = Str(root, "brandName"),
                    SocialMediaHandle = Str(root, "socialMediaHandle"),
                    CustomerName = contact,
                    ContactPersonName = contact,
                    Phone = Str(root, "phone"),
                    Email = User.FindFirstValue(ClaimTypes.Email),
                    MediaType = Str(root, "mediaType"),
                    ServiceName = Str(root, "mediaType"),
                    Budget = Str(root, "budget"),
                    CampaignGoals = Str(root, "campaignGoals"),
                    Status = Str(root, "status") ?? "Pending"
                };

                if (decimal.TryParse(
                        Str(root, "estimatedPrice"),
                        NumberStyles.Any,
                        CultureInfo.InvariantCulture,
                        out var price))
                {
                    quote.EstimatedPrice = price;
                    quote.EstimatedAmount = price;
                }

                if (DateTime.TryParse(
                        Str(root, "createdAt"),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var created))
                {
                    quote.CreatedAt = created;
                }

                return View(quote);
            }
            catch
            {
                return NotFound();
            }
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private static string? ExtractMessage(string body)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    return null;
                }

                return Str(root, "message") ?? Str(root, "error");
            }
            catch (JsonException)
            {
                return null;
            }
        }

        // Case-insensitive property read (API may send id / Id)
        private static string? Str(JsonElement root, string name)
        {
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            foreach (var property in root.EnumerateObject())
            {
                if (!string.Equals(
                        property.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                return property.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.Undefined => null,
                    JsonValueKind.String => property.Value.GetString(),
                    _ => property.Value.ToString()
                };
            }

            return null;
        }
    }
}