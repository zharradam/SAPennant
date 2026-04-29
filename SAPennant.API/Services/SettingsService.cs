using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Services;

public class SettingsService
{
    private readonly IAppSettingRepository _appSettings;

    public SettingsService(IAppSettingRepository appSettings) => _appSettings = appSettings;

    public async Task<bool> GetBoolAsync(string key, bool defaultValue = true)
    {
        var setting = await _appSettings.GetAsync(key);
        if (setting == null) return defaultValue;
        return bool.TryParse(setting.Value, out var val) ? val : defaultValue;
    }

    public async Task SetBoolAsync(string key, bool value)
    {
        await _appSettings.SetAsync(key, value.ToString());
    }

    public async Task<int> GetIntAsync(string key, int defaultValue = 0)
    {
        var setting = await _appSettings.GetAsync(key);
        if (setting == null) return defaultValue;
        return int.TryParse(setting.Value, out var val) ? val : defaultValue;
    }

    public async Task SetIntAsync(string key, int value)
    {
        await _appSettings.SetAsync(key, value.ToString());
    }
}