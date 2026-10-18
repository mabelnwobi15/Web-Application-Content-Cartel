using ContentCartel.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Net.Http.Json;

namespace ContentCartel.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ReportController : Controller
    {
        private readonly HttpClient _httpClient;

        public ReportController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ContentCartelAPI");
        }

        // ============================================================
        // REPORT LIST
        // GET: /Report
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var data = await LoadAsync();

            if (data == null)
            {
                TempData["ErrorMessage"] =
                    "Unable to load activity from the API. Please try again.";

                return View(new List<ReportMonthRow>());
            }

            var tz = GetTimeZone();
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
            var current = new DateTime(nowLocal.Year, nowLocal.Month, 1);

            // Start at the earliest activity (max 12 months back).
            var all = data.Bookings.Select(b => ToLocal(b.CreatedAt, tz))
                .Concat(data.Quotes.Select(q => ToLocal(q.CreatedAt, tz)))
                .Concat(data.Invoices.Select(i => ToLocal(i.CreatedAt, tz)))
                .ToList();

            var earliest = all.Any()
                ? new DateTime(all.Min().Year, all.Min().Month, 1)
                : current;

            var limit = current.AddMonths(-11);
            if (earliest < limit) earliest = limit;

            var rows = new List<ReportMonthRow>();

            for (var m = current; m >= earliest; m = m.AddMonths(-1))
            {
                var slice = data.Slice(m, m.AddMonths(1), tz);

                rows.Add(new ReportMonthRow
                {
                    Key = m.ToString("yyyy-MM"),
                    Label = m.ToString("MMMM yyyy", CultureInfo.InvariantCulture),
                    Bookings = slice.Bookings.Count,
                    Quotes = slice.Quotes.Count,
                    Invoiced = slice.Invoices.Sum(i => i.TotalAmount),
                    Collected = slice.Invoices.Sum(i => i.AmountPaid),
                    IsCurrent = m == current
                });
            }

            ViewBag.AllBookings = data.Bookings.Count;
            ViewBag.AllQuotes = data.Quotes.Count;
            ViewBag.AllInvoiced = data.Invoices.Sum(i => i.TotalAmount);
            ViewBag.AllCollected = data.Invoices.Sum(i => i.AmountPaid);

            return View(rows);
        }

        // ============================================================
        // PDF
        // GET: /Report/DownloadPdf?month=2026-10   (or month=all)
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(string month)
        {
            if (string.IsNullOrWhiteSpace(month))
            {
                return BadRequest("Report month not specified.");
            }

            var data = await LoadAsync();

            if (data == null)
            {
                TempData["ErrorMessage"] =
                    "Unable to load activity from the API. Please try again.";

                return RedirectToAction(nameof(Index));
            }

            var tz = GetTimeZone();
            string title;
            string fileKey;
            ReportData slice;

            if (string.Equals(month, "all", StringComparison.OrdinalIgnoreCase))
            {
                title = "All activity";
                fileKey = "All_Activity";
                slice = data;
            }
            else if (DateTime.TryParseExact(
                         month, "yyyy-MM",
                         CultureInfo.InvariantCulture,
                         DateTimeStyles.None,
                         out var start))
            {
                title = start.ToString("MMMM yyyy", CultureInfo.InvariantCulture);
                fileKey = start.ToString("yyyy-MM");
                slice = data.Slice(start, start.AddMonths(1), tz);
            }
            else
            {
                return BadRequest("Invalid month. Use yyyy-MM, e.g. 2026-10.");
            }

            var pdf = BuildPdf(title, slice, tz);

            return File(pdf, "application/pdf", $"ContentCartel_Report_{fileKey}.pdf");
        }

        // ============================================================
        // DATA
        // ============================================================

        private async Task<ReportData?> LoadAsync()
        {
            var bookings = await TryGetListAsync<Booking>("api/bookings");
            var invoices = await TryGetListAsync<Invoice>("api/invoices");
            var quotes = await TryGetListAsync<QuoteRequest>("api/quotes");

            if (bookings == null && invoices == null && quotes == null)
            {
                return null;
            }

            return new ReportData
            {
                Bookings = bookings ?? new(),
                Invoices = invoices ?? new(),
                Quotes = quotes ?? new()
            };
        }

        private async Task<List<T>?> TryGetListAsync<T>(string url)
        {
            try
            {
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode) return null;

                return await response.Content.ReadFromJsonAsync<List<T>>()
                       ?? new List<T>();
            }
            catch
            {
                return null;
            }
        }

        private static TimeZoneInfo GetTimeZone()
        {
            foreach (var id in new[] { "Africa/Johannesburg", "South Africa Standard Time" })
            {
                try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
                catch { }
            }

            return TimeZoneInfo.Utc;
        }

        private static DateTime ToLocal(DateTime value, TimeZoneInfo tz)
        {
            var utc = value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);

            return TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
        }

        // ============================================================
        // PDF BUILDER
        // ============================================================

        private static string Money(decimal value) =>
            "R " + value.ToString("N2", CultureInfo.InvariantCulture);

        private static string S(string? value) =>
            string.IsNullOrWhiteSpace(value) ? "-" : value!;

        private static byte[] BuildPdf(string title, ReportData d, TimeZoneInfo tz)
        {
            QuestPDF.Settings.License = LicenseType.Community;

            var bookings = d.Bookings.OrderByDescending(b => b.CreatedAt).ToList();
            var invoices = d.Invoices.OrderByDescending(i => i.CreatedAt).ToList();
            var quotes = d.Quotes.OrderByDescending(q => q.CreatedAt).ToList();

            bool Is(string? v, string e) =>
                string.Equals(v?.Trim(), e, StringComparison.OrdinalIgnoreCase);

            var invoiced = invoices.Sum(i => i.TotalAmount);
            var collected = invoices.Sum(i => i.AmountPaid);
            var outstanding = invoices.Sum(i => i.BalanceDue);

            var generated = TimeZoneInfo
                .ConvertTimeFromUtc(DateTime.UtcNow, tz)
                .ToString("dd MMM yyyy HH:mm", CultureInfo.InvariantCulture);

            var clients = bookings.Select(b => b.UserId)
                .Concat(invoices.Select(i => i.ClientId))
                .Concat(quotes.Select(q => q.UserId ?? string.Empty))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count();

            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(col =>
                    {
                        col.Item().Text("CONTENT CARTEL")
                            .FontSize(9).Bold().FontColor(Colors.Grey.Darken1);

                        col.Item().Text("Business Report - " + title)
                            .FontSize(20).Bold();

                        col.Item().PaddingBottom(6)
                            .Text("Generated " + generated)
                            .FontColor(Colors.Grey.Darken1);

                        col.Item().LineHorizontal(1);
                    });

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        // ---------- SUMMARY ----------
                        col.Item().Text("Summary").FontSize(12).Bold();

                        col.Item().PaddingTop(4).Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3);
                                c.RelativeColumn(2);
                            });

                            void Row(string label, string value)
                            {
                                t.Cell().BorderBottom(0.5f)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4).Text(label);

                                t.Cell().BorderBottom(0.5f)
                                    .BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4).AlignRight().Text(value).Bold();
                            }

                            Row("Bookings", bookings.Count.ToString());
                            Row("  Pending", bookings.Count(b => Is(b.Status, "Pending")).ToString());
                            Row("  Confirmed / approved", bookings.Count(b => Is(b.Status, "Confirmed") || Is(b.Status, "Approved")).ToString());
                            Row("  Completed", bookings.Count(b => Is(b.Status, "Completed")).ToString());
                            Row("  Cancelled", bookings.Count(b => Is(b.Status, "Cancelled")).ToString());
                            Row("Quotations", quotes.Count.ToString());
                            Row("  Pending", quotes.Count(q => Is(q.Status, "Pending")).ToString());
                            Row("Invoices issued", invoices.Count.ToString());
                            Row("Total invoiced", Money(invoiced));
                            Row("Payments received", Money(collected));
                            Row("Outstanding balance", Money(outstanding));
                            Row("Active clients", clients.ToString());
                        });

                        // ---------- BOOKINGS ----------
                        AddTable(col, "Bookings",
                            new[] { "Created", "Client", "Service", "Booking date", "Total", "Status", "Payment" },
                            new float[] { 2, 3, 3, 2, 2, 2, 2 },
                            bookings.Select(b => new[]
                            {
                                ToLocal(b.CreatedAt, tz).ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                                S(b.CustomerName),
                                S(string.IsNullOrWhiteSpace(b.ServiceName) ? b.ServiceId : b.ServiceName),
                                b.BookingDate.ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                                Money(b.TotalAmount),
                                S(b.Status),
                                S(b.PaymentStatus)
                            }).ToList());

                        // ---------- INVOICES ----------
                        AddTable(col, "Invoices & payments",
                            new[] { "Invoice", "Client", "Total", "Paid", "Balance", "Status" },
                            new float[] { 3, 3, 2, 2, 2, 2 },
                            invoices.Select(i => new[]
                            {
                                S(i.InvoiceNumber),
                                S(i.ClientName),
                                Money(i.TotalAmount),
                                Money(i.AmountPaid),
                                Money(i.BalanceDue),
                                S(i.PaymentStatus)
                            }).ToList());

                        // ---------- QUOTATIONS ----------
                        AddTable(col, "Quotations",
                            new[] { "Created", "Brand", "Contact", "Media type", "Budget", "Status" },
                            new float[] { 2, 3, 3, 3, 3, 2 },
                            quotes.Select(q => new[]
                            {
                                ToLocal(q.CreatedAt, tz).ToString("dd MMM yyyy", CultureInfo.InvariantCulture),
                                S(q.BrandName),
                                S(string.IsNullOrWhiteSpace(q.ContactPersonName) ? q.CustomerName : q.ContactPersonName),
                                S(q.MediaType),
                                S(q.Budget),
                                S(q.Status)
                            }).ToList());
                    });

                    page.Footer().AlignCenter().Text(t =>
                    {
                        t.DefaultTextStyle(x => x.FontSize(8).FontColor(Colors.Grey.Darken1));
                        t.Span("Content Cartel  |  Page ");
                        t.CurrentPageNumber();
                        t.Span(" of ");
                        t.TotalPages();
                    });
                });
            }).GeneratePdf();
        }

        private static void AddTable(
            ColumnDescriptor col,
            string title,
            string[] headers,
            float[] widths,
            List<string[]> rows)
        {
            col.Item().PaddingTop(16)
                .Text($"{title} ({rows.Count})")
                .FontSize(12).Bold();

            if (rows.Count == 0)
            {
                col.Item().PaddingTop(4)
                    .Text("No records for this period.")
                    .FontColor(Colors.Grey.Darken1);

                return;
            }

            col.Item().PaddingTop(4).Table(table =>
            {
                table.ColumnsDefinition(c =>
                {
                    foreach (var w in widths) c.RelativeColumn(w);
                });

                table.Header(h =>
                {
                    foreach (var text in headers)
                    {
                        h.Cell().Background(Colors.Grey.Lighten3)
                            .Padding(4).Text(text).Bold().FontSize(8);
                    }
                });

                foreach (var row in rows)
                {
                    foreach (var cell in row)
                    {
                        table.Cell().BorderBottom(0.5f)
                            .BorderColor(Colors.Grey.Lighten2)
                            .Padding(4).Text(cell).FontSize(8);
                    }
                }
            });
        }
    }

    // ================================================================
    // SUPPORTING TYPES
    // ================================================================

    public class ReportMonthRow
    {
        public string Key { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Bookings { get; set; }
        public int Quotes { get; set; }
        public decimal Invoiced { get; set; }
        public decimal Collected { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class ReportData
    {
        public List<Booking> Bookings { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();
        public List<QuoteRequest> Quotes { get; set; } = new();

        // Items created from 'from' (inclusive) to 'to' (exclusive), local time.
        public ReportData Slice(DateTime from, DateTime to, TimeZoneInfo tz)
        {
            bool In(DateTime value)
            {
                var utc = value.Kind == DateTimeKind.Local
                    ? value.ToUniversalTime()
                    : DateTime.SpecifyKind(value, DateTimeKind.Utc);

                var local = TimeZoneInfo.ConvertTimeFromUtc(utc, tz);
                return local >= from && local < to;
            }

            return new ReportData
            {
                Bookings = Bookings.Where(b => In(b.CreatedAt)).ToList(),
                Invoices = Invoices.Where(i => In(i.CreatedAt)).ToList(),
                Quotes = Quotes.Where(q => In(q.CreatedAt)).ToList()
            };
        }
    }
}