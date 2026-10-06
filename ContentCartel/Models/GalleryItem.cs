using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContentCartel.Models
{
    public class GalleryItem
    {
        [Key]
        public int GalleryItemId { get; set; }

        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;

        [Required]
        public int BookingId { get; set; }

        [ForeignKey("BookingId")]
        public Booking? Booking { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty;

        public string MediaType { get; set; } = "Photo"; // Photo, Video, Reel

        public string UploadedByStaffId { get; set; } = string.Empty;

        public bool IsApprovedByAdmin { get; set; } = false;

        public string Description { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}