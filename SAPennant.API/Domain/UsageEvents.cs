namespace SAPennant.API.Domain;

public static class UsageEvents
{
    private static readonly TimeZoneInfo Adelaide = FindAdelaide();

    private static TimeZoneInfo FindAdelaide()
    {
        // IANA id works on Windows with ICU; the Windows id is the fallback.
        try { return TimeZoneInfo.FindSystemTimeZoneById("Australia/Adelaide"); }
        catch (TimeZoneNotFoundException) { return TimeZoneInfo.FindSystemTimeZoneById("Cen. Australia Standard Time"); }
    }

    public static DateOnly AdelaideDay(DateTime utc) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utc, Adelaide));

    /// Collapses the detail the client logs into the thing worth counting:
    /// the search term without its result count, the player without where
    /// they were opened from.
    public static string NormaliseDetail(string eventName, string detail)
    {
        var cut = eventName switch
        {
            "search" => detail.IndexOf(" → ", StringComparison.Ordinal),
            "player" => detail.IndexOf(" (via ", StringComparison.Ordinal),
            _ => -1,
        };
        if (cut >= 0) detail = detail[..cut];
        return eventName == "search" ? detail.Trim().Trim('"').ToLowerInvariant() : detail.Trim();
    }
}
