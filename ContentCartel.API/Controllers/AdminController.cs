using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/admin")]
    public class AdminController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public AdminController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // GET: api/admin/staff
        [HttpGet("staff")]
        public async Task<IActionResult> GetStaff()
        {
            try
            {
                var users =
                    await _firebaseService.GetAsync<
                        Dictionary<string, UserProfile>
                    >("users");

                if (users == null || users.Count == 0)
                {
                    return Ok(new List<AdminStaffResponse>());
                }

                var staff = users
                    .Where(x =>
                        x.Value.Role.Equals(
                            "Admin",
                            StringComparison.OrdinalIgnoreCase
                        )
                        ||
                        x.Value.Role.Equals(
                            "Staff",
                            StringComparison.OrdinalIgnoreCase
                        )
                        ||
                        x.Value.Role.Equals(
                            "Manager",
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    .Select(x => new AdminStaffResponse
                    {
                        Id = x.Key,
                        FullName = x.Value.FullName,
                        Email = x.Value.Email,
                        Role = x.Value.Role,
                        Status = "Active",
                        BiometricRegistered =
                            x.Value.BiometricEnabled
                    })
                    .ToList();

                return Ok(staff);
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = ex.Message
                    }
                );
            }
        }
    }

    public class AdminStaffResponse
    {
        public string Id { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public string Status { get; set; } = "Active";

        public bool BiometricRegistered { get; set; }
    }
}