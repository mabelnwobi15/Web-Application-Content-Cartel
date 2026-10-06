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

        public AccountController(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
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
                    var error =
                        await response.Content
                            .ReadFromJsonAsync<ApiResponse>();

                    ModelState.AddModelError(
                        "",
                        error?.Message ??
                        "Registration failed."
                    );

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
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "The Content Cartel API is not running."
                );

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
                    var error =
                        await response.Content
                            .ReadFromJsonAsync<ApiResponse>();

                    ModelState.AddModelError(
                        "",
                        error?.Message ??
                        "Invalid email or password."
                    );

                    return View(model);
                }

                var result =
                    await response.Content
                        .ReadFromJsonAsync<LoginResponse>();

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
            catch (HttpRequestException)
            {
                ModelState.AddModelError(
                    "",
                    "The Content Cartel API is not running."
                );

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