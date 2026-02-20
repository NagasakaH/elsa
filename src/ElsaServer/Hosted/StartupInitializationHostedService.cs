using ElsaServer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NagasakaEventSystem.Activities.Loader;
using NagasakaEventSystem.WorkflowCatalog;

namespace ElsaServer.Hosted;

public class StartupInitializationHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StartupInitializationHostedService> _logger;

    public StartupInitializationHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<StartupInitializationHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading activities and workflows at startup");

        using var scope = _scopeFactory.CreateScope();
        // Prefer the new loader (supports module hook + better dependency resolution),
        // but keep the existing loader for backward compatibility.
        var directoryLoader = scope.ServiceProvider.GetService<DirectoryActivityAssemblyLoader>();
        var activityLoader = scope.ServiceProvider.GetRequiredService<ActivityAssemblyLoader>();
        var catalogLoader = scope.ServiceProvider.GetRequiredService<WorkflowCatalogLoader>();

        if (directoryLoader != null)
            await directoryLoader.LoadAndRegisterAsync(cancellationToken);
        else
            await activityLoader.LoadAsync(cancellationToken);
        await catalogLoader.LoadAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
