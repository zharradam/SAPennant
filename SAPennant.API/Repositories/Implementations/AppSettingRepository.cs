using Microsoft.EntityFrameworkCore;
using SAPennant.API.Data;
using SAPennant.API.Models;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Repositories.Implementations;

public class AppSettingRepository : IAppSettingRepository
{
    private readonly AppDbContext _context;

    public AppSettingRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<AppSetting?> GetAsync(string key)
    {
        return await _context.AppSettings.FindAsync(key);
    }

    public async Task SetAsync(string key, string value)
    {
        var setting = await _context.AppSettings.FindAsync(key);
        if (setting == null)
        {
            _context.AppSettings.Add(new AppSetting { Key = key, Value = value });
        }
        else
        {
            setting.Value = value;
        }
        await _context.SaveChangesAsync();
    }
}