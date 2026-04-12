using System.ComponentModel.DataAnnotations;

namespace SAPennant.API.Models;

public class HonourRollNarrative
{
    [Key]
    public string Competition { get; set; } = string.Empty;
    public string Narrative { get; set; } = string.Empty;
}