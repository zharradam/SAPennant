using SAPennant.API.Repositories.Implementations;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Infrastructure;

public static class DatabaseProviderFactory
{
    public static IServiceCollection AddRepositories(this IServiceCollection services)
    {
        services.AddScoped<IPennantMatchRepository, PennantMatchRepository>();
        services.AddScoped<ISeasonRepository, SeasonRepository>();
        services.AddScoped<ISyncLogRepository, SyncLogRepository>();
        services.AddScoped<IRoundStatusRepository, RoundStatusRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IHonourRollRepository, HonourRollRepository>();

        return services;
    }
}