using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private static readonly string[] TextSizes =
            { "small", "normal", "large", "xlarge" };

        private readonly FirebaseService _firebaseService;

        public UsersController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // ============================================================
        // GET PROFILE
        // GET: api/users/{uid}
        // ============================================================

        [HttpGet("{uid}")]
        public async Task<IActionResult> GetUser(string uid)
        {
            try
            {
                var user =
                    await _firebaseService
                        .GetAsync<UserProfile>($"users/{uid}");

                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found."
                    });
                }

                user.Uid = uid;

                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load profile.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // UPDATE PROFILE
        // PUT: api/users/{uid}
        // ============================================================

        [HttpPut("{uid}")]
        public async Task<IActionResult> UpdateUser(
            string uid,
            [FromBody] UpdateProfileRequest request)
        {
            try
            {
                var name = request.FullName?.Trim() ?? string.Empty;
                var phone = request.Phone?.Trim() ?? string.Empty;

                if (name.Length < 2 || name.Length > 100)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Enter your full name (2 to 100 characters)."
                    });
                }

                if (phone.Length > 30)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "That phone number is too long."
                    });
                }

                var existing =
                    await _firebaseService
                        .GetAsync<UserProfile>($"users/{uid}");

                if (existing == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "User not found."
                    });
                }

                var changes = new Dictionary<string, object?>
                {
                    ["FullName"] = name,
                    ["Phone"] = phone
                };

                if (request.BiometricEnabled.HasValue)
                {
                    changes["BiometricEnabled"] =
                        request.BiometricEnabled.Value;
                }

                await _firebaseService
                    .PatchAsync($"users/{uid}", changes);

                return Ok(new
                {
                    success = true,
                    message = "Profile updated."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to update profile.",
                    error = ex.Message
                });
            }
        }

        // ============================================================
        // PREFERENCES
        // GET / PUT: api/users/{uid}/preferences
        // ============================================================

        [HttpGet("{uid}/preferences")]
        public async Task<IActionResult> GetPreferences(string uid)
        {
            try
            {
                var prefs =
                    await _firebaseService
                        .GetAsync<UserPreferences>($"settings/users/{uid}");

                return Ok(prefs ?? new UserPreferences());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load preferences.",
                    error = ex.Message
                });
            }
        }

        [HttpPut("{uid}/preferences")]
        public async Task<IActionResult> SavePreferences(
            string uid,
            [FromBody] UserPreferences prefs)
        {
            try
            {
                if (!TextSizes.Contains(prefs.TextSize))
                {
                    prefs.TextSize = "normal";
                }

                if (string.IsNullOrWhiteSpace(prefs.Language))
                {
                    prefs.Language = "en-ZA";
                }

                if (string.IsNullOrWhiteSpace(prefs.TimeZone))
                {
                    prefs.TimeZone = "Africa/Johannesburg";
                }

                if (string.IsNullOrWhiteSpace(prefs.DateFormat))
                {
                    prefs.DateFormat = "dd MMM yyyy";
                }

                await _firebaseService
                    .UpdateAsync($"settings/users/{uid}", prefs);

                return Ok(prefs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to save preferences.",
                    error = ex.Message
                });
            }
        }
    }
}