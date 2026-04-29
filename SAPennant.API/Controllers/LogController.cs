using Microsoft.AspNetCore.Mvc;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/log")]
public class LogController : ControllerBase
{
    private readonly ILogger<LogController> _logger;

    public LogController(ILogger<LogController> logger)
    {
        _logger = logger;
    }

    public class FrontendLogEntry
    {
        public string Level { get; set; } = "info";
        public string Message { get; set; } = "";
        public string? Context { get; set; }
    }

    [HttpPost]
    public IActionResult Log([FromBody] FrontendLogEntry entry)
    {
        using (_logger.BeginScope(new Dictionary<string, object> { ["source"] = "frontend" }))
        {
            var level = entry.Level?.ToLower();
            if (level == "error")
                _logger.LogError("{Context} {Message}", entry.Context, entry.Message);
            else if (level == "warn")
                _logger.LogWarning("{Context} {Message}", entry.Context, entry.Message);
            else
                _logger.LogInformation("{Context} {Message}", entry.Context, entry.Message);
        }
        return Ok();
    }
}