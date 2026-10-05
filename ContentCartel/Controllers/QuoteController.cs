using ContentCartel.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;

namespace ContentCartel.Controllers
{
    public class QuoteController : Controller
    {
        private readonly HttpClient _httpClient;

        public QuoteController(
            IHttpClientFactory httpClientFactory)
        {
            _httpClient =
                httpClientFactory.CreateClient("ContentCartelAPI");
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View(new QuoteRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitQuote(
            QuoteRequest quote)
        {
            if (!ModelState.IsValid)
            {
                return View("Index", quote);
            }

            quote.Status = "Pending";
            quote.CreatedAt = DateTime.UtcNow;

            try
            {
                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/quotes",
                        quote
                    );

                if (!response.IsSuccessStatusCode)
                {
                    var error =
                        await response.Content.ReadAsStringAsync();

                    ModelState.AddModelError(
                        "",
                        "Unable to submit quote. " + error
                    );

                    return View("Index", quote);
                }

                var createdQuote =
                    await response.Content
                        .ReadFromJsonAsync<QuoteRequest>();

                if (createdQuote == null)
                {
                    ModelState.AddModelError(
                        "",
                        "Quote was submitted but could not be retrieved."
                    );

                    return View("Index", quote);
                }

                return RedirectToAction(
                    "QuoteSubmitted",
                    new
                    {
                        id = createdQuote.Id
                    }
                );
            }
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "The quote service is currently unavailable. Please make sure the ContentCartel API is running."
                );

                return View("Index", quote);
            }
        }

        [HttpGet]
        public async Task<IActionResult> QuoteSubmitted(
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
                        $"api/quotes/{id}"
                    );

                if (!response.IsSuccessStatusCode)
                {
                    return NotFound();
                }

                var quote =
                    await response.Content
                        .ReadFromJsonAsync<QuoteRequest>();

                if (quote == null)
                {
                    return NotFound();
                }

                quote.Id = id;

                return View(quote);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}