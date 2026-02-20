using Elsa.Extensions;
using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NagasakaEventSystem.Activities.Loader;
using Xunit;

namespace ElsaServer.UnitTests.Activities;

public class DirectoryActivityAssemblyLoaderTests
{
    [Fact]
    public async Task Loads_Dlls_From_Directory_And_Registers_Activities()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddElsa();

        // Hook registrar to Elsa's activity registry.
        var captured = new List<Type>();
        services.AddScoped<IActivityRegistrar>(_ => new CapturingRegistrar(captured));

        // Fake environment.
        services.AddSingleton<IHostEnvironment>(new TestHostEnvironment
        {
            ContentRootPath = Directory.GetCurrentDirectory()
        });

        var outputDir = AppContext.BaseDirectory;
        var activitiesDir = Path.Combine(outputDir, "Activities");
        Directory.CreateDirectory(activitiesDir);

        // Copy template DLL next to the test output so the loader can find it.
        var templateDll = FindFileUpwards(
            startDirectory: AppContext.BaseDirectory,
            relativePath: Path.Combine("src", "Activities.Templates", "CustomActivityTemplate", "bin", "Debug", "net8.0", "CustomActivityTemplate.dll"));
        Assert.True(File.Exists(templateDll), $"Template DLL not found: {templateDll}");
        File.Copy(templateDll, Path.Combine(activitiesDir, Path.GetFileName(templateDll)), overwrite: true);

        services.Configure<ActivityLoaderOptions>(o =>
        {
            o.Directory = activitiesDir;
            o.SearchPattern = "*.dll";
            o.Strict = false;
            o.Recursive = false;
        });

        services.AddScoped<DirectoryActivityAssemblyLoader>();

        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        var loader = scope.ServiceProvider.GetRequiredService<DirectoryActivityAssemblyLoader>();

        await loader.LoadAndRegisterAsync(CancellationToken.None);

        Assert.Contains(captured, t => t.FullName == "NagasakaEventSystem.Activities.Templates.CustomActivityTemplate.SampleCustomActivity");
    }

    private sealed class CapturingRegistrar : IActivityRegistrar
    {
        private readonly IList<Type> _captured;

        public CapturingRegistrar(IList<Type> captured) => _captured = captured;

        public Task RegisterExportedActivitiesAsync(System.Reflection.Assembly assembly, CancellationToken cancellationToken)
        {
            var types = assembly
                .GetExportedTypes()
                .Where(t => typeof(IActivity).IsAssignableFrom(t) && t is { IsAbstract: false, IsInterface: false, IsGenericType: false })
                .ToList();

            foreach (var type in types)
                _captured.Add(type);

            return Task.CompletedTask;
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private static string FindFileUpwards(string startDirectory, string relativePath)
    {
        var current = new DirectoryInfo(startDirectory);
        while (current != null)
        {
            var candidate = Path.Combine(current.FullName, relativePath);
            if (File.Exists(candidate))
                return candidate;
            current = current.Parent;
        }

        // return last candidate for assertion message.
        return Path.Combine(new DirectoryInfo(startDirectory).Root.FullName, relativePath);
    }
}
