using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class QuoteController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
