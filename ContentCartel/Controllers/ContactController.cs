using Microsoft.AspNetCore.Mvc;
using ContentCartel.Models;

namespace ContentCartel.Controllers
{
    public class ContactController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SendEmail(ContactMessage model)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", model);
            }

            try
            {
                // Add database persistence or email dispatch logic here
                // e.g., _context.ContactMessages.Add(model); await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Thank you! Your inquiry has been submitted successfully. We will get back to you shortly.";
                return RedirectToAction(nameof(Index));
            }
            catch
            {
                TempData["ErrorMessage"] = "An error occurred while sending your inquiry. Please try contacting us directly via WhatsApp.";
                return View("Index", model);
            }
        }
    }
}