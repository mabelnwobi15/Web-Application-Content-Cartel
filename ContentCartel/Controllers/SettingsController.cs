using ContentCartel.Helpers;
using ContentCartel.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContentCartel.Controllers
{
    [Authorize]
    public class SettingsController : Controller
    {
        private readonly HttpClient _httpClient;

        public SettingsController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("ContentCartelAPI");
        }

        private string? Uid => User.FindFirstValue(ClaimTypes.NameIdentifier);

        // ============================================================
        // SETTINGS PAGE (admin and client each get their own view)
        // GET: /Settings
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var model = await SettingsApi.LoadAsync(_httpClient, User);

            return View(model.IsAdmin ? "Admin" : "Client", model);
        }

        // ============================================================
        // SAVE PROFILE (name, phone, biometric for admins)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveProfile(
            string fullName,
            string? phone,
            bool? biometricEnabled)
        {
            var uid = Uid;

            if (string.IsNullOrWhiteSpace(uid))
            {
                return Challenge();
            }

            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"api/users/{Uri.EscapeDataString(uid)}",
                    new
                    {
                        FullName = fullName,
                        Phone = phone,
                        BiometricEnabled =
                            User.IsInRole("Admin") ? biometricEnabled : null
                    });

                if (!response.IsSuccessStatusCode)
                {
                    TempData["ErrorMessage"] = await ApiMessageAsync(
                        response, "Unable to update your profile.");

                    return RedirectToAction(nameof(Index));
                }

                // Keep the signed-in name in step with the profile.
                await RefreshNameClaimAsync(fullName.Trim());

                TempData["SuccessMessage"] = "Profile updated.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to update your profile: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // SAVE PREFERENCES (language, notifications, accessibility)
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePreferences(UserPreferences prefs)
        {
            var uid = Uid;

            if (string.IsNullOrWhiteSpace(uid))
            {
                return Challenge();
            }

            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    $"api/users/{Uri.EscapeDataString(uid)}/preferences",
                    prefs);

                TempData[response.IsSuccessStatusCode
                    ? "SuccessMessage"
                    : "ErrorMessage"] =
                    response.IsSuccessStatusCode
                        ? "Preferences saved."
                        : await ApiMessageAsync(
                            response, "Unable to save your preferences.");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to save your preferences: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // SAVE BUSINESS DETAILS (admin only)
        // ============================================================

        [HttpPost]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveBusiness(BusinessSettings business)
        {
            try
            {
                var response = await _httpClient.PutAsJsonAsync(
                    "api/settings/business",
                    business);

                TempData[response.IsSuccessStatusCode
                    ? "SuccessMessage"
                    : "ErrorMessage"] =
                    response.IsSuccessStatusCode
                        ? "Business details saved."
                        : await ApiMessageAsync(
                            response, "Unable to save business details.");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] =
                    "Unable to save business details: " + ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        // ============================================================
        // HELPERS
        // ============================================================

        private static async Task<string> ApiMessageAsync(
            HttpResponseMessage response,
            string fallback)
        {
            try
            {
                var body = await response.Content.ReadAsStringAsync();

                using var doc = System.Text.Json.JsonDocument.Parse(body);

                if (doc.RootElement.TryGetProperty("message", out var message))
                {
                    var text = message.GetString();

                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        return text;
                    }
                }
            }
            catch { }

            return fallback;
        }

        private async Task RefreshNameClaimAsync(string fullName)
        {
            var current = await HttpContext.AuthenticateAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            var claims = User.Claims
                .Where(c => c.Type != ClaimTypes.Name)
                .Append(new Claim(ClaimTypes.Name, fullName))
                .ToList();

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identity),
                current?.Properties);
        }
    }
}