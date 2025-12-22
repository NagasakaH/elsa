using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NagasakaEventSystem.Activities.Loader.Hosted;

namespace NagasakaEventSystem.Activities.Loader;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDirectoryActivityLoader(this IServiceCollection services, Action<ActivityLoaderOptions>? configure = null)
    {
        if (configure != null)
            services.Configure(configure);

        services.TryAddScoped<DirectoryActivityAssemblyLoader>();
        services.AddHostedService<ActivityLoaderHostedService>();

        return services;
    }
}
