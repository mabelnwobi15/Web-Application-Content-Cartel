using ContentCartel.Models;
using ContentCartel.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Json;


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

        [HttpGet]
        public IActionResult Dashboard()
        {
            var model = new AdminDashboardViewModel();

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
        public IActionResult Invoices()
        {
            var invoices = new List<Invoice>();

            return View(invoices);
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
                    nameof(Dashboard)
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
                    nameof(Dashboard)
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
                        nameof(Dashboard)
                    );
                }

                TempData["SuccessMessage"] =
                    "Booking cancelled successfully.";

                return RedirectToAction(
                    nameof(Dashboard)
                );
            }
            catch (HttpRequestException)
            {
                TempData["ErrorMessage"] =
                    "The booking service is currently unavailable.";

                return RedirectToAction(
                    nameof(Dashboard)
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

        [HttpGet]
        public IActionResult Reports()
        {
            return View();
        }

        [HttpGet]
        public IActionResult DownloadReport(string month)
        {
            TempData["SuccessMessage"] =
                $"Report for {month} is ready for download.";

            return RedirectToAction(nameof(Reports));
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
