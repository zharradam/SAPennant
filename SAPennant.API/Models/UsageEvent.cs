using System.ComponentModel.DataAnnotations;

namespace SAPennant.API.Models;

/// One "usage" event posted by the browser (visit, tab, search, player, ...).
public class UsageEvent
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    /// Calendar day in Adelaide time, so daily counts match how the site is used.
    public DateOnly Day { get; set; }
    [MaxLength(40)]
    public string Event { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string Detail { get; set; } = string.Empty;
    [MaxLength(12)]
    public string? VisitorId { get; set; }
    [MaxLength(12)]
    public string? SessionId { get; set; }
    /// Hour of day in Adelaide time (0-23).
    public int Hour { get; set; }
    /// From the User-Agent; null for rows backfilled from Loki.
    public bool? IsMobile { get; set; }
}
