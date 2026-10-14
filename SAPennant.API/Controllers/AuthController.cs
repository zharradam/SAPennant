using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SAPennant.API.Services;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<AuthController> _logger;
    private readonly SettingsService _settings;

    public AuthController(IConfiguration config, ILogger<AuthController> logger, SettingsService settings)
    {
        _config = config;
        _logger = logger;
        _settings = settings;
    }

    // The admin's own browsing is left out of the usage stats. One id per
    // browser the admin has logged in from, comma-separated.
    public const string AdminVisitorIdsKey = "AdminVisitorIds";

    private async Task RememberAdminVisitorAsync(string? visitorId)
    {
        if (visitorId == null || !System.Text.RegularExpressions.Regex.IsMatch(visitorId, "^[a-z0-9]{1,12}$")) return;
        var ids = (await _settings.GetStringAsync(AdminVisitorIdsKey) ?? "").Split(',', StringSplitOptions.RemoveEmptyEntries);
        if (ids.Contains(visitorId)) return;
        await _settings.SetStringAsync(AdminVisitorIdsKey, string.Join(',', ids.Append(visitorId)));
    }

    private static bool SameSecret(string? given, string expected) =>
        given != null && System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(given), Encoding.UTF8.GetBytes(expected));

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString()
            ?? HttpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? "unknown";

        var validUsername = _config["Admin:Username"];
        var validPassword = _config["Admin:Password"];

        // Fail closed if the admin credentials aren't configured, otherwise an
        // empty login would match the empty defaults.
        var configured = !string.IsNullOrEmpty(validUsername) && !string.IsNullOrEmpty(validPassword);
        if (!configured || !SameSecret(request.Username, validUsername!) || !SameSecret(request.Password, validPassword!))
        {
            _logger.LogWarning("Failed admin login attempt. Username: {Username} IP: {IP}",
                request.Username, ip);
            return Unauthorized(new { message = "Invalid credentials" });
        }

        _logger.LogInformation("Successful admin login. Username: {Username} IP: {IP}",
            request.Username, ip);

        await RememberAdminVisitorAsync(request.VisitorId);

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _config["Jwt:Issuer"],
            audience: _config["Jwt:Audience"],
            claims: new[] { new Claim(ClaimTypes.Name, request.Username) },
            expires: DateTime.UtcNow.AddHours(8),
            signingCredentials: creds
        );

        return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? VisitorId { get; set; }
}