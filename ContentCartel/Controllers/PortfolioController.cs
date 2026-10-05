using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class PortfolioController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
