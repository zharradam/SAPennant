using System.ComponentModel.DataAnnotations;

namespace SAPennant.API.Models;

public class HonourRoll
{
    public int Id { get; set; }
    public int Year { get; set; }
    public string Competition { get; set; } = string.Empty;
    public string Pool { get; set; } = string.Empty;
    public string? Winner { get; set; }
}