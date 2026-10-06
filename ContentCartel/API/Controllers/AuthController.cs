using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Json;
using System.Text.Json;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;
        private readonly RecaptchaService _recaptchaService;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public AuthController(
            FirebaseService firebaseService,
            RecaptchaService recaptchaService,
            IConfiguration configuration,
            HttpClient httpClient)
        {
            _firebaseService = firebaseService;
            _recaptchaService = recaptchaService;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        // =========================================================
        // REGISTER
        // =========================================================

        [HttpPost("register")]
        public async Task<IActionResult> Register(
            [FromBody] RegisterRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Full name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password is required."
                });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password must be at least 6 characters."
                });
            }

            // -----------------------------------------------------
            // Verify reCAPTCHA
            // -----------------------------------------------------

            var recaptchaValid =
                await _recaptchaService.VerifyAsync(
                    request.RecaptchaToken,
                    "register");

            if (!recaptchaValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Security verification failed. Please try again."
                });
            }

            try
            {
                // -------------------------------------------------
                // Get Firebase Web API Key
                // -------------------------------------------------

                var apiKey = _configuration["Firebase:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Firebase API key is not configured."
                    });
                }

                // -------------------------------------------------
                // Create Firebase Authentication User
                // -------------------------------------------------

                var signUpUrl =
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

                var firebaseRequest = new
                {
                    email = request.Email,
                    password = request.Password,
                    returnSecureToken = true
                };

                var firebaseResponse =
                    await _httpClient.PostAsJsonAsync(
                        signUpUrl,
                        firebaseRequest);

                var firebaseResponseBody =
                    await firebaseResponse.Content.ReadAsStringAsync();

                if (!firebaseResponse.IsSuccessStatusCode)
                {
                    string message = "Registration failed.";

                    try
                    {
                        using var errorDocument =
                            JsonDocument.Parse(firebaseResponseBody);

                        var errorMessage =
                            errorDocument.RootElement
                                .GetProperty("error")
                                .GetProperty("message")
                                .GetString();

                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            message = errorMessage;
                        }
                    }
                    catch
                    {
                        // Keep default message
                    }

                    return BadRequest(new
                    {
                        success = false,
                        message
                    });
                }

                using var document =
                    JsonDocument.Parse(firebaseResponseBody);

                var root = document.RootElement;

                var uid =
                    root.GetProperty("localId").GetString();

                var email =
                    root.GetProperty("email").GetString();

                if (string.IsNullOrWhiteSpace(uid))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Firebase did not return a user ID."
                    });
                }

                // -------------------------------------------------
                // Create User Profile in Realtime Database
                // -------------------------------------------------

                var profile = new UserProfile
                {
                    Uid = uid,
                    FullName = request.FullName,
                    Email = email ?? request.Email,
                    Role = "Client",
                    CreatedAt = DateTime.UtcNow
                };

                await _firebaseService.UpdateAsync(
                    $"users/{uid}",
                    profile);

                // -------------------------------------------------
                // Return successful registration
                // -------------------------------------------------

                return Ok(new
                {
                    success = true,
                    uid = uid,
                    email = email ?? request.Email,
                    fullName = request.FullName,
                    role = "Client",
                    message = "Registration successful."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during registration.",
                    error = ex.Message
                });
            }
        }


        // =========================================================
        // LOGIN
        // =========================================================

        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password is required."
                });
            }

            // -----------------------------------------------------
            // Verify reCAPTCHA
            // -----------------------------------------------------

            var recaptchaValid =
                await _recaptchaService.VerifyAsync(
                    request.RecaptchaToken,
                    "login");

            if (!recaptchaValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Security verification failed. Please try again."
                });
            }

            try
            {
                // -------------------------------------------------
                // Get Firebase Web API Key
                // -------------------------------------------------

                var apiKey = _configuration["Firebase:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Firebase API key is not configured."
                    });
                }

                // -------------------------------------------------
                // Firebase Authentication Login
                // -------------------------------------------------

                var loginUrl =
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signInWithPassword?key={apiKey}";

                var firebaseRequest = new
                {
                    email = request.Email,
                    password = request.Password,
                    returnSecureToken = true
                };

                var firebaseResponse =
                    await _httpClient.PostAsJsonAsync(
                        loginUrl,
                        firebaseRequest);

                var firebaseResponseBody =
                    await firebaseResponse.Content.ReadAsStringAsync();

                if (!firebaseResponse.IsSuccessStatusCode)
                {
                    string message = "Invalid email or password.";

                    try
                    {
                        using var errorDocument =
                            JsonDocument.Parse(firebaseResponseBody);

                        var errorMessage =
                            errorDocument.RootElement
                                .GetProperty("error")
                                .GetProperty("message")
                                .GetString();

                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            message = errorMessage;
                        }
                    }
                    catch
                    {
                        // Keep default message
                    }

                    return Unauthorized(new
                    {
                        success = false,
                        message
                    });
                }

                // -------------------------------------------------
                // Read Firebase login response
                // -------------------------------------------------

                var loginResult =
                 JsonSerializer.Deserialize<FirebaseLoginResponse>(
                     firebaseResponseBody,
                     new JsonSerializerOptions
                     {
                         PropertyNameCaseInsensitive = true
                     });

                if (loginResult == null ||
                    string.IsNullOrWhiteSpace(loginResult.LocalId))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Invalid Firebase login response.",
                        firebaseResponse = firebaseResponseBody
                    });
                }

                // -------------------------------------------------
                // Get user's profile from Realtime Database
                // -------------------------------------------------

                var profile =
                    await _firebaseService.GetAsync<UserProfile>(
                        $"users/{loginResult.LocalId}");

                // -------------------------------------------------
                // IMPORTANT:
                // Get the user's role.
                //
                // If the profile does not exist, default to Client.
                // -------------------------------------------------

                var role = profile?.Role ?? "Client";

                // -------------------------------------------------
                // Return login response
                //
                // THIS is the important part:
                // role = profile?.Role ?? "Client"
                // -------------------------------------------------

                return Ok(new
                {
                    success = true,

                    uid = loginResult.LocalId,

                    email =
                        loginResult.Email ??
                        request.Email,

                    fullName =
                        profile?.FullName ??
                        loginResult.Email ??
                        request.Email,

                    role = role,

                    idToken = loginResult.IdToken,

                    refreshToken = loginResult.RefreshToken
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during login.",
                    error = ex.Message
                });
            }
        }

        [HttpPost("seed-admin")]
        public async Task<IActionResult> SeedAdmin()
        {
            const string email = "admin@contentcartel.test";
            const string password = "Admin@12345";
            const string fullName = "Content Cartel Admin";

            try
            {
                var apiKey = _configuration["Firebase:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Firebase API key is not configured."
                    });
                }

                // Create Firebase Authentication account
                var signUpUrl =
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

                var firebaseRequest = new
                {
                    email,
                    password,
                    returnSecureToken = true
                };

                var firebaseResponse =
                    await _httpClient.PostAsJsonAsync(
                        signUpUrl,
                        firebaseRequest);

                var responseBody =
                    await firebaseResponse.Content.ReadAsStringAsync();

                // If the account already exists, tell us instead of crashing.
                if (!firebaseResponse.IsSuccessStatusCode)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Admin account could not be created.",
                        firebaseResponse = responseBody
                    });
                }

                using var document =
                    JsonDocument.Parse(responseBody);

                var uid =
                    document.RootElement
                        .GetProperty("localId")
                        .GetString();

                if (string.IsNullOrWhiteSpace(uid))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Firebase did not return a UID."
                    });
                }

                // Create Admin profile in Realtime Database
                var profile = new UserProfile
                {
                    Uid = uid,
                    FullName = fullName,
                    Email = email,
                    Role = "Admin",
                    CreatedAt = DateTime.UtcNow
                };

                await _firebaseService.UpdateAsync(
                    $"users/{uid}",
                    profile);

                return Ok(new
                {
                    success = true,
                    message = "Admin account created successfully.",
                    email,
                    password,
                    uid,
                    role = "Admin"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the admin account.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // CREATE STAFF
        // =========================================================

        [HttpPost("create-staff")]
        public async Task<IActionResult> CreateStaff(
            [FromBody] 
        CreateStaffRequest request)
        {
            if (request == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request."
                });
            }

            if (string.IsNullOrWhiteSpace(request.FullName))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Full name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Email))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Email is required."
                });
            }

            if (string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password is required."
                });
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Password must be at least 6 characters."
                });
            }

            try
            {
                var apiKey = _configuration["Firebase:ApiKey"];

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Firebase API key is not configured."
                    });
                }

                // -----------------------------------------------------
                // Create Firebase Authentication account
                // -----------------------------------------------------

                var signUpUrl =
                    $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}";

                var firebaseRequest = new
                {
                    email = request.Email,
                    password = request.Password,
                    returnSecureToken = true
                };

                var firebaseResponse =
                    await _httpClient.PostAsJsonAsync(
                        signUpUrl,
                        firebaseRequest);

                var responseBody =
                    await firebaseResponse.Content.ReadAsStringAsync();

                if (!firebaseResponse.IsSuccessStatusCode)
                {
                    string message = "Staff account could not be created.";

                    try
                    {
                        using var errorDocument =
                            JsonDocument.Parse(responseBody);

                        var errorMessage =
                            errorDocument.RootElement
                                .GetProperty("error")
                                .GetProperty("message")
                                .GetString();

                        if (!string.IsNullOrWhiteSpace(errorMessage))
                        {
                            message = errorMessage;
                        }
                    }
                    catch
                    {
                        // Keep default message
                    }

                    return BadRequest(new
                    {
                        success = false,
                        message
                    });
                }

                // -----------------------------------------------------
                // Get Firebase UID
                // -----------------------------------------------------

                using var document =
                    JsonDocument.Parse(responseBody);

                var uid =
                    document.RootElement
                        .GetProperty("localId")
                        .GetString();

                if (string.IsNullOrWhiteSpace(uid))
                {
                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Firebase did not return a UID."
                    });
                }

                // -----------------------------------------------------
                // Save staff profile
                // -----------------------------------------------------

                var profile = new UserProfile
                {
                    Uid = uid,
                    FullName = request.FullName,
                    Email = request.Email,
                    Role = request.Role,
                    CreatedAt = DateTime.UtcNow
                };

                await _firebaseService.UpdateAsync(
                    $"users/{uid}",
                    profile);

                return Ok(new
                {
                    success = true,
                    uid,
                    fullName = request.FullName,
                    email = request.Email,
                    phone = request.Phone,
                    role = request.Role,
                    biometricEnabled = request.BiometricEnabled,
                    message = "Staff member added successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred while creating the staff account.",
                    error = ex.Message
                });
            }
        }

        // =========================================================
        // FIREBASE LOGIN RESPONSE MODEL
        // =========================================================

        private class FirebaseLoginResponse
        {
            [System.Text.Json.Serialization.JsonPropertyName("localId")]
            public string? LocalId { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("email")]
            public string? Email { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("idToken")]
            public string? IdToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("refreshToken")]
            public string? RefreshToken { get; set; }

            [System.Text.Json.Serialization.JsonPropertyName("expiresIn")]
            public string? ExpiresIn { get; set; }
        }

        private class FirebaseSignUpResponse
        {
            public string LocalId { get; set; } =
                string.Empty;

            public string IdToken { get; set; } =
                string.Empty;

            public string RefreshToken { get; set; } =
                string.Empty;

            public string ExpiresIn { get; set; } =
                string.Empty;
        }
    }
}