using ContentCartel.Models;
using ContentCartel.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContentCartel.Controllers
{
    [Authorize(Roles = "Client")]
    public class ClientController : Controller
    {
        private readonly HttpClient _httpClient;

        public ClientController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ContentCartelAPI");
        }

        // ============================================================
        // GET LOGGED-IN CLIENT ID
        // ============================================================

        private string? GetClientId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier);
        }

        // ============================================================
        // INDEX
        // ============================================================

        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        // ============================================================
        // DASHBOARD
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var clientId = GetClientId();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            var model = new ClientDashboardViewModel();

            try
            {
                // ----------------------------------------------------
                // BOOKINGS
                // ----------------------------------------------------

                var bookingsResponse = await _httpClient.GetAsync(
                    $"api/bookings/client/{Uri.EscapeDataString(clientId)}"
                );

                if (bookingsResponse.IsSuccessStatusCode)
                {
                    model.Bookings =
                        await bookingsResponse.Content
                            .ReadFromJsonAsync<List<Booking>>()
                        ?? new List<Booking>();
                }
                else
                {
                    model.Bookings = new List<Booking>();
                }


                // ----------------------------------------------------
                // INVOICES
                // ----------------------------------------------------

                var invoicesResponse = await _httpClient.GetAsync(
                    $"api/invoices/client/{Uri.EscapeDataString(clientId)}"
                );

                if (invoicesResponse.IsSuccessStatusCode)
                {
                    model.Invoices =
                        await invoicesResponse.Content
                            .ReadFromJsonAsync<List<Invoice>>()
                        ?? new List<Invoice>();
                }
                else
                {
                    model.Invoices = new List<Invoice>();
                }


                // ----------------------------------------------------
                // QUOTATIONS
                // ----------------------------------------------------

                var quotationsResponse = await _httpClient.GetAsync(
                    $"api/quotes/client/{Uri.EscapeDataString(clientId)}"
                );

                if (quotationsResponse.IsSuccessStatusCode)
                {
                    model.Quotations =
                        await quotationsResponse.Content
                            .ReadFromJsonAsync<List<QuoteRequest>>()
                        ?? new List<QuoteRequest>();
                }
                else
                {
                    model.Quotations = new List<QuoteRequest>();
                }


                return View(model);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The client service is currently unavailable. Please try again.";

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load your dashboard: " + ex.Message;

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

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/bookings/client/{Uri.EscapeDataString(clientId)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load your bookings.";

                    return View(new List<Booking>());
                }

                var bookings =
                    await response.Content
                        .ReadFromJsonAsync<List<Booking>>()
                    ?? new List<Booking>();

                return View(bookings);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The booking service is currently unavailable.";

                return View(new List<Booking>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load bookings: " + ex.Message;

                return View(new List<Booking>());
            }
        }

        // ============================================================
        // BOOKING DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> BookingDetails(string id)
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
                var response = await _httpClient.GetAsync(
                    $"api/bookings/{Uri.EscapeDataString(id)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var booking =
                    await response.Content
                        .ReadFromJsonAsync<Booking>();

                if (booking == null)
                {
                    return NotFound();
                }

                // ====================================================
                // SECURITY
                // Client can ONLY see their own booking.
                // ====================================================

                if (!string.Equals(
                        booking.UserId,
                        clientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

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

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/invoices/client/{Uri.EscapeDataString(clientId)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load your invoices.";

                    return View(new List<Invoice>());
                }

                var invoices =
                    await response.Content
                        .ReadFromJsonAsync<List<Invoice>>()
                    ?? new List<Invoice>();

                return View(invoices);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The invoice service is currently unavailable.";

                return View(new List<Invoice>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load invoices: " + ex.Message;

                return View(new List<Invoice>());
            }
        }

        // ============================================================
        // INVOICE DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> InvoiceDetails(string id)
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
                var response = await _httpClient.GetAsync(
                    $"api/invoices/{Uri.EscapeDataString(id)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var invoice =
                    await response.Content
                        .ReadFromJsonAsync<Invoice>();

                if (invoice == null)
                {
                    return NotFound();
                }

                // ====================================================
                // SECURITY
                // Client can ONLY see their own invoice.
                // ====================================================

                if (!string.Equals(
                        invoice.ClientId,
                        clientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

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

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/quotes/client/{Uri.EscapeDataString(clientId)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load your quotations.";

                    return View(new List<QuoteRequest>());
                }

                var quotations =
                    await response.Content
                        .ReadFromJsonAsync<List<QuoteRequest>>()
                    ?? new List<QuoteRequest>();

                return View(quotations);
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The quotation service is currently unavailable.";

                return View(new List<QuoteRequest>());
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to load quotations: " + ex.Message;

                return View(new List<QuoteRequest>());
            }
        }

        // ============================================================
        // QUOTATION DETAILS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> QuotationDetails(string id)
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
                var response = await _httpClient.GetAsync(
                    $"api/quotes/{Uri.EscapeDataString(id)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var quotation =
                    await response.Content
                        .ReadFromJsonAsync<QuoteRequest>();

                if (quotation == null)
                {
                    return NotFound();
                }

                // ====================================================
                // SECURITY
                //
                // This assumes QuoteRequest has a UserId property.
                // If your QuoteRequest uses ClientId instead,
                // change quotation.UserId below to quotation.ClientId.
                // ====================================================

                if (!string.Equals(
                        quotation.UserId,
                        clientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

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

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/users/{Uri.EscapeDataString(clientId)}"
                );

                if (response.IsSuccessStatusCode)
                {
                    var profile =
                        await response.Content
                            .ReadFromJsonAsync<UserProfile>();

                    if (profile != null)
                    {
                        return View(profile);
                    }
                }
            }
            catch
            {
                // Fall back to authentication claims.
            }

            // ========================================================
            // FALLBACK PROFILE
            // ========================================================

            var fallbackProfile = new UserProfile
            {
                Uid = clientId,

                FullName =
                    User.FindFirstValue(ClaimTypes.Name)
                    ?? "Client",

                Email =
                    User.FindFirstValue(ClaimTypes.Email)
                    ?? string.Empty,

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
        public async Task<IActionResult> SimulatePayment(
            string invoiceId,
            decimal amount)
        {
            var clientId = GetClientId();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            if (string.IsNullOrWhiteSpace(invoiceId) ||
                amount <= 0)
            {
                TempData["ErrorMessage"] =
                    "Invalid payment information.";

                return RedirectToAction(nameof(Invoices));
            }

            try
            {
                // ----------------------------------------------------
                // GET INVOICE FIRST
                // ----------------------------------------------------

                var invoiceResponse = await _httpClient.GetAsync(
                    $"api/invoices/{Uri.EscapeDataString(invoiceId)}"
                );

                if (!invoiceResponse.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var invoice =
                    await invoiceResponse.Content
                        .ReadFromJsonAsync<Invoice>();

                if (invoice == null)
                {
                    return NotFound();
                }

                // ----------------------------------------------------
                // SECURITY
                // ----------------------------------------------------

                if (!string.Equals(
                        invoice.ClientId,
                        clientId,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return Forbid();
                }

                // ----------------------------------------------------
                // DON'T ALLOW OVERPAYMENT
                // ----------------------------------------------------

                if (amount > invoice.BalanceDue)
                {
                    amount = invoice.BalanceDue;
                }

                if (amount <= 0)
                {
                    TempData["ErrorMessage"] =
                        "There is no outstanding balance to pay.";

                    return RedirectToAction(nameof(Invoices));
                }

                // ----------------------------------------------------
                // SEND PAYMENT TO API
                // ----------------------------------------------------

                var paymentResponse =
                    await _httpClient.PutAsJsonAsync(
                        $"api/invoices/{Uri.EscapeDataString(invoiceId)}/payment",
                        amount
                    );

                if (!paymentResponse.IsSuccessStatusCode)
                {
                    var error =
                        await paymentResponse.Content
                            .ReadAsStringAsync();

                    TempData["ErrorMessage"] =
                        "Unable to process payment. " + error;

                    return RedirectToAction(nameof(Invoices));
                }

                TempData["SuccessMessage"] =
                    "Payment completed successfully.";

                return RedirectToAction(nameof(Invoices));
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Payment failed: " + ex.Message;

                return RedirectToAction(nameof(Invoices));
            }
        }

        // ============================================================
        // GALLERIES
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Galleries()
        {
            var clientId = GetClientId();

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Forbid();
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/galleries/client/{Uri.EscapeDataString(clientId)}"
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
        // LOGOUT
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            return RedirectToAction(
                "Logout",
                "Account"
            );
        }
    }
}