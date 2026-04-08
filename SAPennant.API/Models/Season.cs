using System.ComponentModel.DataAnnotations;

namespace SAPennant.API.Models;

public class Season
{
    [Key]
    public int Year { get; set; }
    public int RegularId { get; set; }
    public int? FinalsId { get; set; }
}