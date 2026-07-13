using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FCG.Usuario.WebApi.Controllers
{
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "UP",
                application = "FCG.Usuario.WebApi",
                date = DateTime.UtcNow
            });
        }

        [Authorize]
        [HttpGet("secure")]
        public IActionResult HealthSecure()
        {
            return Ok(new
            {
                status = "UP",
                authenticated = true,
                user = User.FindFirst(ClaimTypes.Name)?.Value,
                email = User.FindFirst(ClaimTypes.Email)?.Value
            });
        }
    }
}
