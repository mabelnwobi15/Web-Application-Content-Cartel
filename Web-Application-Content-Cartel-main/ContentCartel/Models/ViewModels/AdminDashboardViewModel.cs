namespace ContentCartel.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }

        public int TotalQuotes { get; set; }
        public int PendingQuotes { get; set; }

        public decimal Revenue { get; set; }
        public int ActiveClients { get; set; }

        public List<AdminBookingViewModel> RecentBookings { get; set; } = new();
        public List<AdminQuoteViewModel> RecentQuotes { get; set; } = new();
    }

    public class AdminBookingViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // ADD THIS
        public string ServiceId { get; set; } = string.Empty;

        public string ServiceName { get; set; } = string.Empty;

        public DateTime BookingDate { get; set; }
        public string BookingTime { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal RemainingBalance { get; set; }

        public string Status { get; set; } = "Pending";
        public string PaymentStatus { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }
    }

    public class AdminQuoteViewModel
    {
        public string Id { get; set; } = string.Empty;

        public string UserId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string Service { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public decimal Budget { get; set; }

        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }
    }
}