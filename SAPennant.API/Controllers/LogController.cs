using System.Security.Cryptography;
using System.Text;
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
        var visitor = VisitorTag();
        var level = entry.Level?.ToLower();

        // Usage events: level "usage", Context carries the event name
        // (visit, tab, search, player, club, handicap, share) and Message
        // the detail. Rendered as one clean line per user action.
        if (level == "usage")
        {
            using (BeginFrontendScope("usage", visitor))
                _logger.LogInformation("{Event:l} {Data:l} [{Visitor:l}]",
                    entry.Context ?? "event", entry.Message, visitor);
            return Ok();
        }

        using (BeginFrontendScope("frontend", visitor))
        {
            if (level == "error")
                _logger.LogError("{Context:l}: {Message:l} [{Visitor:l}]", entry.Context, entry.Message, visitor);
            else if (level == "warn")
                _logger.LogWarning("{Context:l}: {Message:l} [{Visitor:l}]", entry.Context, entry.Message, visitor);
            else
                _logger.LogInformation("{Context:l}: {Message:l} [{Visitor:l}]", entry.Context, entry.Message, visitor);
        }
        return Ok();
    }

    private IDisposable? BeginFrontendScope(string category, string visitor) =>
        _logger.BeginScope(new Dictionary<string, object>
        {
            ["source"] = "frontend",
            ["category"] = category,
            ["visitor"] = visitor
        });

    /// Short anonymous per-visitor tag (hash of IP + user agent) so distinct
    /// visitors are countable in Grafana without storing raw IPs.
    private string VisitorTag()
    {
        var ip = Request.Headers["X-Forwarded-For"].FirstOrDefault()?.Split(',')[0].Trim()
                 ?? HttpContext.Connection.RemoteIpAddress?.ToString()
                 ?? "unknown";
        var ua = Request.Headers.UserAgent.ToString();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{ip}|{ua}"));
        return Convert.ToHexString(hash)[..6].ToLower();
    }
}
