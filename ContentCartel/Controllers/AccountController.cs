using ContentCartel.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContentCartel.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<AccountController> logger)
        {
            _logger = logger;
            _httpClient =
                httpClientFactory.CreateClient(
                    "ContentCartelAPI"
                );

            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.RecaptchaSiteKey =
                _configuration["Recaptcha:SiteKey"];

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            ViewBag.RecaptchaSiteKey =
                _configuration["Recaptcha:SiteKey"];

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var request = new
                {
                    fullName = model.FullName,
                    email = model.Email,
                    password = model.Password,
                    recaptchaToken = model.RecaptchaToken
                };

                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/auth/register",
                        request
                    );

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", await ReadApiErrorAsync(response, "Registration failed."));

                    return View(model);
                }

                return RedirectToAction(
                    "Login",
                    new
                    {
                        registered = true
                    }
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API not reachable");
                ModelState.AddModelError("", "The Content Cartel API is not reachable right now. Please try again in a minute.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The server took too long to respond (it may be waking up). Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account request failed");
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public IActionResult Login(
            bool registered = false)
        {
            ViewBag.RecaptchaSiteKey =
                _configuration["Recaptcha:SiteKey"];

            ViewBag.Registered = registered;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            ViewBag.RecaptchaSiteKey =
                _configuration["Recaptcha:SiteKey"];

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var request = new
                {
                    email = model.Email,
                    password = model.Password,
                    recaptchaToken = model.RecaptchaToken
                };

                var response =
                    await _httpClient.PostAsJsonAsync(
                        "api/auth/login",
                        request
                    );

                if (!response.IsSuccessStatusCode)
                {
                    ModelState.AddModelError("", await ReadApiErrorAsync(response, "Invalid email or password."));

                    return View(model);
                }

                LoginResponse? result = null;
                try
                {
                    result = await response.Content
                        .ReadFromJsonAsync<LoginResponse>();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Login response from API could not be read");
                }

                if (result == null ||
                    string.IsNullOrWhiteSpace(result.Uid))
                {
                    ModelState.AddModelError(
                        "",
                        "Unable to complete login."
                    );

                    return View(model);
                }

                // CREATE MVC LOGIN COOKIE

                var claims = new List<Claim>
                {
                    new Claim(
                        ClaimTypes.NameIdentifier,
                        result.Uid
                    ),

                    new Claim(
                        ClaimTypes.Email,
                        result.Email ?? model.Email
                    ),

                    new Claim(
                        ClaimTypes.Name,
                        result.FullName ?? "User"
                    ),

                    new Claim(
                        ClaimTypes.Role,
                        result.Role ?? "Client"
                    )
                };

                var identity =
                    new ClaimsIdentity(
                        claims,
                        CookieAuthenticationDefaults
                            .AuthenticationScheme
                    );

                var principal =
                    new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults
                        .AuthenticationScheme,
                    principal
                );

                // ADMIN → ADMIN DASHBOARD

                if (string.Equals(
                        result.Role,
                        "Admin",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(
                        "Dashboard",
                        "Admin"
                    );
                }

                // CLIENT → CLIENT DASHBOARD

                if (string.Equals(
                        result.Role,
                        "Client",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return RedirectToAction(
                        "Dashboard",
                        "Client"
                    );
                }

                // FALLBACK

                return RedirectToAction(
                    "Index",
                    "Home"
                );
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "API not reachable");
                ModelState.AddModelError("", "The Content Cartel API is not reachable right now. Please try again in a minute.");
                return View(model);
            }
            catch (TaskCanceledException)
            {
                ModelState.AddModelError("", "The server took too long to respond (it may be waking up). Please try again.");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account request failed");
                ModelState.AddModelError("", "Something went wrong. Please try again.");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults
                    .AuthenticationScheme
            );

            return RedirectToAction(
                "Index",
                "Home"
            );
        }

        private static async Task<string> ReadApiErrorAsync(HttpResponseMessage response, string fallback)
        {
            try
            {
                var error = await response.Content.ReadFromJsonAsync<ApiResponse>();
                if (!string.IsNullOrWhiteSpace(error?.Message)) return error.Message!;
            }
            catch { /* API returned a non-JSON error body */ }
            return fallback;
        }

        private class ApiResponse
        {
            public bool Success { get; set; }

            public string? Message { get; set; }
        }

        private class LoginResponse
        {
            public bool Success { get; set; }

            public string? Message { get; set; }

            public string? Uid { get; set; }

            public string? Email { get; set; }

            public string? FullName { get; set; }

            public string? Role { get; set; }

            public string? IdToken { get; set; }

            public string? RefreshToken { get; set; }
        }
    }
}
