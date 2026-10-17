using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/bookings")]
    public class BookingController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public BookingController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // ============================================================
        // CREATE BOOKING
        // POST: /api/bookings
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> CreateBooking(
            [FromBody] Booking booking)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(booking.CustomerName))
                {
                    return BadRequest(new
                    {
                        message = "Customer name is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(booking.Email))
                {
                    return BadRequest(new
                    {
                        message = "Email is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(booking.ServiceId))
                {
                    return BadRequest(new
                    {
                        message = "Please select a service."
                    });
                }

                SetServiceDetails(booking);

                booking.DepositAmount =
                    Math.Round(
                        booking.TotalAmount * 0.30m,
                        2
                    );

                booking.RemainingBalance =
                    booking.TotalAmount -
                    booking.DepositAmount;

                booking.Status = "Pending";
                booking.PaymentStatus = "Pending";
                booking.CreatedAt = DateTime.UtcNow;

                var id =
                    await _firebaseService.CreateAsync(
                        "bookings",
                        booking
                    );

                booking.Id = id;

                return Ok(booking);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        error = ex.Message
                    }
                );
            }
        }

        // ============================================================
        // GET BOOKING
        // GET: /api/bookings/{id}
        // ============================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetBooking(
            string id)
        {
            try
            {
                var booking =
                    await _firebaseService.GetAsync<Booking>(
                        $"bookings/{id}"
                    );

                if (booking == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Booking not found."
                    });
                }

                booking.Id = id;

                return Ok(booking);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        error = ex.Message
                    }
                );
            }
        }

        // ============================================================
        // UPDATE BOOKING
        // PUT: /api/bookings/{id}
        //
        // Used for:
        // - Editing booking details
        // - Rescheduling
        // - Updating booking status
        // - Updating payment status
        // ============================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBooking(
            string id,
            [FromBody] Booking booking)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new
                    {
                        message = "Booking ID is required."
                    });
                }

                var existingBooking =
                    await _firebaseService.GetAsync<Booking>(
                        $"bookings/{id}"
                    );

                if (existingBooking == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Booking not found."
                    });
                }

                if (string.IsNullOrWhiteSpace(booking.CustomerName))
                {
                    return BadRequest(new
                    {
                        message = "Customer name is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(booking.Email))
                {
                    return BadRequest(new
                    {
                        message = "Email is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(booking.ServiceId))
                {
                    return BadRequest(new
                    {
                        message = "Service is required."
                    });
                }

                // Keep the Firebase ID
                booking.Id = id;

                // Keep original creation date
                booking.CreatedAt =
                    existingBooking.CreatedAt;

                // Recalculate service information
                SetServiceDetails(booking);

                // Recalculate payment amounts
                booking.DepositAmount =
                    Math.Round(
                        booking.TotalAmount * 0.30m,
                        2
                    );

                booking.RemainingBalance =
                    booking.TotalAmount -
                    booking.DepositAmount;

                // If no status was supplied, keep existing status
                if (string.IsNullOrWhiteSpace(booking.Status))
                {
                    booking.Status =
                        existingBooking.Status;
                }

                if (string.IsNullOrWhiteSpace(
                    booking.PaymentStatus))
                {
                    booking.PaymentStatus =
                        existingBooking.PaymentStatus;
                }

                await _firebaseService.UpdateAsync(
                    $"bookings/{id}",
                    booking
                );

                return Ok(booking);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        error = ex.Message
                    }
                );
            }
        }

        // ============================================================
        // CANCEL BOOKING
        // PATCH: /api/bookings/{id}/cancel
        // ============================================================

        [HttpPatch("{id}/cancel")]
        public async Task<IActionResult> CancelBooking(
            string id)
        {
            try
            {
                var booking =
                    await _firebaseService.GetAsync<Booking>(
                        $"bookings/{id}"
                    );

                if (booking == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Booking not found."
                    });
                }

                booking.Id = id;
                booking.Status = "Cancelled";

                await _firebaseService.UpdateAsync(
                    $"bookings/{id}",
                    booking
                );

                return Ok(new
                {
                    success = true,
                    message = "Booking cancelled successfully.",
                    booking
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        error = ex.Message
                    }
                );
            }
        }

        // ============================================================
        // SERVICE DETAILS
        // ============================================================

        private static void SetServiceDetails(
            Booking booking)
        {
            switch (booking.ServiceId.ToLower())
            {
                case "reels":

                    booking.ServiceName =
                        "Short-Form Reels Package";

                    booking.TotalAmount = 3500;

                    break;

                case "podcast":

                    booking.ServiceName =
                        "Podcast Studio Production";

                    booking.TotalAmount = 7800;

                    break;

                case "creator":

                    booking.ServiceName =
                        "Influencer Creator Collab";

                    booking.TotalAmount = 12000;

                    break;

                case "other":

                    if (string.IsNullOrWhiteSpace(
                        booking.ServiceName))
                    {
                        throw new ArgumentException(
                            "Please specify the service you need."
                        );
                    }

                    booking.TotalAmount = 0;

                    break;

                default:

                    throw new ArgumentException(
                        "Invalid service selected."
                    );
            }
        }
    }
}