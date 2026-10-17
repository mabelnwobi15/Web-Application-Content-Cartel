namespace ContentCartel.API.Models
{
    public class Invoice
    {
        public string InvoiceId { get; set; } = string.Empty;
        public string InvoiceNumber { get; set; } = string.Empty;
        public string BookingId { get; set; } = string.Empty;

        public string ClientId { get; set; } = string.Empty;
        public string ClientName { get; set; } = string.Empty;
        public string ClientEmail { get; set; } = string.Empty;
        public string ClientPhone { get; set; } = string.Empty;

        public string ServiceId { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;

        public decimal TotalAmount { get; set; }
        public decimal DepositAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal BalanceDue { get; set; }

        public string PaymentStatus { get; set; } = "Pending";
        public string Status { get; set; } = "Issued";

        public DateTime IssueDate { get; set; } = DateTime.UtcNow;
        public DateTime? DueDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}