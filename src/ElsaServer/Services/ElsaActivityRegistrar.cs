using System.Reflection;
using Elsa.Workflows;
using Microsoft.Extensions.Logging;
using NagasakaEventSystem.Activities.Loader;

namespace ElsaServer.Services;

public sealed class ElsaActivityRegistrar : IActivityRegistrar
{
    private readonly IActivityRegistry _activityRegistry;
    private readonly ILogger<ElsaActivityRegistrar> _logger;

    public ElsaActivityRegistrar(IActivityRegistry activityRegistry, ILogger<ElsaActivityRegistrar> logger)
    {
        _activityRegistry = activityRegistry;
        _logger = logger;
    }

    public async Task RegisterExportedActivitiesAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var activityTypes = assembly
            .GetExportedTypes()
            .Where(x => typeof(IActivity).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, IsGenericType: false })
            .ToList();

        if (activityTypes.Count == 0)
        {
            _logger.LogDebug("No exported IActivity types found in {Assembly}", assembly.FullName);
            return;
        }

        await _activityRegistry.RegisterAsync(activityTypes, cancellationToken);
        _logger.LogInformation("Registered {Count} activities from {Assembly}", activityTypes.Count, assembly.FullName);
    }
}
