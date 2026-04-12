using System.ComponentModel.DataAnnotations;

public class RoundStatus
{
    public int Id { get; set; }
    public int Year { get; set; }

    [MaxLength(100)]
    public string Pool { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Round { get; set; } = string.Empty;

    public bool IsSettled { get; set; }
    public DateTime? LastChecked { get; set; }
    public DateTime? SettledAt { get; set; }
}