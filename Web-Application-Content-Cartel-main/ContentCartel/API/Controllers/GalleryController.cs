using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/galleries")]
    public class GalleryController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public GalleryController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        // Reads every gallery item, skipping any record that can't be read.
        private async Task<List<GalleryItem>> LoadAllAsync()
        {
            var raw =
                await _firebaseService
                    .GetAsync<Dictionary<string, System.Text.Json.JsonElement>>("galleries");

            var result = new List<GalleryItem>();

            if (raw == null)
            {
                return result;
            }

            foreach (var entry in raw)
            {
                try
                {
                    var item = System.Text.Json.JsonSerializer
                        .Deserialize<GalleryItem>(entry.Value, JsonOptions);

                    if (item != null)
                    {
                        item.Id = entry.Key;
                        result.Add(item);
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                }
            }

            return result;
        }

        // ============================================================
        // GET ALL (admin)
        // GET: api/galleries
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                var result = (await LoadAllAsync())
                    .OrderByDescending(x => x.UploadedAt)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve galleries.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET CLIENT GALLERIES (approved items only)
        // GET: api/galleries/client/{userId}
        // ============================================================

        [HttpGet("client/{userId}")]
        public async Task<IActionResult> GetClientGalleries(string userId)
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

                var result = (await LoadAllAsync())
                    .Where(x =>
                        x.IsApprovedByAdmin &&
                        string.Equals(
                            x.UserId,
                            userId,
                            StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(x => x.UploadedAt)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve client galleries.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // GET ONE
        // GET: api/galleries/{id}
        // ============================================================

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(string id)
        {
            try
            {
                var item =
                    await _firebaseService
                        .GetAsync<GalleryItem>($"galleries/{id}");

                if (item == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Gallery item not found."
                    });
                }

                item.Id = id;

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve gallery item.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // CREATE (admin / staff)
        // POST: api/galleries
        // The owner is taken from the booking, so the admin only
        // has to pick the booking.
        // ============================================================

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] GalleryItem item)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(item.BookingId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Please choose a booking."
                    });
                }

                if (string.IsNullOrWhiteSpace(item.Title))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A title is required."
                    });
                }

                if (string.IsNullOrWhiteSpace(item.FileUrl))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "A file link is required."
                    });
                }

                var booking =
                    await _firebaseService
                        .GetAsync<Booking>($"bookings/{item.BookingId}");

                if (booking == null || string.IsNullOrWhiteSpace(booking.UserId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Booking not found."
                    });
                }

                item.UserId = booking.UserId!;
                item.IsApprovedByAdmin = false;
                item.UploadedAt = DateTime.UtcNow;

                var id =
                    await _firebaseService
                        .CreateAsync("galleries", item);

                item.Id = id;

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to save gallery item.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // APPROVE (admin)
        // PUT: api/galleries/{id}/approve
        // ============================================================

        [HttpPut("{id}/approve")]
        public async Task<IActionResult> Approve(string id)
        {
            try
            {
                var item =
                    await _firebaseService
                        .GetAsync<GalleryItem>($"galleries/{id}");

                if (item == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Gallery item not found."
                    });
                }

                item.Id = id;
                item.IsApprovedByAdmin = true;

                await _firebaseService
                    .UpdateAsync($"galleries/{id}", item);

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to approve gallery item.",
                    error = ex.Message
                });
            }
        }
    }
}