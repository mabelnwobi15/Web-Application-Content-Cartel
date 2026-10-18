using ContentCartel.API.Models;

namespace ContentCartel.API.Services
{
    // Creates and updates the invoice that belongs to a booking.
    // The invoice is stored at invoices/{bookingId}, so a booking can only
    // ever have one invoice and every call here is safe to repeat.
    public static class InvoiceHelper
    {
        public static async Task<Invoice> EnsureInvoiceAsync(
            FirebaseService firebase,
            Booking booking)
        {
            if (string.IsNullOrWhiteSpace(booking.Id))
            {
                throw new ArgumentException("Booking ID is required.");
            }

            var existing = await firebase.GetAsync<Invoice>(
                $"invoices/{booking.Id}"
            );

            if (existing != null)
            {
                existing.InvoiceId = booking.Id;
                return existing;
            }

            var invoice = new Invoice
            {
                InvoiceId = booking.Id,
                InvoiceNumber = BuildInvoiceNumber(booking.Id),
                BookingId = booking.Id,
                ClientId = booking.UserId ?? string.Empty,
                ClientName = booking.CustomerName,
                ClientEmail = booking.Email,
                ClientPhone = booking.Phone,
                ServiceId = booking.ServiceId,
                ServiceName = booking.ServiceName ?? string.Empty,
                TotalAmount = booking.TotalAmount,
                DepositAmount = booking.DepositAmount,
                AmountPaid = 0,
                BalanceDue = booking.TotalAmount,
                PaymentStatus = "Pending",
                Status = "Issued",
                IssueDate = DateTime.UtcNow,
                DueDate = booking.BookingDate,
                CreatedAt = DateTime.UtcNow
            };

            // If the deposit was already paid before the invoice existed,
            // start the invoice with that payment recorded.
            if (string.Equals(
                    booking.PaymentStatus,
                    "Paid",
                    StringComparison.OrdinalIgnoreCase))
            {
                invoice.AmountPaid = booking.DepositAmount;
            }

            Recalculate(invoice);

            await firebase.UpdateAsync($"invoices/{booking.Id}", invoice);

            return invoice;
        }

        // Records the booking deposit against the booking's invoice.
        public static async Task MarkDepositPaidAsync(
            FirebaseService firebase,
            Booking booking)
        {
            var invoice = await EnsureInvoiceAsync(firebase, booking);

            if (invoice.AmountPaid < invoice.DepositAmount)
            {
                invoice.AmountPaid = invoice.DepositAmount;

                Recalculate(invoice);

                await firebase.UpdateAsync(
                    $"invoices/{booking.Id}",
                    invoice
                );
            }
        }

        public static void Recalculate(Invoice invoice)
        {
            if (invoice.TotalAmount > 0 &&
                invoice.AmountPaid >= invoice.TotalAmount)
            {
                invoice.AmountPaid = invoice.TotalAmount;
                invoice.BalanceDue = 0;
                invoice.PaymentStatus = "Paid";
            }
            else if (invoice.AmountPaid > 0)
            {
                invoice.BalanceDue = invoice.TotalAmount - invoice.AmountPaid;
                invoice.PaymentStatus = "Partially Paid";
            }
            else
            {
                invoice.BalanceDue = invoice.TotalAmount;
                invoice.PaymentStatus = "Pending";
            }
        }

        private static string BuildInvoiceNumber(string bookingId)
        {
            var tail = new string(
                bookingId.Where(char.IsLetterOrDigit).ToArray()
            );

            if (tail.Length > 6)
            {
                tail = tail.Substring(tail.Length - 6);
            }

            return $"CC-{DateTime.UtcNow:yyyy}-{tail.ToUpperInvariant()}";
        }
    }
}