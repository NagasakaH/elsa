using Elsa.Workflows;
using Microsoft.Extensions.DependencyInjection;
using NagasakaEventSystem.Activities.Contracts;

[assembly: ActivityModuleMetadata("CustomActivityTemplate", "1.0.0", Description = "Sample custom activity module")]

namespace NagasakaEventSystem.Activities.Templates.CustomActivityTemplate;

public sealed class Module : IActivityModule
{
    public async ValueTask RegisterAsync(ActivityModuleContext context, CancellationToken cancellationToken)
    {
        var registry = context.Services.GetRequiredService<IActivityRegistry>();
        await registry.RegisterAsync(new[] { typeof(SampleCustomActivity) }, cancellationToken);
    }
}
