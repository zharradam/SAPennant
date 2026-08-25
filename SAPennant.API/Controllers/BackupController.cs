using System.IO.Compression;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Domain;

namespace SAPennant.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ILogger<BackupController> _logger;

    public BackupController(AppDbContext db, ILogger<BackupController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// Streams a zip containing every mapped table as CSV plus a manifest.
    /// Works against whichever provider is active (SQL Server locally,
    /// Postgres/Neon in production).
    [HttpGet]
    public async Task<IActionResult> Download()
    {
        var provider = _db.Database.ProviderName ?? "unknown";
        var isNpgsql = provider.Contains("Npgsql", StringComparison.OrdinalIgnoreCase);

        var tables = _db.Model.GetEntityTypes()
            .Select(e => e.GetTableName())
            .Where(t => !string.IsNullOrEmpty(t))
            .Select(t => t!)
            .Distinct()
            .OrderBy(t => t)
            .ToList();

        var ms = new MemoryStream();
        await _db.Database.OpenConnectionAsync();
        try
        {
            var conn = _db.Database.GetDbConnection();
            using var zip = new ZipArchive(ms, ZipArchiveMode.Create, leaveOpen: true);

            var manifest = new StringBuilder()
                .AppendLine("SA Pennant database backup")
                .AppendLine($"Taken (UTC): {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}")
                .AppendLine($"Provider: {provider}")
                .AppendLine();

            foreach (var table in tables)
            {
                // Table names come from the EF model, not user input; quote per provider.
                var quoted = isNpgsql ? $"\"{table}\"" : $"[{table}]";

                await using var cmd = conn.CreateCommand();
                cmd.CommandText = $"SELECT * FROM {quoted}";
                await using var reader = await cmd.ExecuteReaderAsync();

                var entry = zip.CreateEntry($"{table}.csv", CompressionLevel.Optimal);
                long rows = 0;
                await using (var writer = new StreamWriter(entry.Open(), Encoding.UTF8))
                {
                    var header = string.Join(",", Enumerable.Range(0, reader.FieldCount)
                        .Select(i => CsvExport.Escape(reader.GetName(i))));
                    await writer.WriteLineAsync(header);

                    while (await reader.ReadAsync())
                    {
                        var fields = new string[reader.FieldCount];
                        for (var i = 0; i < reader.FieldCount; i++)
                            fields[i] = CsvExport.Escape(CsvExport.FormatValue(reader.GetValue(i)));
                        await writer.WriteLineAsync(string.Join(",", fields));
                        rows++;
                    }
                }

                manifest.AppendLine($"{table}: {rows} rows");
            }

            var manifestEntry = zip.CreateEntry("manifest.txt");
            await using var mw = new StreamWriter(manifestEntry.Open(), Encoding.UTF8);
            await mw.WriteAsync(manifest.ToString());
        }
        finally
        {
            await _db.Database.CloseConnectionAsync();
        }

        ms.Position = 0;
        _logger.LogInformation("Database backup downloaded ({Tables} tables, {Bytes} bytes zipped)",
            tables.Count, ms.Length);

        var fileName = $"sapennant-backup-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
        return File(ms, "application/zip", fileName);
    }
}
