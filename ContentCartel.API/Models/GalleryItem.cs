namespace ContentCartel.API.Models
{
    public class GalleryItem
    {
        public string? Id { get; set; }

        // The client who owns the media (taken from the booking).
        public string UserId { get; set; } = string.Empty;

        // Firebase booking id (a string, not a number).
        public string BookingId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        // Link to the file (uploaded file or an external link).
        public string FileUrl { get; set; } = string.Empty;

        // Photo, Video, Reel, Audio or File (any other type).
        public string MediaType { get; set; } = "Photo";

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        // Path inside Firebase Storage (empty for external links).
        public string StoragePath { get; set; } = string.Empty;

        public string UploadedByStaffId { get; set; } = string.Empty;

        public bool IsApprovedByAdmin { get; set; } = false;

        public string Description { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}