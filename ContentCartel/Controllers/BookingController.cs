using ContentCartel.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ContentCartel.Controllers
{
    public class BookingController : Controller
    {
        private readonly HttpClient _httpClient;

        public BookingController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClient =
                httpClientFactory.CreateClient("ContentCartelAPI");
        }

        // GET: /Booking
        [HttpGet]
        public IActionResult Index()
        {
            return View(new Booking());
        }

        // POST: /Booking/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(
            Booking booking)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", booking);
            }

            booking.Status = "Pending";
            booking.PaymentStatus = "Pending";
            booking.CreatedAt = DateTime.UtcNow;

            try
            {
                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/bookings",
                        booking
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        "Unable to create booking. " + error
                    );

                    return View("Index", booking);
                }

                var createdBooking =
                    await response.Content
                        .ReadFromJsonAsync<Booking>();

                if (createdBooking == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Booking was created but could not be retrieved."
                    );

                    return View("Index", booking);
                }

                // Go directly to the confirmed booking page
                return RedirectToAction(
                    "ConfirmedBooking",
                    new
                    {
                        id = createdBooking.Id
                    }
                );
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "The booking service is currently unavailable. Please make sure the ContentCartel API is running."
                );

                return View("Index", booking);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    "An error occurred while creating the booking: "
                    + ex.Message
                );

                return View("Index", booking);
            }
        }

        // GET: /Booking/ConfirmedBooking/{id}
        [HttpGet]
        public async Task<IActionResult> ConfirmedBooking(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"api/bookings/{id}"
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

                booking.Id = id;

                return View(booking);
            }
            catch (HttpRequestException)
            {
                return NotFound();
            }
            catch
            {
                return NotFound();
            }
        }
    }
}