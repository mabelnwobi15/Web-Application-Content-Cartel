using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class ContactController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
