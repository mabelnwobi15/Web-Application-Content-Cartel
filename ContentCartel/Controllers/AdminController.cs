using ContentCartel.Models;
using ContentCartel.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Json;
using System.Text.RegularExpressions;


namespace ContentCartel.Controllers
{

    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {

        private readonly HttpClient _httpClient;

        public AdminController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClient =
                httpClientFactory.CreateClient("ContentCartelAPI");
        }


        [HttpGet]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        // Calls the API and returns the list, or null if the call failed.
        private async Task<List<T>?> TryGetListAsync<T>(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                return await response.Content
                           .ReadFromJsonAsync<List<T>>()
                       ?? new List<T>();
            }
            catch
            {
                return null;
            }
        }

        private static bool IsStatus(string? value, string expected)
        {
            return string.Equals(
                value?.Trim(),
                expected,
                StringComparison.OrdinalIgnoreCase
            );
        }

        // Quotes store the budget as text (e.g. "R5 000 - R10 000").
        // Pull out the first number so the dashboard can show an amount.
        private static decimal BudgetAmount(QuoteRequest quote)
        {
            if (quote.EstimatedPrice > 0)
            {
                return quote.EstimatedPrice;
            }

            if (quote.EstimatedAmount.HasValue &&
                quote.EstimatedAmount.Value > 0)
            {
                return quote.EstimatedAmount.Value;
            }

            if (!string.IsNullOrWhiteSpace(quote.Budget))
            {
                var match = Regex.Match(
                    quote.Budget.Replace(" ", "").Replace(",", ""),
                    @"\d+(\.\d+)?"
                );

                if (match.Success &&
                    decimal.TryParse(
                        match.Value,
                        System.Globalization.NumberStyles.Number,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var amount))
                {
                    return amount;
                }
            }

            return 0m;
        }

        // The API returns ContactPersonName / EstimatedPrice; the admin
        // quote views read CustomerName / EstimatedAmount. Fill both.
        private static void NormaliseQuote(QuoteRequest quote)
        {
            if (string.IsNullOrWhiteSpace(quote.CustomerName))
            {
                quote.CustomerName = quote.ContactPersonName;
            }

            if (!quote.EstimatedAmount.HasValue ||
                quote.EstimatedAmount.Value <= 0)
            {
                quote.EstimatedAmount =
                    quote.EstimatedPrice > 0
                        ? quote.EstimatedPrice
                        : null;
            }
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var model = new AdminDashboardViewModel();

            var bookings = await TryGetListAsync<Booking>("api/bookings");
            var invoices = await TryGetListAsync<Invoice>("api/invoices");
            var quotes = await TryGetListAsync<QuoteRequest>("api/quotes");

            var failed = new List<string>();

            if (bookings == null) { failed.Add("bookings"); bookings = new(); }
            if (invoices == null) { failed.Add("invoices"); invoices = new(); }
            if (quotes == null) { failed.Add("quotations"); quotes = new(); }

            if (failed.Any())
            {
                TempData["ErrorMessage"] =
                    "Unable to load " + string.Join(", ", failed) +
                    " from the API. Please try again.";
            }

            // ---------------- STAT CARDS ----------------

            model.TotalBookings = bookings.Count;

            model.PendingBookings =
                bookings.Count(b => IsStatus(b.Status, "Pending"));

            model.TotalQuotes = quotes.Count;

            model.PendingQuotes =
                quotes.Count(q => IsStatus(q.Status, "Pending"));

            // Revenue = everything clients have actually paid on invoices.
            model.Revenue = invoices.Sum(i => i.AmountPaid);

            model.ActiveClients =
                bookings.Select(b => b.UserId)
                    .Concat(invoices.Select(i => i.ClientId))
                    .Concat(quotes.Select(q => q.UserId ?? string.Empty))
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Count();

            // ---------------- RECENT BOOKINGS ----------------

            model.RecentBookings = bookings
                .OrderByDescending(b => b.CreatedAt)
                .Take(5)
                .Select(b => new AdminBookingViewModel
                {
                    Id = b.Id,
                    UserId = b.UserId,
                    CustomerName = b.CustomerName,
                    Email = b.Email,
                    Phone = b.Phone ?? string.Empty,
                    ServiceId = b.ServiceId,
                    ServiceName = b.ServiceName,
                    BookingDate = b.BookingDate,
                    BookingTime = b.BookingTime,
                    TotalAmount = b.TotalAmount,
                    DepositAmount = b.DepositAmount,
                    RemainingBalance = b.RemainingBalance,
                    Status = b.Status,
                    PaymentStatus = b.PaymentStatus,
                    CreatedAt = b.CreatedAt
                })
                .ToList();

            // ---------------- RECENT QUOTATIONS ----------------

            model.RecentQuotes = quotes
                .OrderByDescending(q => q.CreatedAt)
                .Take(5)
                .Select(q => new AdminQuoteViewModel
                {
                    Id = q.Id ?? string.Empty,
                    UserId = q.UserId ?? string.Empty,
                    Name = q.ContactPersonName
                           ?? q.CustomerName
                           ?? q.BrandName
                           ?? string.Empty,
                    Email = q.Email ?? string.Empty,
                    Service = q.MediaType
                              ?? q.ServiceName
                              ?? string.Empty,
                    Description = q.CampaignGoals
                                  ?? q.Description
                                  ?? string.Empty,
                    Budget = BudgetAmount(q),
                    Status = q.Status,
                    CreatedAt = q.CreatedAt
                })
                .ToList();

            return View(model);
        }


        // AI LOYALTY / DISCOUNTS


        [HttpGet]
        public IActionResult DiscountLoyalty()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveDiscount(string clientName)
        {
            TempData["SuccessMessage"] =
                $"Discount approved for {clientName}.";

            return RedirectToAction(nameof(DiscountLoyalty));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult RejectDiscount(string clientName)
        {
            TempData["SuccessMessage"] =
                $"Discount recommendation rejected for {clientName}.";

            return RedirectToAction(nameof(DiscountLoyalty));
        }

        // INVOICES


        [HttpGet]
        public async Task<IActionResult> Invoices()
        {
            var invoices = await TryGetListAsync<Invoice>("api/invoices");

            if (invoices == null)
            {
                TempData["ErrorMessage"] =
                    "Unable to load invoices from the API. Please try again.";

                invoices = new List<Invoice>();
            }

            return View(invoices);
        }

        // ------------------------------------------------------------
        // INVOICE DETAILS (admin can open ANY client's invoice)
        // ------------------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> InvoiceDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/invoices/{Uri.EscapeDataString(id)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var invoice =
                    await response.Content.ReadFromJsonAsync<Invoice>();

                if (invoice == null)
                {
                    return NotFound();
                }

                invoice.InvoiceId = id;

                return View(invoice);
            }
            catch
            {
                return NotFound();
            }
        }

        // The "Payment" button on the invoices list opens the invoice,
        // where the record-payment form lives.
        [HttpGet]
        public IActionResult RecordPayment(string id)
        {
            return RedirectToAction(nameof(InvoiceDetails), new { id });
        }

        // ------------------------------------------------------------
        // RECORD A PAYMENT (EFT / cash / card received by the admin)
        // ------------------------------------------------------------

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RecordPayment(
            string invoiceId,
            decimal amount)
        {
            if (string.IsNullOrWhiteSpace(invoiceId))
            {
                return NotFound();
            }

            if (amount <= 0)
            {
                TempData["ErrorMessage"] =
                    "Enter a payment amount greater than zero.";

                return RedirectToAction(
                    nameof(InvoiceDetails),
                    new { id = invoiceId }
                );
            }

            try
            {
                var invoiceResponse = await _httpClient.GetAsync(
                    $"api/invoices/{Uri.EscapeDataString(invoiceId)}"
                );

                if (!invoiceResponse.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var invoice =
                    await invoiceResponse.Content
                        .ReadFromJsonAsync<Invoice>();

                if (invoice == null)
                {
                    return NotFound();
                }

                // Never take more than what is still owed.
                if (amount > invoice.BalanceDue)
                {
                    amount = invoice.BalanceDue;
                }

                if (amount <= 0)
                {
                    TempData["ErrorMessage"] =
                        "This invoice has no outstanding balance.";

                    return RedirectToAction(
                        nameof(InvoiceDetails),
                        new { id = invoiceId }
                    );
                }

                // The API expects an object: { "amount": 123.45 }
                var paymentResponse = await _httpClient.PutAsJsonAsync(
                    $"api/invoices/{Uri.EscapeDataString(invoiceId)}/payment",
                    new { Amount = amount }
                );

                if (!paymentResponse.IsSuccessStatusCode)
                {
                    var error =
                        await paymentResponse.Content.ReadAsStringAsync();

                    TempData["ErrorMessage"] =
                        "Unable to record payment. " + error;

                    return RedirectToAction(
                        nameof(InvoiceDetails),
                        new { id = invoiceId }
                    );
                }

                TempData["SuccessMessage"] =
                    $"Payment of R {amount:N2} recorded for invoice " +
                    $"{invoice.InvoiceNumber}.";

                return RedirectToAction(
                    nameof(InvoiceDetails),
                    new { id = invoiceId }
                );
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to record payment: " + ex.Message;

                return RedirectToAction(
                    nameof(InvoiceDetails),
                    new { id = invoiceId }
                );
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessRefund(string invoiceId)
        {
            TempData["SuccessMessage"] =
                $"Refund request processed for invoice {invoiceId}.";

            return RedirectToAction(nameof(Invoices));
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendInvoiceReminder(string invoiceId)
        {
            TempData["SuccessMessage"] =
                $"Payment reminder sent for invoice {invoiceId}.";

            return RedirectToAction(nameof(Invoices));
        }


        // ============================================================
        // ALL BOOKINGS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Bookings()
        {
            var bookings = await TryGetListAsync<Booking>("api/bookings");

            if (bookings == null)
            {
                TempData["ErrorMessage"] =
                    "Unable to load bookings from the API. Please try again.";

                bookings = new List<Booking>();
            }

            return View(bookings);
        }


        // ============================================================
        // ALL QUOTATIONS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Quotations()
        {
            var quotes = await TryGetListAsync<QuoteRequest>("api/quotes");

            if (quotes == null)
            {
                TempData["ErrorMessage"] =
                    "Unable to load quotations from the API. Please try again.";

                quotes = new List<QuoteRequest>();
            }

            quotes.ForEach(NormaliseQuote);

            return View(quotes);
        }

        [HttpGet]
        public async Task<IActionResult> QuoteDetails(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var response = await _httpClient.GetAsync(
                    $"api/quotes/{Uri.EscapeDataString(id)}"
                );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var quote =
                    await response.Content.ReadFromJsonAsync<QuoteRequest>();

                if (quote == null)
                {
                    return NotFound();
                }

                quote.Id = id;
                NormaliseQuote(quote);

                return View(quote);
            }
            catch
            {
                return NotFound();
            }
        }


        // GALLERY


        [HttpGet]
        public IActionResult Gallery()
        {
            var galleryItems = new List<GalleryItem>();

            return View(galleryItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ApproveGalleryItem(string id)
        {
            TempData["SuccessMessage"] =
                $"Gallery item {id} approved.";

            return RedirectToAction(nameof(Gallery));
        }


        // EDIT BOOKING


        [HttpGet]
        public async Task<IActionResult> EditBooking(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"api/bookings/{id}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var booking =
                    await response.Content
                        .ReadFromJsonAsync<Booking>();

                if (booking == null)
                {
                    return NotFound();
                }

                booking.Id = id;

                return View(booking);
            }
            catch
            {
                return NotFound();
            }
        }



        // SAVE EDITED BOOKING


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBooking(
            Booking booking)
        {
            // ServiceName / User are not fields on this form; the API
            // fills ServiceName from ServiceId, so don't let them block saving.
            ModelState.Remove(nameof(Booking.ServiceName));
            ModelState.Remove(nameof(Booking.User));

            if (!ModelState.IsValid)
            {
                return View(booking);
            }

            try
            {
                var response =
                    await _httpClient.PutAsJsonAsync(
                        $"api/bookings/{booking.Id}",
                        booking
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        "Unable to update booking. " + error
                    );

                    return View(booking);
                }

                TempData["SuccessMessage"] =
                    "Booking updated successfully.";

                return RedirectToAction(
                    nameof(Bookings)
                );
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "The booking service is currently unavailable."
                );

                return View(booking);
            }
        }


        // RESCHEDULE BOOKING


        [HttpGet]
        public async Task<IActionResult> RescheduleBooking(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"api/bookings/{id}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var booking =
                    await response.Content
                        .ReadFromJsonAsync<Booking>();

                if (booking == null)
                {
                    return NotFound();
                }

                booking.Id = id;

                return View(booking);
            }
            catch
            {
                return NotFound();
            }
        }



        // SAVE RESCHEDULED BOOKING


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RescheduleBooking(
            Booking booking)
        {
            // ServiceName / User are not fields on this form; the API
            // fills ServiceName from ServiceId, so don't let them block saving.
            ModelState.Remove(nameof(Booking.ServiceName));
            ModelState.Remove(nameof(Booking.User));

            if (!ModelState.IsValid)
            {
                return View(booking);
            }

            try
            {
                var response =
                    await _httpClient.PutAsJsonAsync(
                        $"api/bookings/{booking.Id}",
                        booking
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        "Unable to reschedule booking. " + error
                    );

                    return View(booking);
                }

                TempData["SuccessMessage"] =
                    "Booking rescheduled successfully.";

                return RedirectToAction(
                    nameof(Bookings)
                );
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "The booking service is currently unavailable."
                );

                return View(booking);
            }
        }


        // CANCEL BOOKING


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelBooking(
            string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var response =
                    await _httpClient.PatchAsync(
                        $"api/bookings/{id}/cancel",
                        null
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    TempData["ErrorMessage"] =
                        "Unable to cancel booking. " + error;

                    return RedirectToAction(
                        nameof(Bookings)
                    );
                }

                TempData["SuccessMessage"] =
                    "Booking cancelled successfully.";

                return RedirectToAction(
                    nameof(Bookings)
                );
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The booking service is currently unavailable.";

                return RedirectToAction(
                    nameof(Bookings)
                );
            }
        }

        // ============================================================
        // STAFF
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Staff()
        {
            try
            {
                var response = await _httpClient.GetAsync("api/admin/staff");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load staff members.";

                    return View(new List<AdminStaffViewModel>());
                }

                var staff = await response.Content
                    .ReadFromJsonAsync<List<AdminStaffViewModel>>();

                return View(
                    staff ?? new List<AdminStaffViewModel>()
                );
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The staff service is currently unavailable.";

                return View(
                    new List<AdminStaffViewModel>()
                );
            }
        }


        // ADD STAFF - DISPLAY FORM


        [HttpGet]
        public IActionResult AddStaff()
        {
            return View(new AddStaffViewModel());
        }



        // ADD STAFF - SUBMIT FORM


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStaff(
            AddStaffViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var request = new
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    Phone = model.Phone,
                    Password = model.Password,
                    Role = model.Role,
                    BiometricEnabled = model.BiometricEnabled
                };

                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/auth/create-staff",
                        request
                    );

                var result =
                    await response.Content
                        .ReadFromJsonAsync<ApiResponse>();

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError(
                        "",
                        result?.Message ??
                        "Unable to create staff member."
                    );

                    return View(model);
                }

                TempData["SuccessMessage"] =
                    result?.Message ??
                    "Staff member added successfully.";

                TempData["PlayNotificationSound"] = "true";

                return RedirectToAction(
                    nameof(Staff)
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Unable to create staff member: {ex.Message}"
                );

                return View(model);
            }
        }

        private class ApiResponse
        {
            public bool Success { get; set; }

            public string? Message { get; set; }
        }

        // SERVICES & PRICING


        [HttpGet]
        public async Task<IActionResult> ServicesPricing()
        {
            try
            {
                var response =
                    await _httpClient.GetAsync("api/services");

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to load services.";

                    return View(new List<Service>());
                }

                var services =
                    await response.Content
                        .ReadFromJsonAsync<List<Service>>();

                return View(
                    services ?? new List<Service>()
                );
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The service API is currently unavailable.";

                return View(new List<Service>());
            }
        }


        [HttpGet]
        public IActionResult AddService()
        {
            return View(new Service());
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddService(Service model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.CreatedAt = DateTime.UtcNow;
                model.IsActive = true;

                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/services",
                        model
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        $"Unable to create service: {error}"
                    );

                    return View(model);
                }

                TempData["SuccessMessage"] =
                    "Service added successfully.";

                return RedirectToAction(
                    nameof(ServicesPricing)
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Unable to create service: {ex.Message}"
                );

                return View(model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> EditService(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return NotFound();
            }

            try
            {
                var response =
                    await _httpClient.GetAsync(
                        $"api/services/{id}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Service not found.";

                    return RedirectToAction(
                        nameof(ServicesPricing)
                    );
                }

                var service =
                    await response.Content
                        .ReadFromJsonAsync<Service>();

                if (service == null)
                {
                    return NotFound();
                }

                return View(service);
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Unable to load service.";

                return RedirectToAction(
                    nameof(ServicesPricing)
                );
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(
            string id,
            Service model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                model.ServiceId = id;

                var response =
                    await _httpClient.PutAsJsonAsync(
                        $"api/services/{id}",
                        model
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        $"Unable to update service: {error}"
                    );

                    return View(model);
                }

                TempData["SuccessMessage"] =
                    "Service updated successfully.";

                return RedirectToAction(
                    nameof(ServicesPricing)
                );
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(
                    "",
                    $"Unable to update service: {ex.Message}"
                );

                return View(model);
            }
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(string id)
        {
            try
            {
                var response =
                    await _httpClient.DeleteAsync(
                        $"api/services/{id}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] =
                        "Unable to delete service.";

                    return RedirectToAction(
                        nameof(ServicesPricing)
                    );
                }

                TempData["SuccessMessage"] =
                    "Service deleted successfully.";

                return RedirectToAction(
                    nameof(ServicesPricing)
                );
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Unable to delete service.";

                return RedirectToAction(
                    nameof(ServicesPricing)
                );
            }
        }

        // ============================================================
        // REPORTS
        // ============================================================

        // Reports now live in ReportController (real data + PDF download).
        [HttpGet]
        public IActionResult Reports()
        {
            return RedirectToAction("Index", "Report");
        }

        [HttpGet]
        public IActionResult DownloadReport(string month)
        {
            return RedirectToAction("DownloadPdf", "Report", new { month });
        }

        // ============================================================
        // SETTINGS
        // ============================================================

        [HttpGet]
        public IActionResult Settings()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SaveSettings(
            string businessName,
            string supportEmail,
            bool biometricEnabled)
        {
            TempData["SuccessMessage"] =
                "Platform settings saved successfully.";

            return RedirectToAction(nameof(Settings));
        }
    }


}