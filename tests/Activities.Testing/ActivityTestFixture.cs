using Elsa.Extensions;
using Elsa.Features.Services;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace NagasakaEventSystem.Activities.Testing;

/// <summary>
/// Minimal, repo-local test fixture inspired by Elsa.Testing.Shared.WorkflowTestFixture.
/// Keeps dependencies small and avoids relying on submodule test packages.
/// </summary>
public sealed class ActivityTestFixture
{
    private readonly ITestOutputHelper? _output;
    private readonly ServiceCollection _services = new();
    private Action<IModule> _configureElsa;
    private IServiceProvider? _provider;

    public ActivityTestFixture(ITestOutputHelper? output = null)
    {
        _output = output;

        _services
            .AddSingleton<IHostEnvironment>(new TestHostEnvironment())
            .AddLogging(b =>
            {
                b.SetMinimumLevel(LogLevel.Debug);
                if (_output != null)
                    b.AddProvider(new XunitLoggerProvider(_output));
            });

        _configureElsa += elsa => elsa
            .AddActivitiesFrom<WriteLine>()
            .UseScheduling()
            ;
    }

    public IServiceProvider Services => _provider ?? throw new InvalidOperationException("BuildAsync must be called first");

    public ActivityTestFixture ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    public ActivityTestFixture ConfigureElsa(Action<IModule> configure)
    {
        _configureElsa += configure;
        return this;
    }

    public ActivityTestFixture AddActivitiesFrom<T>()
    {
        _configureElsa += elsa => elsa.AddActivitiesFrom<T>();
        return this;
    }

    public async Task<ActivityTestFixture> BuildAsync()
    {
        _services.AddElsa(_configureElsa);
        _provider = _services.BuildServiceProvider();
        await _provider.PopulateRegistriesAsync();
        return this;
    }

    public async Task<RunWorkflowResult> RunActivityAsync(IActivity activity, CancellationToken cancellationToken = default)
    {
        if (_provider == null)
            await BuildAsync();

        var runner = Services.GetRequiredService<IWorkflowRunner>();
        return await runner.RunAsync(activity, cancellationToken: cancellationToken);
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "Activities.Testing";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = new Microsoft.Extensions.FileProviders.NullFileProvider();
    }
}
