using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/invoices")]
    public class InvoiceController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public InvoiceController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // ============================================================
        // GET ALL INVOICES (ADMIN)
        // GET: api/invoices
        //
        // Invoices are normally created the first time a client opens
        // their invoices page. The admin needs to see EVERY booking's
        // invoice, so any booking that doesn't have one yet gets one here.
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetInvoices()
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var invoices =
                    await _firebaseService
                        .GetAsync<Dictionary<string, System.Text.Json.JsonElement>>("invoices")
                    ?? new Dictionary<string, System.Text.Json.JsonElement>();

                // Which bookings already have an invoice?
                var invoicedBookingIds = new HashSet<string>(
                    invoices.Keys,
                    StringComparer.OrdinalIgnoreCase
                );

                foreach (var entry in invoices)
                {
                    try
                    {
                        var existing = System.Text.Json.JsonSerializer
                            .Deserialize<Invoice>(entry.Value, options);

                        if (existing != null &&
                            !string.IsNullOrWhiteSpace(existing.BookingId))
                        {
                            invoicedBookingIds.Add(existing.BookingId);
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                    }
                }

                var bookings =
                    await _firebaseService
                        .GetAsync<Dictionary<string, System.Text.Json.JsonElement>>("bookings");

                var createdAny = false;

                if (bookings != null)
                {
                    foreach (var entry in bookings)
                    {
                        if (invoicedBookingIds.Contains(entry.Key))
                        {
                            continue;
                        }

                        try
                        {
                            var booking = System.Text.Json.JsonSerializer
                                .Deserialize<Booking>(entry.Value, options);

                            if (booking != null)
                            {
                                booking.Id = entry.Key;

                                await InvoiceHelper.EnsureInvoiceAsync(
                                    _firebaseService,
                                    booking
                                );

                                createdAny = true;
                            }
                        }
                        catch (Exception)
                        {
                            // Skip records that can't be read.
                        }
                    }
                }

                if (createdAny)
                {
                    invoices =
                        await _firebaseService
                            .GetAsync<Dictionary<string, System.Text.Json.JsonElement>>("invoices")
                        ?? new Dictionary<string, System.Text.Json.JsonElement>();
                }

                var result = new List<Invoice>();

                foreach (var entry in invoices)
                {
                    try
                    {
                        var invoice = System.Text.Json.JsonSerializer
                            .Deserialize<Invoice>(entry.Value, options);

                        if (invoice != null)
                        {
                            invoice.InvoiceId = entry.Key;
                            result.Add(invoice);
                        }
                    }
                    catch (System.Text.Json.JsonException)
                    {
                        // Skip records that don't match the model.
                    }
                }

                return Ok(
                    result
                        .OrderByDescending(x => x.CreatedAt)
                        .ToList()
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve invoices.",
                    error = ex.Message
                });
            }
        }

        // GET: api/invoices/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(string id)
        {
            try
            {
                var invoice =
                    await _firebaseService
                        .GetAsync<Invoice>($"invoices/{id}");

                if (invoice == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Invoice not found."
                    });
                }

                invoice.InvoiceId = id;

                return Ok(invoice);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve invoice.",
                    error = ex.Message
                });
            }
        }

        // POST: api/invoices
        [HttpPost]
        public async Task<IActionResult> CreateInvoice(
            [FromBody] Invoice invoice)
        {
            if (invoice == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid invoice."
                });
            }

            try
            {
                invoice.InvoiceNumber =
                    $"CC-{DateTime.UtcNow:yyyy}-" +
                    $"{DateTime.UtcNow:MMddHHmmss}";

                invoice.IssueDate = DateTime.UtcNow;
                invoice.CreatedAt = DateTime.UtcNow;

                invoice.AmountPaid = 0;

                invoice.BalanceDue =
                    invoice.TotalAmount;

                invoice.PaymentStatus = "Pending";
                invoice.Status = "Issued";

                var id =
                    await _firebaseService.CreateAsync(
                        "invoices",
                        invoice
                    );

                invoice.InvoiceId = id;

                return Ok(new
                {
                    success = true,
                    message = "Invoice created successfully.",
                    invoice
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to create invoice.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
// GET CLIENT INVOICES
// GET: api/invoices/client/{clientId}
// ============================================================

[HttpGet("client/{clientId}")]
        public async Task<IActionResult> GetClientInvoices(string clientId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(clientId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Client ID is required."
                    });
                }

                var options = new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Make sure every booking this client made has an invoice.
                var bookings =
                    await _firebaseService
                        .GetAsync<Dictionary<string, System.Text.Json.JsonElement>>("bookings");

                if (bookings != null)
                {
                    foreach (var entry in bookings)
                    {
                        try
                        {
                            var booking = System.Text.Json.JsonSerializer
                                .Deserialize<Booking>(entry.Value, options);

                            if (booking != null &&
                                string.Equals(
                                    booking.UserId,
                                    clientId,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                booking.Id = entry.Key;

                                await InvoiceHelper.EnsureInvoiceAsync(
                                    _firebaseService,
                                    booking
                                );
                            }
                        }
                        catch (Exception)
                        {
                            // Skip records that can't be read.
                        }
                    }
                }

                var invoices =
                    await _firebaseService
                        .GetAsync<Dictionary<string, System.Text.Json.JsonElement>>("invoices");

                var result = new List<Invoice>();

                if (invoices != null)
                {
                    foreach (var entry in invoices)
                    {
                        try
                        {
                            var invoice = System.Text.Json.JsonSerializer
                                .Deserialize<Invoice>(entry.Value, options);

                            if (invoice != null &&
                                string.Equals(
                                    invoice.ClientId,
                                    clientId,
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                invoice.InvoiceId = entry.Key;
                                result.Add(invoice);
                            }
                        }
                        catch (System.Text.Json.JsonException)
                        {
                            // Skip records that don't match the model.
                        }
                    }
                }

                result = result
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve client invoices.",
                    error = ex.Message
                });
            }
        }

        // PUT: api/invoices/{id}/payment
        [HttpPut("{id}/payment")]
        public async Task<IActionResult> RecordPayment(
            string id,
            [FromBody] PaymentRequest request)
        {
            if (request == null || request.Amount <= 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Enter a valid payment amount."
                });
            }

            try
            {
                var invoice =
                    await _firebaseService
                        .GetAsync<Invoice>(
                            $"invoices/{id}"
                        );

                if (invoice == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Invoice not found."
                    });
                }

                invoice.InvoiceId = id;

                invoice.AmountPaid += request.Amount;

                if (invoice.AmountPaid >= invoice.TotalAmount)
                {
                    invoice.AmountPaid =
                        invoice.TotalAmount;

                    invoice.BalanceDue = 0;

                    invoice.PaymentStatus = "Paid";
                }
                else if (invoice.AmountPaid > 0)
                {
                    invoice.BalanceDue =
                        invoice.TotalAmount -
                        invoice.AmountPaid;

                    invoice.PaymentStatus =
                        "Partially Paid";
                }
                else
                {
                    invoice.BalanceDue =
                        invoice.TotalAmount;

                    invoice.PaymentStatus =
                        "Pending";
                }

                await _firebaseService.UpdateAsync(
                    $"invoices/{id}",
                    invoice
                );

                // Once the deposit is covered, mark the booking as paid and
                // confirmed so the bookings pages match the invoice.
                if (!string.IsNullOrWhiteSpace(invoice.BookingId) &&
                    invoice.AmountPaid >= invoice.DepositAmount)
                {
                    try
                    {
                        var booking =
                            await _firebaseService.GetAsync<Booking>(
                                $"bookings/{invoice.BookingId}"
                            );

                        if (booking != null &&
                            !string.Equals(
                                booking.PaymentStatus,
                                "Paid",
                                StringComparison.OrdinalIgnoreCase))
                        {
                            booking.Id = invoice.BookingId;
                            booking.PaymentStatus = "Paid";

                            if (string.Equals(
                                    booking.Status,
                                    "Pending",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                booking.Status = "Confirmed";
                            }

                            await _firebaseService.UpdateAsync(
                                $"bookings/{invoice.BookingId}",
                                booking
                            );
                        }
                    }
                    catch
                    {
                        // The payment is saved; the booking can be updated later.
                    }
                }

                return Ok(new
                {
                    success = true,
                    message = "Payment recorded successfully.",
                    invoice
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to record payment.",
                    error = ex.Message
                });
            }
        }
    }

    public class PaymentRequest
    {
        public decimal Amount { get; set; }
    }
}