using ContentCartel.Models;
using System.Net.Http.Json;
using System.Security.Claims;

namespace ContentCartel.Helpers
{
    public static class SettingsApi
    {
        // Loads profile + preferences (+ business details for admins).
        // Anything the API can't return falls back to the login claims / defaults.
        public static async Task<SettingsViewModel> LoadAsync(
            HttpClient http,
            ClaimsPrincipal user)
        {
            var uid = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
            var isAdmin = user.IsInRole("Admin");

            var model = new SettingsViewModel
            {
                IsAdmin = isAdmin,
                Profile = new UserProfile
                {
                    Uid = uid,
                    FullName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                    Email = user.FindFirstValue(ClaimTypes.Email) ?? string.Empty,
                    Role = isAdmin ? "Admin" : "Client"
                }
            };

            if (string.IsNullOrWhiteSpace(uid))
            {
                return model;
            }

            try
            {
                var profile = await http.GetFromJsonAsync<UserProfile>(
                    $"api/users/{Uri.EscapeDataString(uid)}");

                if (profile != null)
                {
                    profile.Uid = uid;
                    model.Profile = profile;
                }
            }
            catch { }

            try
            {
                var prefs = await http.GetFromJsonAsync<UserPreferences>(
                    $"api/users/{Uri.EscapeDataString(uid)}/preferences");

                if (prefs != null)
                {
                    model.Preferences = prefs;
                }
            }
            catch { }

            if (isAdmin)
            {
                try
                {
                    var business = await http.GetFromJsonAsync<BusinessSettings>(
                        "api/settings/business");

                    if (business != null)
                    {
                        model.Business = business;
                    }
                }
                catch { }
            }

            return model;
        }
    }
}