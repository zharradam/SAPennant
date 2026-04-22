using System.ComponentModel.DataAnnotations.Schema;

namespace SAPennant.API.Models;

public class PennantMatch
{
    public int Id { get; set; }
    public int Year { get; set; }
    public bool IsFinals { get; set; }
    public string Division { get; set; } = string.Empty;
    public string Pool { get; set; } = string.Empty;
    public string Round { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public string HomeClub { get; set; } = string.Empty;
    public string AwayClub { get; set; } = string.Empty;
    public string PlayerName { get; set; } = string.Empty;
    public string OpponentName { get; set; } = string.Empty;
    public string PlayerClub { get; set; } = string.Empty;
    public string OpponentClub { get; set; } = string.Empty;
    public string Result { get; set; } = string.Empty;
    public bool? PlayerWon { get; set; }
    public string Format { get; set; } = string.Empty;
    public string? PlayerHandicap { get; set; }
    public string? OpponentHandicap { get; set; }
    public string? Venue { get; set; }
    public bool IsSenior { get; set; }

    [NotMapped]
    public DateTime? SortDate
    {
        get
        {
            var normalised = (Date ?? "")
                .Replace("Sept ", "Sep ")
                .Replace("June ", "Jun ")
                .Replace("July ", "Jul ");

            return DateTime.TryParseExact(
                normalised,
                new[] { "dd MMM yyyy", "dd MMMM yyyy" },
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out var dt
            ) ? dt : null;
        }
    }
}