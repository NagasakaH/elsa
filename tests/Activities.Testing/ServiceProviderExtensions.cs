using Elsa.Workflows;
using Elsa.Workflows.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace NagasakaEventSystem.Activities.Testing;

public static class ServiceProviderExtensions
{
    public static Task PopulateRegistriesAsync(this IServiceProvider services)
    {
        var populator = services.GetRequiredService<IRegistriesPopulator>();
        return populator.PopulateAsync();
    }
}
