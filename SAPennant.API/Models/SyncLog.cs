namespace SAPennant.API.Models;

public class SyncLog
{
    public int Id { get; set; }
    public DateTime SyncedAt { get; set; }
    public string Type { get; set; } = string.Empty;
}