using ContentCartel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;

namespace ContentCartel.Controllers
{
    [Authorize(Roles = "Client")]
    public class BookingController : Controller
    {
        private readonly HttpClient _httpClient;

        public BookingController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ContentCartelAPI");
        }


        // ============================================================
        // BOOKING PAGE
        // GET: /Booking
        // ============================================================

        [HttpGet]
        public IActionResult Index()
        {
            return View(new Booking
            {
                CustomerName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                Email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty
            });
        }


        // ============================================================
        // CREATE BOOKING
        // POST: /Booking/ConfirmBooking
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(Booking booking)
        {
            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(clientId))
            {
                return Challenge();
            }

            // Server-side fields (never posted by the form)
            booking.UserId = clientId;
            booking.Status = "Pending";
            booking.PaymentStatus = "Pending";
            booking.CreatedAt = DateTime.UtcNow;

            // These are filled by the server/API, so they must not be
            // validated as "required" form input.
            foreach (var key in new[]
            {
                nameof(Booking.Id),
                nameof(Booking.BookingId),
                nameof(Booking.UserId),
                nameof(Booking.User),
                nameof(Booking.ServiceName),
                nameof(Booking.Status),
                nameof(Booking.PaymentStatus),
                nameof(Booking.Notes),
                nameof(Booking.CreatedAt)
            })
            {
                ModelState.Remove(key);
            }

            // Validate form
            if (string.IsNullOrWhiteSpace(booking.CustomerName))
                ModelState.AddModelError(nameof(booking.CustomerName), "Full name is required.");

            if (string.IsNullOrWhiteSpace(booking.Email))
                ModelState.AddModelError(nameof(booking.Email), "Email address is required.");

            if (string.IsNullOrWhiteSpace(booking.Phone))
                ModelState.AddModelError(nameof(booking.Phone), "Phone / WhatsApp number is required.");

            if (string.IsNullOrWhiteSpace(booking.ServiceId))
                ModelState.AddModelError(nameof(booking.ServiceId), "Please select a service.");

            // ServiceName is only needed when the client chose "other"
            if (string.Equals(booking.ServiceId, "other", StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrWhiteSpace(booking.ServiceName))
            {
                ModelState.AddModelError(nameof(booking.ServiceName), "Please describe the service you need.");
            }

            if (booking.BookingDate == default)
                ModelState.AddModelError(nameof(booking.BookingDate), "Please select a booking date.");
            else if (booking.BookingDate.Date < DateTime.Today)
                ModelState.AddModelError(nameof(booking.BookingDate), "The booking date cannot be in the past.");

            if (string.IsNullOrWhiteSpace(booking.BookingTime))
                ModelState.AddModelError(nameof(booking.BookingTime), "Please select a booking time.");

            if (!ModelState.IsValid)
            {
                return View("Index", booking);
            }

            // Send booking to API
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/bookings", booking);
                var responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    string errorMessage =
                        response.StatusCode == System.Net.HttpStatusCode.Conflict
                            ? "This date and time is already booked. Please select another date or time."
                            : "Booking could not be saved.";

                    try
                    {
                        using var document = JsonDocument.Parse(responseBody);

                        if (document.RootElement.TryGetProperty("message", out var messageProperty))
                        {
                            var apiMessage = messageProperty.GetString();
                            if (!string.IsNullOrWhiteSpace(apiMessage))
                                errorMessage = apiMessage;
                        }
                        else if (document.RootElement.TryGetProperty("error", out var errorProperty))
                        {
                            var apiError = errorProperty.GetString();
                            if (!string.IsNullOrWhiteSpace(apiError))
                                errorMessage = apiError;
                        }
                    }
                    catch
                    {
                        // Keep the default message.
                    }

                    ModelState.AddModelError("", errorMessage);
                    return View("Index", booking);
                }

                Booking? createdBooking;

