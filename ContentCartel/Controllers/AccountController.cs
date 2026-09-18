using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
