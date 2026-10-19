namespace ContentCartel.Models
{
    public class GalleryItem
    {
        // Kept so older views still compile; not stored by the API.
        public int GalleryItemId { get; set; }

        // Firebase id
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        // Firebase booking id (a string, not a number)
        public string BookingId { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string FileUrl { get; set; } = string.Empty;

        // Photo, Video, Reel, Audio or File (any other type)
        public string MediaType { get; set; } = "Photo";

        public string FileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long SizeBytes { get; set; }

        public string StoragePath { get; set; } = string.Empty;

        public string UploadedByStaffId { get; set; } = string.Empty;

        public bool IsApprovedByAdmin { get; set; } = false;

        public string Description { get; set; } = string.Empty;

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}