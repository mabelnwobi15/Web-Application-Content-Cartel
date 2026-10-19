using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/galleries")]
    public class GalleryController : ControllerBase
    {
        // 100 MB per file.
        private const long MaxUploadBytes = 104_857_600;

        private readonly FirebaseService _firebaseService;

        public GalleryController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
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
                var items =
                    await _firebaseService
                        .GetAsync<Dictionary<string, GalleryItem>>("galleries");

                if (items == null || items.Count == 0)
                {
                    return Ok(new List<GalleryItem>());
                }

                var result = items
                    .Where(x => x.Value != null)
                    .Select(x =>
                    {
                        var item = x.Value;
                        item.Id = x.Key;
                        return item;
                    })
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

                var items =
                    await _firebaseService
                        .GetAsync<Dictionary<string, GalleryItem>>("galleries");

                if (items == null || items.Count == 0)
                {
                    return Ok(new List<GalleryItem>());
                }

                var result = items
                    .Where(x =>
                        x.Value != null &&
                        x.Value.IsApprovedByAdmin &&
                        string.Equals(
                            x.Value.UserId,
                            userId,
                            StringComparison.OrdinalIgnoreCase))
                    .Select(x =>
                    {
                        var item = x.Value;
                        item.Id = x.Key;
                        return item;
                    })
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
        // UPLOAD A FILE (admin: any type, client: pictures)
        // POST: api/galleries/upload   (multipart/form-data)
        // ============================================================

        [HttpPost("upload")]
        [RequestSizeLimit(MaxUploadBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadBytes)]
        public async Task<IActionResult> Upload([FromForm] GalleryUploadRequest request)
        {
            try
            {
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Choose a file to upload." });
                }

                // Work out who owns the file.
                string userId;

                if (!string.IsNullOrWhiteSpace(request.BookingId))
                {
                    var booking =
                        await _firebaseService
                            .GetAsync<Booking>($"bookings/{request.BookingId}");

                    if (booking == null || string.IsNullOrWhiteSpace(booking.UserId))
                    {
                        return BadRequest(new { success = false, message = "Booking not found." });
                    }

                    if (!string.IsNullOrWhiteSpace(request.UserId) &&
                        !string.Equals(booking.UserId, request.UserId,
                            StringComparison.OrdinalIgnoreCase))
                    {
                        return BadRequest(new
                        {
                            success = false,
                            message = "That booking does not belong to this client."
                        });
                    }

                    userId = booking.UserId!;
                }
                else if (!string.IsNullOrWhiteSpace(request.UserId))
                {
                    userId = request.UserId;
                }
                else
                {
                    return BadRequest(new { success = false, message = "Please choose a booking." });
                }

                var originalName = Path.GetFileName(request.File.FileName);

                var safeName = new string(
                    originalName
                        .Select(c => char.IsLetterOrDigit(c) || c is '.' or '-' or '_' ? c : '_')
                        .ToArray());

                if (string.IsNullOrWhiteSpace(safeName))
                {
                    safeName = "file";
                }

                var contentType = request.File.ContentType ?? string.Empty;
                var objectName = $"galleries/{userId}/{Guid.NewGuid():N}_{safeName}";

                string url;

                await using (var stream = request.File.OpenReadStream())
                {
                    url = await _firebaseService.UploadFileAsync(
                        objectName, contentType, stream);
                }

                // Work out the media type from the file itself.
                var mediaType = "File";

                if (contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    mediaType = "Photo";
                }
                else if (contentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase))
                {
                    mediaType = "Video";
                }
                else if (contentType.StartsWith("audio/", StringComparison.OrdinalIgnoreCase))
                {
                    mediaType = "Audio";
                }

                var role =
                    string.Equals(request.UploadedByRole, "Client",
                        StringComparison.OrdinalIgnoreCase)
                        ? "Client"
                        : "Admin";

                var title = string.IsNullOrWhiteSpace(request.Title)
                    ? Path.GetFileNameWithoutExtension(originalName)
                    : request.Title.Trim();

                var item = new GalleryItem
                {
                    UserId = userId,
                    BookingId = request.BookingId ?? string.Empty,
                    Title = title,
                    Description = request.Description ?? string.Empty,
                    FileUrl = url,
                    MediaType = mediaType,
                    FileName = originalName,
                    ContentType = contentType,
                    SizeBytes = request.File.Length,
                    StoragePath = objectName,
                    UploadedByStaffId = request.UploadedByStaffId ?? string.Empty,
                    UploadedByRole = role,
                    // A client's own pictures are visible to them straight away;
                    // admin uploads wait for approval.
                    IsApprovedByAdmin = role == "Client",
                    UploadedAt = DateTime.UtcNow
                };

                item.Id = await _firebaseService.CreateAsync("galleries", item);

                return Ok(item);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to upload the file. " + ex.Message
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

    public class GalleryUploadRequest
    {
        public string? BookingId { get; set; }
        public string? UserId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? UploadedByStaffId { get; set; }
        public string? UploadedByRole { get; set; }
        public IFormFile? File { get; set; }
    }
}