using Microsoft.Extensions.DependencyInjection;
using SAPennant.API.Services;

namespace SAPennant.API.Tests;

public class GolfboxHttpClientTests
{
    [Fact]
    public void ConfigureHttpClientSetsBrowserHeaders()
    {
        var client = new HttpClient();

        GolfboxSyncService.ConfigureHttpClient(client);

        Assert.Contains("Mozilla/5.0", client.DefaultRequestHeaders.UserAgent.ToString());
        Assert.Equal("https://golf.com.au/", client.DefaultRequestHeaders.Referrer?.ToString());
    }

    /// Regression guard: registering the service with AddScoped as well as
    /// AddHttpClient means the later registration wins and the typed client's
    /// configuration is silently discarded. This asserts the headers survive
    /// the registration the app actually uses.
    [Fact]
    public void TypedClientRegistrationKeepsTheConfiguredHeaders()
    {
        var services = new ServiceCollection();
        services.AddHttpClient<GolfboxSyncService>(GolfboxSyncService.ConfigureHttpClient);

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IHttpClientFactory>();
        var client = factory.CreateClient(nameof(GolfboxSyncService));

        Assert.Contains("Mozilla/5.0", client.DefaultRequestHeaders.UserAgent.ToString());
        Assert.Equal("https://golf.com.au/", client.DefaultRequestHeaders.Referrer?.ToString());
    }
}
