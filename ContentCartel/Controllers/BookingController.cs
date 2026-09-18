using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class BookingController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
