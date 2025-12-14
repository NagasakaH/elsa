using System.Reflection;
using Elsa.Workflows;
using ElsaServer.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ElsaServer.Services;

public class ActivityAssemblyLoader
{
    private readonly ActivityAssemblyOptions _options;
    private readonly IActivityRegistry _activityRegistry;
    private readonly ILogger<ActivityAssemblyLoader> _logger;
    private readonly IHostEnvironment _environment;

    public ActivityAssemblyLoader(
        IOptions<ActivityAssemblyOptions> options,
        IActivityRegistry activityRegistry,
        ILogger<ActivityAssemblyLoader> logger,
        IHostEnvironment environment)
    {
        _options = options.Value;
        _activityRegistry = activityRegistry;
        _logger = logger;
        _environment = environment;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var basePath = ResolvePath(_options.Directory);
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

    private string ResolvePath(string directory)
    {
        if (Path.IsPathRooted(directory))
            return directory;

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, directory)),
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", directory))
        };

        var existing = candidates.FirstOrDefault(Directory.Exists);
        return existing ?? candidates.Last();
    }
}
