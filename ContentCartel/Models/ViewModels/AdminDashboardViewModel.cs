namespace ContentCartel.Models.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalBookings { get; set; }
        public int PendingApprovalsCount { get; set; }
        public decimal MtdRevenue { get; set; }
        public int ActiveClientsCount { get; set; }
        public List<DiscountRecommendation> PendingRecommendations { get; set; } = new();
        public List<Booking> UpcomingBookings { get; set; } = new();
    }
}
