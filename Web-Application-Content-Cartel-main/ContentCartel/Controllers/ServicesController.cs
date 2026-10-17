using ContentCartel.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ContentCartel.Controllers
{
    public class ServicesController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public ServicesController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        // GET: /Services
        public async Task<IActionResult> Index()
        {
            try
            {
                var client =
                    _httpClientFactory
                        .CreateClient("ContentCartelAPI");

                var response =
                    await client.GetAsync("api/services");

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
    }
}