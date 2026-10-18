using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/settings/business")]
    public class BusinessSettingsController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public BusinessSettingsController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // GET: api/settings/business
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var settings =
                    await _firebaseService
                        .GetAsync<BusinessSettings>("settings/business");

                return Ok(settings ?? new BusinessSettings());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to load business settings.",
                    error = ex.Message
                });
            }
        }

        // PUT: api/settings/business
        [HttpPut]
        public async Task<IActionResult> Save([FromBody] BusinessSettings settings)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(settings.BusinessName))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Business name is required."
                    });
                }

                if (!string.IsNullOrWhiteSpace(settings.SupportEmail) &&
                    !settings.SupportEmail.Contains('@'))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Enter a valid support email."
                    });
                }

                await _firebaseService
                    .UpdateAsync("settings/business", settings);

                return Ok(settings);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to save business settings.",
                    error = ex.Message
                });
            }
        }
    }
}