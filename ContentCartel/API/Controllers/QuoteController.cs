using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;


namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/quotes")]
    public class QuoteController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public QuoteController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // POST: api/quotes
        [HttpPost]
        public async Task<IActionResult> CreateQuote(
            [FromBody] QuoteRequest quote)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(quote.BrandName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Brand name is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(quote.ContactPersonName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Contact person name is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(quote.Phone))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Phone number is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(quote.MediaType))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Media type is required."
                    });
                }

                // Set server-side values
                quote.Status = "Pending";
                quote.CreatedAt = DateTime.UtcNow;

                // Save to Firebase
                var id = await _firebaseService.CreateAsync(
                    "quotes",
                    quote
                );

                // Return Firebase ID
                quote.Id = id;

                return Ok(quote);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Unable to save quote.",
                        error = ex.Message
                    }
                );
            }
        }

        // ============================================================
// GET CLIENT QUOTATIONS
// GET: /api/quotes/client/{userId}
// ============================================================

[HttpGet("client/{userId}")]
public async Task<IActionResult> GetClientQuotes(string userId)
{
    try
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new
            {
                success = false,
                message = "Client ID is required."
            });
        }

        var quotes =
            await _firebaseService
                .GetAsync<Dictionary<string, QuoteRequest>>("quotes");

        if (quotes == null || quotes.Count == 0)
        {
            return Ok(new List<QuoteRequest>());
        }

        var result = quotes
            .Where(x =>
                x.Value != null &&
                string.Equals(
                    x.Value.UserId,
                    userId,
                    StringComparison.OrdinalIgnoreCase))
            .Select(x =>
            {
                var quote = x.Value;
                quote.Id = x.Key;
                return quote;
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
            message = "Unable to retrieve client quotations.",
            error = ex.Message
        });
    }
}

        // GET: api/quotes/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQuote(string id)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Quote ID is required."
                    });
                }

                var quote =
                    await _firebaseService.GetAsync<QuoteRequest>(
                        $"quotes/{id}"
                    );

                if (quote == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Quote not found."
                    });
                }

                quote.Id = id;

                return Ok(quote);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Unable to retrieve quote.",
                        error = ex.Message
                    }
                );
            }
        }
    }
}