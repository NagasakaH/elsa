using ElsaServer.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ElsaServer.Hosted;

public class StartupInitializationHostedService : IHostedService
{
    private readonly ActivityAssemblyLoader _activityLoader;
    private readonly WorkflowCatalogLoader _catalogLoader;
    private readonly ILogger<StartupInitializationHostedService> _logger;

    public StartupInitializationHostedService(
        ActivityAssemblyLoader activityLoader,
        WorkflowCatalogLoader catalogLoader,
        ILogger<StartupInitializationHostedService> logger)
    {
        _activityLoader = activityLoader;
        _catalogLoader = catalogLoader;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Loading activities and workflows at startup");
        await _activityLoader.LoadAsync(cancellationToken);
        await _catalogLoader.LoadAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
