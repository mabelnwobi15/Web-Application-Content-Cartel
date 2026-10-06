using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/firebase")]
    public class FirebaseTestController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public FirebaseTestController(
            FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        [HttpGet("test")]
        public async Task<IActionResult> TestFirebase()
        {
            try
            {
                var data = new
                {
                    message = "Firebase is working.",
                    createdAt = DateTime.UtcNow
                };

                var id = await _firebaseService.CreateAsync(
                    "test",
                    data
                );

                return Ok(new
                {
                    success = true,
                    id = id,
                    message = "Firebase is working."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(
                    500,
                    new
                    {
                        success = false,
                        message = "Firebase connection failed.",
                        error = ex.Message
                    }
                );
            }
        }
    }
}