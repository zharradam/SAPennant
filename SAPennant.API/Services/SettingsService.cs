using SAPennant.API.Data;
using SAPennant.API.Models;

namespace SAPennant.API.Services;

public class SettingsService
{
    private readonly AppDbContext _db;

    public SettingsService(AppDbContext db) => _db = db;

    public bool GetBool(string key, bool defaultValue = true)
    {
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == key);
        if (setting == null) return defaultValue;
        return bool.TryParse(setting.Value, out var val) ? val : defaultValue;
    }

    public async Task SetBoolAsync(string key, bool value)
    {
        var setting = _db.AppSettings.FirstOrDefault(s => s.Key == key);
        if (setting == null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value.ToString() });
        }
        else
        {
            setting.Value = value.ToString();
        }
        await _db.SaveChangesAsync();
    }
}