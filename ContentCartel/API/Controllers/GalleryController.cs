using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/galleries")]
    public class GalleryController : ControllerBase
    {
        // Files are stored in the database (no paid storage needed):
        // 10 MB per file, with some room for the form fields.
        private const long MaxFileBytes = 10_485_760;
        private const long MaxRequestBytes = 12_582_912;

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
        // The file itself is kept in the Realtime Database.
        // ============================================================

        [HttpPost("upload")]
        [RequestSizeLimit(MaxRequestBytes)]
        [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
        public async Task<IActionResult> Upload([FromForm] GalleryUploadRequest request)
        {
            string? createdId = null;

            try
            {
                if (request.File == null || request.File.Length == 0)
                {
                    return BadRequest(new { success = false, message = "Choose a file to upload." });
                }

                if (request.File.Length > MaxFileBytes)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Files can be up to 10 MB."
                    });
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
                var contentType = request.File.ContentType ?? string.Empty;

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

                byte[] bytes;

                using (var memory = new MemoryStream())
                {
                    await request.File.CopyToAsync(memory);
                    bytes = memory.ToArray();
                }

                var item = new GalleryItem
                {
                    UserId = userId,
                    BookingId = request.BookingId ?? string.Empty,
                    Title = title,
                    Description = request.Description ?? string.Empty,
                    FileUrl = string.Empty,
                    MediaType = mediaType,
                    FileName = originalName,
                    ContentType = contentType,
                    SizeBytes = request.File.Length,
                    UploadedByStaffId = request.UploadedByStaffId ?? string.Empty,
                    UploadedByRole = role,
                    // A client's own pictures are visible to them straight away;
                    // admin uploads wait for approval.
                    IsApprovedByAdmin = role == "Client",
                    UploadedAt = DateTime.UtcNow
                };

                // 1. the gallery record (gives us the id)
                createdId = await _firebaseService.CreateAsync("galleries", item);

                // 2. the file itself
                await _firebaseService.UpdateAsync(
                    $"galleryFiles/{createdId}",
                    new StoredFile
                    {
                        ContentType = contentType,
                        Data = Convert.ToBase64String(bytes)
                    });

                // 3. point the record at the file (served by the website at /media/{id})
                item.Id = createdId;
                item.StoragePath = $"galleryFiles/{createdId}";
                item.FileUrl = $"/media/{createdId}";

                await _firebaseService.UpdateAsync($"galleries/{createdId}", item);

                return Ok(item);
            }
            catch (Exception ex)
            {
                // Don't leave a half-saved record behind.
                if (!string.IsNullOrWhiteSpace(createdId))
                {
                    try { await _firebaseService.DeleteAsync($"galleries/{createdId}"); } catch { }
                    try { await _firebaseService.DeleteAsync($"galleryFiles/{createdId}"); } catch { }
                }

                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to upload the file. " + ex.Message
                });
            }
        }

        // ============================================================
        // GET THE FILE
        // GET: api/galleries/{id}/file
        // ============================================================

        [HttpGet("{id}/file")]
        public async Task<IActionResult> GetFile(string id)
        {
            try
            {
                var stored =
                    await _firebaseService
                        .GetAsync<StoredFile>($"galleryFiles/{id}");

                if (stored == null || string.IsNullOrEmpty(stored.Data))
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "File not found."
                    });
                }

                var bytes = Convert.FromBase64String(stored.Data);

                return File(
                    bytes,
                    string.IsNullOrWhiteSpace(stored.ContentType)
                        ? "application/octet-stream"
                        : stored.ContentType);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to read the file.",
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

    // A file kept in the database (base64 text).
    public class StoredFile
    {
        public string ContentType { get; set; } = string.Empty;
        public string Data { get; set; } = string.Empty;
    }
}