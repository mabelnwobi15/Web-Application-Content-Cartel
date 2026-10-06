using ContentCartel.API.Models;
using ContentCartel.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace ContentCartel.API.Controllers
{
    [ApiController]
    [Route("api/services")]
    public class ServiceController : ControllerBase
    {
        private readonly FirebaseService _firebaseService;

        public ServiceController(FirebaseService firebaseService)
        {
            _firebaseService = firebaseService;
        }

        // GET: api/services
        [HttpGet]
        public async Task<IActionResult> GetServices()
        {
            try
            {
                var services =
                    await _firebaseService
                        .GetAsync<Dictionary<string, Service>>("services");

                if (services == null || services.Count == 0)
                {
                    return Ok(new List<Service>());
                }

                var result = services
                    .Select(x =>
                    {
                        var service = x.Value;
                        service.ServiceId = x.Key;
                        return service;
                    })
                    .OrderBy(x => x.Category)
                    .ThenBy(x => x.Name)
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve services.",
                    error = ex.Message
                });
            }
        }

        // GET: api/services/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetService(string id)
        {
            try
            {
                var service =
                    await _firebaseService
                        .GetAsync<Service>($"services/{id}");

                if (service == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Service not found."
                    });
                }

                service.ServiceId = id;

                return Ok(service);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to retrieve service.",
                    error = ex.Message
                });
            }
        }

        // POST: api/services
        [HttpPost]
        public async Task<IActionResult> CreateService(
            [FromBody] Service service)
        {
            if (service == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid service data."
                });
            }

            if (string.IsNullOrWhiteSpace(service.Name))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Service name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(service.Category))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Service category is required."
                });
            }

            if (service.BasePrice < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Service price cannot be negative."
                });
            }

            try
            {
                service.ServiceId = string.Empty;
                service.CreatedAt = DateTime.UtcNow;

                var id = await _firebaseService.CreateAsync(
                    "services",
                    service
                );

                service.ServiceId = id;

                return Ok(new
                {
                    success = true,
                    message = "Service created successfully.",
                    service
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to create service.",
                    error = ex.Message
                });
            }
        }

        // PUT: api/services/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateService(
            string id,
            [FromBody] Service service)
        {
            if (service == null)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid service data."
                });
            }

            if (string.IsNullOrWhiteSpace(service.Name))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Service name is required."
                });
            }

            if (string.IsNullOrWhiteSpace(service.Category))
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Service category is required."
                });
            }

            if (service.BasePrice < 0)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Service price cannot be negative."
                });
            }

            try
            {
                var existing =
                    await _firebaseService
                        .GetAsync<Service>($"services/{id}");

                if (existing == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Service not found."
                    });
                }

                service.ServiceId = id;
                service.CreatedAt = existing.CreatedAt;

                await _firebaseService.UpdateAsync(
                    $"services/{id}",
                    service
                );

                return Ok(new
                {
                    success = true,
                    message = "Service updated successfully.",
                    service
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to update service.",
                    error = ex.Message
                });
            }
        }

        // DELETE: api/services/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteService(string id)
        {
            try
            {
                var existing =
                    await _firebaseService
                        .GetAsync<Service>($"services/{id}");

                if (existing == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Service not found."
                    });
                }

                await _firebaseService.DeleteAsync(
                    $"services/{id}"
                );

                return Ok(new
                {
                    success = true,
                    message = "Service deleted successfully."
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Unable to delete service.",
                    error = ex.Message
                });
            }
        }
    }
}