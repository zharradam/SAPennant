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
        public string? VisitorId { get; set; }
        public string? SessionId { get; set; }
    }

    [HttpPost]
    public IActionResult Log([FromBody] FrontendLogEntry entry)
    {
        // Prefer the browser-generated anonymous ids (stable per browser /
        // per session); fall back to the IP+UA hash for old cached clients.
        var vid = SanitizeId(entry.VisitorId);
        var sid = SanitizeId(entry.SessionId);
        var visitor = vid ?? VisitorTag();
        var tag = vid != null
            ? (sid != null ? $"v:{vid} s:{sid}" : $"v:{vid}")
            : visitor;

        var level = entry.Level?.ToLower();

        // Usage events: level "usage", Context carries the event name
        // (visit, tab, search, player, club, handicap, share) and Message
        // the detail. Rendered as one clean line per user action.
        if (level == "usage")
        {
            using (BeginFrontendScope("usage", visitor, sid))
                _logger.LogInformation("{Event:l} {Data:l} [{Tag:l}]",
                    entry.Context ?? "event", entry.Message, tag);
            return Ok();
        }

        using (BeginFrontendScope("frontend", visitor, sid))
        {
            if (level == "error")
                _logger.LogError("{Context:l}: {Message:l} [{Tag:l}]", entry.Context, entry.Message, tag);
            else if (level == "warn")
                _logger.LogWarning("{Context:l}: {Message:l} [{Tag:l}]", entry.Context, entry.Message, tag);
            else
                _logger.LogInformation("{Context:l}: {Message:l} [{Tag:l}]", entry.Context, entry.Message, tag);
        }
        return Ok();
    }

    /// Client-supplied ids go straight into log lines, so restrict them to
    /// short alphanumerics.
    private static string? SanitizeId(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        var clean = new string(id.Where(char.IsAsciiLetterOrDigit).Take(12).ToArray());
        return clean.Length == 0 ? null : clean;
    }

    private IDisposable? BeginFrontendScope(string category, string visitor, string? session) =>
        _logger.BeginScope(new Dictionary<string, object>
        {
            ["source"] = "frontend",
            ["category"] = category,
            ["visitor"] = visitor,
            ["session"] = session ?? ""
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