                try
                {
                    createdBooking = JsonSerializer.Deserialize<Booking>(
                        responseBody,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                catch (JsonException ex)
                {
                    ModelState.AddModelError("",
                        "The booking was sent successfully, but the API returned an invalid booking response. " + ex.Message);
                    return View("Index", booking);
                }

                if (createdBooking == null)
                {
                    ModelState.AddModelError("",
                        "The booking was sent successfully, but no booking data was returned by the API.");
                    return View("Index", booking);
                }

                if (string.IsNullOrWhiteSpace(createdBooking.Id))
                {
                    ModelState.AddModelError("",
                        "The booking was created, but the API did not return the booking reference.");
                    return View("Index", booking);
                }

                return RedirectToAction(nameof(ConfirmedBooking), new { id = createdBooking.Id });
            }
            catch (HttpRequestException ex)
            {
                ModelState.AddModelError("",
                    "The Content Cartel API could not be reached. Make sure the API is running. Details: " + ex.Message);
                return View("Index", booking);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("",
                    "An unexpected error occurred while creating the booking: " + ex.Message);
                return View("Index", booking);
            }
        }


        // ============================================================
        // CONFIRMED BOOKING
        // GET: /Booking/ConfirmedBooking/{id}
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> ConfirmedBooking(string id)
        {
            var (booking, error) = await LoadOwnedBookingAsync(id);
            if (error != null) return error;

            return View(booking);
        }


        // ============================================================
        // PAYMENT PAGE
        // GET: /Booking/Payment/{id}
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Payment(string id)
        {
            var (booking, error) = await LoadOwnedBookingAsync(id);
            if (error != null) return error;

            if (string.Equals(booking!.PaymentStatus, "Paid",
                    StringComparison.OrdinalIgnoreCase))
                return RedirectToAction(nameof(ConfirmedBooking), new { id });

            return View(booking);
        }


        // ============================================================
        // SIMULATE DEPOSIT PAYMENT
        // POST: /Booking/SimulatePayment
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SimulatePayment(string id)
        {
            var (booking, error) = await LoadOwnedBookingAsync(id);
            if (error != null) return error;

            try
            {
                booking!.Id = id;
                booking.PaymentStatus = "Paid";
                booking.Status = "Confirmed";

                var putResponse = await _httpClient.PutAsJsonAsync(
                    $"api/bookings/{Uri.EscapeDataString(id)}", booking);

                if (putResponse.IsSuccessStatusCode)
                    TempData["Success"] = "Deposit recorded. Your booking is confirmed.";
                else
                    TempData["Error"] = "Payment could not be recorded. Please try again.";
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "The Content Cartel API could not be reached.";
            }

            return RedirectToAction(nameof(ConfirmedBooking), new { id });
        }


        // ============================================================
        // DELETE BOOKING
        // POST: /Booking/DeleteBooking
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBooking(string id)
        {
            var (booking, error) = await LoadOwnedBookingAsync(id);
            if (error != null) return error;

            try
            {
                var deleteResponse = await _httpClient.DeleteAsync(
                    $"api/bookings/{Uri.EscapeDataString(id)}");

                if (!deleteResponse.IsSuccessStatusCode)
                {
                    TempData["Error"] = "The booking could not be deleted.";
                    return RedirectToAction(nameof(ConfirmedBooking), new { id });
                }

                TempData["Success"] = "Booking deleted.";
                return RedirectToAction("Index", "Home");
            }
            catch (HttpRequestException)
            {
                TempData["Error"] = "The Content Cartel API could not be reached.";
                return RedirectToAction(nameof(ConfirmedBooking), new { id });
            }
        }


        // ============================================================
        // HELPER: load booking + ownership check
        // ============================================================

        private async Task<(Booking? booking, IActionResult? error)> LoadOwnedBookingAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return (null, NotFound());

            var clientId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(clientId))
                return (null, Challenge());

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/bookings/{Uri.EscapeDataString(id)}");

                if (!response.IsSuccessStatusCode)
                    return (null, NotFound());

                var booking = await response.Content.ReadFromJsonAsync<Booking>();
                if (booking == null)
                    return (null, NotFound());

                if (!string.Equals(booking.UserId, clientId,
                        StringComparison.OrdinalIgnoreCase))
                    return (null, Forbid());

                booking.Id = id;
                return (booking, null);
            }
            catch (HttpRequestException)
            {
                return (null, NotFound());
            }
        }
    }
}