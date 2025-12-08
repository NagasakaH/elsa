using Elsa.Features.Services;
using Elsa.ServiceBus.MassTransit.Features;
using Elsa.ServiceBus.MassTransit.RabbitMq.Features;
using Elsa.ServiceBus.MassTransit.RabbitMq.Options;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit and the RabbitMQ transport.
/// </summary>
public static class ModuleExtensions
{
    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, string connectionString)
    {
        feature.Module.Configure((Action<RabbitMqServiceBusFeature>)Configure);
        return feature;

        void Configure(RabbitMqServiceBusFeature bus)
        {
            bus.ConnectionString = connectionString;
        }
    }

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit using connection options with virtual host support.
    /// </summary>
    /// <param name="feature">The MassTransit feature.</param>
    /// <param name="configureOptions">Action to configure the RabbitMQ connection options.</param>
    /// <returns>The MassTransit feature for chaining.</returns>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Action<RabbitMqConnectionOptions> configureOptions)
    {
        var options = new RabbitMqConnectionOptions();
        configureOptions(options);
        
        feature.Module.Configure<RabbitMqServiceBusFeature>(rabbitMqFeature =>
        {
            rabbitMqFeature.ConnectionOptions = options;
        });
        return feature;
    }

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit using connection options with virtual host support.
    /// </summary>
    /// <param name="feature">The MassTransit feature.</param>
    /// <param name="connectionOptions">The RabbitMQ connection options.</param>
    /// <returns>The MassTransit feature for chaining.</returns>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, RabbitMqConnectionOptions connectionOptions)
    {
        feature.Module.Configure<RabbitMqServiceBusFeature>(rabbitMqFeature =>
        {
            rabbitMqFeature.ConnectionOptions = connectionOptions;
        });
        return feature;
    }

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit using connection options with additional configuration.
    /// </summary>
    /// <param name="feature">The MassTransit feature.</param>
    /// <param name="configureOptions">Action to configure the RabbitMQ connection options.</param>
    /// <param name="configure">Additional configuration for the RabbitMQ feature.</param>
    /// <returns>The MassTransit feature for chaining.</returns>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Action<RabbitMqConnectionOptions> configureOptions, Action<RabbitMqServiceBusFeature> configure)
    {
        var options = new RabbitMqConnectionOptions();
        configureOptions(options);
        
        feature.Module.Configure<RabbitMqServiceBusFeature>(rabbitMqFeature =>
        {
            rabbitMqFeature.ConnectionOptions = options;
            configure(rabbitMqFeature);
        });
        return feature;
    }

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, Action<RabbitMqServiceBusFeature> configure)
    {
        feature.Module.Configure(configure);
        return feature;
    }

    /// <summary>
    /// Enable and configure the RabbitMQ transport for MassTransit.
    /// </summary>
    public static MassTransitFeature UseRabbitMq(this MassTransitFeature feature, string connectionString, Action<RabbitMqServiceBusFeature> configure)
    {
        feature.Module.Configure<RabbitMqServiceBusFeature>(rabbitMqFeature =>
        {
            rabbitMqFeature.ConnectionString = connectionString;
            configure(rabbitMqFeature);
        });
        return feature;
    }
}