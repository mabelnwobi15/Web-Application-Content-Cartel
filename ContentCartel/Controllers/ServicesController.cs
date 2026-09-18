using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class ServicesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
