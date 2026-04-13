using SAPennant.API.Models;

namespace SAPennant.API.Repositories.Interfaces;

public interface IAppSettingRepository
{
    Task<AppSetting?> GetAsync(string key);
    Task SetAsync(string key, string value);
}