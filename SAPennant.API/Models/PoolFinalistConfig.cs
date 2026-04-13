using System.ComponentModel.DataAnnotations;

namespace SAPennant.API.Models;

public class PoolFinalistConfig
{
    [Key]
    public string Pool { get; set; } = string.Empty;
    public int FinalistCount { get; set; }
}