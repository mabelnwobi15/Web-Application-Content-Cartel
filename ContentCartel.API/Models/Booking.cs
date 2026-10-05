namespace ContentCartel.API.Models
{
    public class Booking
    {
        public string? Id { get; set; }

        public string? UserId { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string ServiceId { get; set; } = string.Empty;

        public string? ServiceName { get; set; }

        public DateTime BookingDate { get; set; }

        public string BookingTime { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }

        public decimal DepositAmount { get; set; }

        public decimal RemainingBalance { get; set; }

        public string Status { get; set; } = "Pending";

        public string PaymentStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}