using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace NagasakaEventSystem.Activities.Loader.Hosted;

public sealed class ActivityLoaderHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ActivityLoaderHostedService> _logger;

    public ActivityLoaderHostedService(IServiceScopeFactory scopeFactory, ILogger<ActivityLoaderHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var loader = scope.ServiceProvider.GetRequiredService<DirectoryActivityAssemblyLoader>();
            await loader.LoadAndRegisterAsync(stoppingToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ActivityLoaderHostedService failed");
            throw;
        }
    }
}
