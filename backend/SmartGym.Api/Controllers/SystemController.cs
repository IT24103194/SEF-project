using Microsoft.AspNetCore.Mvc;

namespace SmartGym.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SystemController : ControllerBase
{
    private readonly IHostEnvironment _environment;

    public SystemController(IHostEnvironment environment)
    {
        _environment = environment;
    }

    [HttpGet("info")]
    public IActionResult GetSystemInfo()
    {
        return Ok(new
        {
            Application = "SmartGym Backend API",
            Version = "1.0.0",
            Environment = _environment.EnvironmentName,
            Status = "Online",
            Timestamp = DateTime.UtcNow,
            Roles = new[] { "Member", "Trainer", "Admin" },
            Components = new[]
            {
                "Supplier & Supplement Inventory Management",
                "Feedback & Facility Resolution",
                "Class Scheduling & Booking",
                "Membership & Goal Tracking"
            }
        });
    }
}
