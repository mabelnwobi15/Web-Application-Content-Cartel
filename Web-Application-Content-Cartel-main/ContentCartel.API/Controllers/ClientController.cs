using ContentCartel.Models; // Adjust to your API's models namespace
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ContentCartel.API.Controllers
{
    [Route("api/client")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly ApplicationDbContext _context; // Replace with your actual DbContext name
        private readonly IWebHostEnvironment _env;

        public ClientController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ============================================================  
        // GET GALLERIES FOR A CLIENT  
        // Route: GET api/client/galleries/{clientId}  
        // ============================================================  
        [HttpGet("galleries/{clientId}")]
        public async Task<IActionResult> GetGalleries(string clientId)
        {
            if (string.IsNullOrWhiteSpace(clientId))
            {
                return BadRequest("Client ID is required.");
            }

            try
            {
                var galleries = await _context.GalleryItems
                    .Where(g => g.ClientId == clientId)
                    .ToListAsync();

                return Ok(galleries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        // ============================================================  
        // UPLOAD GALLERY ITEM FOR A CLIENT  
        // Route: POST api/client/galleries/upload  
        // ============================================================  
        [HttpPost("galleries/upload")]
        public async Task<IActionResult> UploadGallery(
            [FromForm] IFormFile file, 
            [FromForm] string clientId, 
            [FromForm] string title, 
            [FromForm] string description)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file was uploaded.");
                }

                if (string.IsNullOrWhiteSpace(clientId))
                {
                    return BadRequest("Client ID is required.");
                }

                // 1. Resolve web root path and create directory if missing
                string webRootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                string uploadsFolder = Path.Combine(webRootPath, "uploads", "galleries");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                // 2. Generate a unique safe filename
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(file.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 3. Save file to disk
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(fileStream);
                }

                // 4. Create database record
                var galleryItem = new GalleryItem
                {
                    ClientId = clientId,
                    Title = !string.IsNullOrWhiteSpace(title) ? title : file.FileName,
                    Description = description,
                    ImageUrl = $"/uploads/galleries/{uniqueFileName}",
                    CreatedAt = DateTime.UtcNow
                };

                _context.GalleryItems.Add(galleryItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Uploaded successfully", data = galleryItem });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}