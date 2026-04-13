using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SAPennant.API.Data;
using SAPennant.API.Repositories.Implementations;
using SAPennant.API.Repositories.Interfaces;

namespace SAPennant.API.Infrastructure;

public static class DatabaseProviderFactory
{
    public static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["DatabaseProvider"];

        var connectionString = provider == "postgres"
            ? configuration.GetConnectionString("NeonConnection")
            : configuration.GetConnectionString("DefaultConnection");

        if (provider == "postgres")
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString,
                    x => x.MigrationsAssembly("SAPennant.API")
                           .MigrationsHistoryTable("__EFMigrationsHistory"))
                       .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning)));
        }
        else
        {
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(connectionString,
                    x => x.MigrationsAssembly("SAPennant.API")
                           .MigrationsHistoryTable("__EFMigrationsHistory", "sqlserver")));
        }

        return services;
    }

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