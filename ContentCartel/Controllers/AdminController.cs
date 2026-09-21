using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class AdminController : Controller
    {
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Dashboard));
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult DiscountLoyalty()
        {
            return View();
        }

        public IActionResult Invoices()
        {
            return View();
        }

        public IActionResult Gallery()
        {
            return View();
        }

        public IActionResult Staff()
        {
            return View();
        }

        public IActionResult ServicesPricing()
        {
            return View();
        }

        public IActionResult Reports()
        {
            return View();
        }

        public IActionResult Settings()
        {
            return View();
        }
    }
}