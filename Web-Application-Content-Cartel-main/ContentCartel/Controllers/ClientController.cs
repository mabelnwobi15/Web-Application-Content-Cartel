using ContentCartel.Models;
using ContentCartel.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContentCartel.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        // Must match the API's GalleryController limits (10 MB file, 12 MB request).
        // Clients are capped lower on purpose: base64 in Realtime Database inflates size by ~33%.
        private const long MaxClientFileBytes = 5_242_880;      // 5 MB
        private const long MaxRequestBytes = 12_582_912;        // 12 MB

        private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/png", "image/webp", "image/gif"
        };

        private readonly HttpClient _httpClient;
        private readonly ILogger<ClientController> _logger;

        public ClientController(IHttpClientFactory httpClientFactory, ILogger<ClientController> logger)
        {
            _httpClient = httpClientFactory.CreateClient("ContentCartelAPI");
            _logger = logger;
        }

        private string? GetClientId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ============================================================
        // INDEX
        // ============================================================
        [HttpGet]
        public IActionResult Index() => RedirectToAction(nameof(Dashboard));

        // ============================================================
        // DASHBOARD
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            var model = new ClientDashboardViewModel();

            try
            {
                var id = Uri.EscapeDataString(clientId);

                var bookingsResponse = await _httpClient.GetAsync($"api/bookings/client/{id}");
                model.Bookings = bookingsResponse.IsSuccessStatusCode
                    ? await bookingsResponse.Content.ReadFromJsonAsync<List<Booking>>() ?? new List<Booking>()
                    : new List<Booking>();

                var invoicesResponse = await _httpClient.GetAsync($"api/invoices/client/{id}");
                model.Invoices = invoicesResponse.IsSuccessStatusCode
                    ? await invoicesResponse.Content.ReadFromJsonAsync<List<Invoice>>() ?? new List<Invoice>()
                    : new List<Invoice>();

                var quotationsResponse = await _httpClient.GetAsync($"api/quotes/client/{id}");
                model.Quotations = quotationsResponse.IsSuccessStatusCode
                    ? await quotationsResponse.Content.ReadFromJsonAsync<List<QuoteRequest>>() ?? new List<QuoteRequest>()
                    : new List<QuoteRequest>();

                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The client service is currently unavailable. Please try again.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dashboard failed for client {ClientId}", clientId);
                TempData["ErrorMessage"] = "Unable to load your dashboard.";
                return View(model);
            }
        }

        // ============================================================
        // MY BOOKINGS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/bookings/client/{Uri.EscapeDataString(clientId)}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Unable to load your bookings.";
                    return View(new List<Booking>());
                }

                var bookings = await response.Content.ReadFromJsonAsync<List<Booking>>() ?? new List<Booking>();
                return View(bookings);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The booking service is currently unavailable.";
                return View(new List<Booking>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bookings failed for client {ClientId}", clientId);
                TempData["ErrorMessage"] = "Unable to load bookings.";
                return View(new List<Booking>());
            }
        }

        // ============================================================
        // BOOKING DETAILS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> BookingDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/bookings/{Uri.EscapeDataString(id)}");
                if (!response.IsSuccessStatusCode) return NotFound();

                var booking = await response.Content.ReadFromJsonAsync<Booking>();
                if (booking == null) return NotFound();

                if (!string.Equals(booking.UserId, clientId, StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                return View(booking);
            }
            catch
            {
                return NotFound();
            }
        }

        // ============================================================
        // INVOICES
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Invoices()
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/invoices/client/{Uri.EscapeDataString(clientId)}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Unable to load your invoices.";
                    return View(new List<Invoice>());
                }

                var invoices = await response.Content.ReadFromJsonAsync<List<Invoice>>() ?? new List<Invoice>();
                return View(invoices);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The invoice service is currently unavailable.";
                return View(new List<Invoice>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoices failed for client {ClientId}", clientId);
                TempData["ErrorMessage"] = "Unable to load invoices.";
                return View(new List<Invoice>());
            }
        }

        // ============================================================
        // INVOICE DETAILS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> InvoiceDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/invoices/{Uri.EscapeDataString(id)}");
                if (!response.IsSuccessStatusCode) return NotFound();

                var invoice = await response.Content.ReadFromJsonAsync<Invoice>();
                if (invoice == null) return NotFound();

                if (!string.Equals(invoice.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                return View(invoice);
            }
            catch
            {
                return NotFound();
            }
        }

        // ============================================================
        // QUOTATIONS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Quotations()
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/quotes/client/{Uri.EscapeDataString(clientId)}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Unable to load your quotations.";
                    return View(new List<QuoteRequest>());
                }

                var quotations = await response.Content.ReadFromJsonAsync<List<QuoteRequest>>() ?? new List<QuoteRequest>();
                return View(quotations);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The quotation service is currently unavailable.";
                return View(new List<QuoteRequest>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Quotations failed for client {ClientId}", clientId);
                TempData["ErrorMessage"] = "Unable to load quotations.";
                return View(new List<QuoteRequest>());
            }
        }

        // ============================================================
        // QUOTATION DETAILS
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> QuotationDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return NotFound();

            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/quotes/{Uri.EscapeDataString(id)}");
                if (!response.IsSuccessStatusCode) return NotFound();

                var quotation = await response.Content.ReadFromJsonAsync<QuoteRequest>();
                if (quotation == null) return NotFound();

                // If QuoteRequest uses ClientId instead of UserId, change it here.
                if (!string.Equals(quotation.UserId, clientId, StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                return View(quotation);
            }
            catch
            {
                return NotFound();
            }
        }

        // ============================================================
        // PROFILE
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/users/{Uri.EscapeDataString(clientId)}");

                if (response.IsSuccessStatusCode)
                {
                    var profile = await response.Content.ReadFromJsonAsync<UserProfile>();
                    if (profile != null) return View(profile);
                }
            }
            catch
            {
                // Fall back to authentication claims.
            }

            var fallbackProfile = new UserProfile
            {
                Uid = clientId,
                FullName = User.FindFirstValue(ClaimTypes.Name) ?? "Client",
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                Role = "Client",
                Status = "Active"
            };

            return View(fallbackProfile);
        }

        // ============================================================
        // SIMULATED PAYMENT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulatePayment(string invoiceId, decimal amount)
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            if (string.IsNullOrWhiteSpace(invoiceId) || amount <= 0)
            {
                TempData["ErrorMessage"] = "Invalid payment information.";
                return RedirectToAction(nameof(Invoices));
            }

            try
            {
                var invoiceResponse = await _httpClient.GetAsync($"api/invoices/{Uri.EscapeDataString(invoiceId)}");
                if (!invoiceResponse.IsSuccessStatusCode) return NotFound();

                var invoice = await invoiceResponse.Content.ReadFromJsonAsync<Invoice>();
                if (invoice == null) return NotFound();

                if (!string.Equals(invoice.ClientId, clientId, StringComparison.OrdinalIgnoreCase))
                    return Forbid();

                if (amount > invoice.BalanceDue) amount = invoice.BalanceDue;

                if (amount <= 0)
                {
                    TempData["ErrorMessage"] = "There is no outstanding balance to pay.";
                    return RedirectToAction(nameof(Invoices));
                }

                var paymentResponse = await _httpClient.PutAsJsonAsync(
                    $"api/invoices/{Uri.EscapeDataString(invoiceId)}/payment",
                    new { Amount = amount });

                if (!paymentResponse.IsSuccessStatusCode)
                {
                    var error = await paymentResponse.Content.ReadAsStringAsync();
                    _logger.LogWarning("Payment API failed for invoice {InvoiceId}: {Error}", invoiceId, error);
                    TempData["ErrorMessage"] = "Unable to process payment.";
                    return RedirectToAction(nameof(Invoices));
                }

                TempData["SuccessMessage"] = "Payment completed successfully.";
                return RedirectToAction(nameof(Invoices));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment failed for invoice {InvoiceId}", invoiceId);
                TempData["ErrorMessage"] = "Payment failed. Please try again.";
                return RedirectToAction(nameof(Invoices));
            }
        }

        // ============================================================
        // SETTINGS
        // ============================================================
        [HttpGet]
        public IActionResult Settings()
        {
            if (string.IsNullOrWhiteSpace(GetClientId())) return Forbid();
            return View();
        }

        // ============================================================
        // GALLERIES (list)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Galleries()
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            try
            {
                var response = await _httpClient.GetAsync($"api/galleries/client/{Uri.EscapeDataString(clientId)}");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = "Unable to load your galleries.";
                    return View(new List<GalleryItem>());
                }

                var galleries = await response.Content.ReadFromJsonAsync<List<GalleryItem>>() ?? new List<GalleryItem>();
                return View(galleries);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] = "The gallery service is currently unavailable.";
                return View(new List<GalleryItem>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Galleries failed for client {ClientId}", clientId);
                TempData["ErrorMessage"] = "Unable to load galleries.";
                return View(new List<GalleryItem>());
            }
        }

        // ============================================================
        // GALLERIES (upload)
        // Form fields: file, title, description, bookingId (optional)
        // Posts to the API's GalleryController: POST api/galleries/upload
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<IActionResult> UploadGallery(
            IFormFile? file, string? title, string? description, string? bookingId)
        {
            var clientId = GetClientId();
            if (string.IsNullOrWhiteSpace(clientId)) return Forbid();

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Please choose a picture to upload.";
                return RedirectToAction(nameof(Galleries));
            }

            if (file.Length > MaxClientFileBytes)
            {
                TempData["ErrorMessage"] = "Pictures can be up to 5 MB.";
                return RedirectToAction(nameof(Galleries));
            }

            if (!AllowedImageTypes.Contains(file.ContentType ?? string.Empty))
            {
                TempData["ErrorMessage"] = "Only JPG, PNG, WEBP or GIF pictures are allowed.";
                return RedirectToAction(nameof(Galleries));
            }

            try
            {
                using var content = new MultipartFormDataContent();
                await using var stream = file.OpenReadStream();

                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType!);

                // Field names must match the API's GalleryUploadRequest.
                content.Add(fileContent, "File", Path.GetFileName(file.FileName));
                content.Add(new StringContent(clientId), "UserId");
                content.Add(new StringContent("Client"), "UploadedByRole");
                content.Add(new StringContent(title ?? string.Empty), "Title");
                content.Add(new StringContent(description ?? string.Empty), "Description");
                content.Add(new StringContent(bookingId ?? string.Empty), "BookingId");

                var response = await _httpClient.PostAsync("api/galleries/upload", content);

                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gallery upload API returned {Status}: {Error}",
                        (int)response.StatusCode, error);
                    TempData["ErrorMessage"] = "Upload failed. Please try again.";
                    return RedirectToAction(nameof(Galleries));
                }

                TempData["SuccessMessage"] = "Upload completed successfully.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Gallery upload failed for client {ClientId}", clientId);
                TempData["ErrorMessage"] = "Upload failed. Please try again.";
            }

            return RedirectToAction(nameof(Galleries));
        }

        // ============================================================
        // LOGOUT
        // ============================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout() => RedirectToAction("Logout", "Account");
    }
}