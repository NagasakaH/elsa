using System.Reflection;
using Elsa.Workflows;
using ElsaServer.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElsaServer.Services;

public class ActivityAssemblyLoader
{
    private readonly ActivityAssemblyOptions _options;
    private readonly IActivityRegistry _activityRegistry;
    private readonly ILogger<ActivityAssemblyLoader> _logger;

    public ActivityAssemblyLoader(
        IOptions<ActivityAssemblyOptions> options,
        IActivityRegistry activityRegistry,
        ILogger<ActivityAssemblyLoader> logger)
    {
        _options = options.Value;
        _activityRegistry = activityRegistry;
        _logger = logger;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var basePath = Path.GetFullPath(_options.Directory);
        if (!Directory.Exists(basePath))
        {
            if (_options.Strict)
                throw new DirectoryNotFoundException($"Activities directory not found: {basePath}");

            _logger.LogInformation("Activities directory not found: {BasePath}", basePath);
            return;
        }

        var assemblies = Directory.EnumerateFiles(basePath, _options.SearchPattern, SearchOption.TopDirectoryOnly);
        foreach (var assemblyPath in assemblies)
        {
            try
            {
                var assembly = Assembly.LoadFrom(assemblyPath);
                var activityTypes = assembly
                    .GetExportedTypes()
                    .Where(x => typeof(IActivity).IsAssignableFrom(x) && x is { IsAbstract: false, IsInterface: false, IsGenericType: false })
                    .ToList();

                if (activityTypes.Count == 0)
                {
                    _logger.LogInformation("No activities found in {AssemblyPath}", assemblyPath);
                    continue;
                }

                await _activityRegistry.RegisterAsync(activityTypes, cancellationToken);
                _logger.LogInformation("Registered {Count} activities from {AssemblyPath}", activityTypes.Count, assemblyPath);
            }
            catch (Exception ex)
            {
                var message = $"Failed to load activities from {assemblyPath}: {ex.Message}";
                if (_options.Strict)
                    throw new InvalidOperationException(message, ex);

                _logger.LogWarning(ex, message);
            }
        }
    }
}
