using System.Reflection;
using System.Runtime.Loader;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NagasakaEventSystem.Activities.Contracts;

namespace NagasakaEventSystem.Activities.Loader;

public sealed class DirectoryActivityAssemblyLoader
{
    private readonly ActivityLoaderOptions _options;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<DirectoryActivityAssemblyLoader> _logger;
    private readonly IServiceProvider _services;
    private readonly IActivityRegistrar _registrar;

    public DirectoryActivityAssemblyLoader(
        IOptions<ActivityLoaderOptions> options,
        IHostEnvironment environment,
        ILogger<DirectoryActivityAssemblyLoader> logger,
        IServiceProvider services,
        IActivityRegistrar registrar)
    {
        _options = options.Value;
        _environment = environment;
        _logger = logger;
        _services = services;
        _registrar = registrar;
    }

    public async Task LoadAndRegisterAsync(CancellationToken cancellationToken)
    {
        var basePath = ResolvePath(_options.Directory);
        if (!Directory.Exists(basePath))
        {
            if (_options.Strict)
                throw new DirectoryNotFoundException($"Activities directory not found: {basePath}");

            _logger.LogInformation("Activities directory not found: {BasePath}", basePath);
            return;
        }

        var searchOption = _options.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var dllPaths = Directory.EnumerateFiles(basePath, _options.SearchPattern, searchOption).ToList();

        if (dllPaths.Count == 0)
        {
            _logger.LogInformation("No activity assemblies found in {BasePath}", basePath);
            return;
        }

        foreach (var dllPath in dllPaths)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var assembly = LoadPluginAssembly(dllPath);

                // 1) Module hook (optional)
                await TryInvokeModulesAsync(assembly, cancellationToken);

                // 2) Fallback: exported IActivity scan (registrar decides)
                await _registrar.RegisterExportedActivitiesAsync(assembly, cancellationToken);
            }
            catch (Exception ex)
            {
                var message = $"Failed to load/register activities from {dllPath}: {ex.Message}";
                if (_options.Strict)
                    throw new InvalidOperationException(message, ex);

                _logger.LogWarning(ex, message);
            }
        }
    }

    private Assembly LoadPluginAssembly(string assemblyPath)
    {
        // Resolve dependencies from the plugin directory.
        var loadContext = new AssemblyLoadContext($"Activities:{Path.GetFileNameWithoutExtension(assemblyPath)}", isCollectible: false);
        loadContext.Resolving += (_, name) =>
        {
            var candidate = Path.Combine(Path.GetDirectoryName(assemblyPath)!, $"{name.Name}.dll");
            return File.Exists(candidate) ? loadContext.LoadFromAssemblyPath(candidate) : null;
        };

        return loadContext.LoadFromAssemblyPath(Path.GetFullPath(assemblyPath));
    }

    private async Task TryInvokeModulesAsync(Assembly assembly, CancellationToken cancellationToken)
    {
        var moduleTypes = assembly
            .GetExportedTypes()
            .Where(t => typeof(IActivityModule).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false })
            .ToList();

        if (moduleTypes.Count == 0)
            return;

        foreach (var moduleType in moduleTypes)
        {
            if (Activator.CreateInstance(moduleType) is not IActivityModule module)
                continue;

            await module.RegisterAsync(new ActivityModuleContext(_services), cancellationToken);
        }
    }

    private string ResolvePath(string directory)
    {
        if (Path.IsPathRooted(directory))
            return EnsureWithinContentRoot(directory);

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, directory)),
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", directory))
        };

        var existing = candidates.FirstOrDefault(Directory.Exists);
        return EnsureWithinContentRoot(existing ?? candidates.Last());
    }

    private string EnsureWithinContentRoot(string resolvedPath)
    {
        var root = Path.GetFullPath(_environment.ContentRootPath);
        var parentRoot = Path.GetFullPath(Path.Combine(root, "..", ".."));
        var fullPath = Path.GetFullPath(resolvedPath);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(parentRoot, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal detected: {resolvedPath} is outside allowed directories.");
        return fullPath;
    }
}
