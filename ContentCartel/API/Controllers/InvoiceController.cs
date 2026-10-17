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

        // GET: api/invoices
        [HttpGet]
        public async Task<IActionResult> GetInvoices()
        {
            try
            {
                var invoices =
                    await _firebaseService
                        .GetAsync<Dictionary<string, Invoice>>("invoices");

                if (invoices == null || invoices.Count == 0)
                {
                    return Ok(new List<Invoice>());
                }

                var result = invoices
                    .Select(x =>
                    {
                        var invoice = x.Value;
                        invoice.InvoiceId = x.Key;
                        return invoice;
                    })
                    .OrderByDescending(x => x.CreatedAt)
                    .ToList();

                return Ok(result);
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

        var invoices =
            await _firebaseService
                .GetAsync<Dictionary<string, Invoice>>("invoices");

        if (invoices == null || invoices.Count == 0)
        {
            return Ok(new List<Invoice>());
        }

        var result = invoices
            .Where(x =>
                x.Value != null &&
                string.Equals(
                    x.Value.ClientId,
                    clientId,
                    StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var invoice = x.Value;
                invoice.InvoiceId = x.Key;
                return invoice;
            })
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