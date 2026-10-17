using System.Collections.Generic;
using System.Linq;

namespace ContentCartel.Models.ViewModels
{
    public class ClientDashboardViewModel
    {
        public List<Booking> Bookings { get; set; } = new();

        public List<Invoice> Invoices { get; set; } = new();

        public List<QuoteRequest> Quotations { get; set; } = new();

        public decimal TotalPaid =>
            Invoices?.Sum(i => i.AmountPaid) ?? 0m;

        public decimal OutstandingBalance =>
            Invoices?.Sum(i => i.BalanceDue) ?? 0m;

        public int TotalBookings =>
            Bookings?.Count ?? 0;

        public int TotalInvoices =>
            Invoices?.Count ?? 0;

        public int TotalQuotations =>
            Quotations?.Count ?? 0;
    }
}