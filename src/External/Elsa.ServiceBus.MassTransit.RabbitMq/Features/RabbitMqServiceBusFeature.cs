using Elsa.Common;
using Elsa.Extensions;
using Elsa.Features.Abstractions;
using Elsa.Features.Attributes;
using Elsa.Features.Services;
using Elsa.Hosting.Management.Contracts;
using Elsa.Hosting.Management.Features;
using Elsa.ServiceBus.MassTransit.Extensions;
using Elsa.ServiceBus.MassTransit.Features;
using Elsa.ServiceBus.MassTransit.Options;
using Elsa.ServiceBus.MassTransit.RabbitMq.Options;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Elsa.ServiceBus.MassTransit.RabbitMq.Features;

/// <summary>
/// Configures MassTransit to use the RabbitMQ transport.
/// </summary>
[DependsOn(typeof(MassTransitFeature))]
[DependsOn(typeof(ClusteringFeature))]
public class RabbitMqServiceBusFeature : FeatureBase
{
    /// <inheritdoc />
    public RabbitMqServiceBusFeature(IModule module) : base(module)
    {
    }

    /// <summary>
    /// A RabbitMQ connection string.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Connection options for RabbitMQ including virtual host support.
    /// When set, this takes precedence over <see cref="ConnectionString"/>.
    /// </summary>
    public RabbitMqConnectionOptions? ConnectionOptions { get; set; }

    /// <summary>
    /// Configures the RabbitMQ transport options.
    /// </summary>
    public Action<RabbitMqTransportOptions>? TransportOptions { get; set; }

    /// <summary>
    /// Configures the RabbitMQ bus.
    /// </summary>
    [Obsolete("Use ConfigureTransportBus instead which provides a reference to IBusRegistrationContext.")]
    public Action<IRabbitMqBusFactoryConfigurator>? ConfigureServiceBus { get; set; }

    /// <summary>
    /// Configures the RabbitMQ bus within MassTransit for additional transport level components or features.
    /// This action provides access to the <see cref="IBusRegistrationContext"/> and <see cref="IRabbitMqBusFactoryConfigurator"/>.
    /// </summary>
    /// <remarks>
    /// Use this action to configure advanced settings and features for the RabbitMQ bus, such as middleware 
    /// or additional endpoints. This action will run in addition to the Elsa required configuration.
    /// </remarks>
    public Action<IBusRegistrationContext, IRabbitMqBusFactoryConfigurator>? ConfigureTransportBus { get; set; }

    /// <inheritdoc />
    public override void Configure()
    {
        Module.Configure<MassTransitFeature>(massTransitFeature =>
        {
            massTransitFeature.BusConfigurator = configure =>
            {
                var temporaryConsumers = massTransitFeature.GetConsumers()
                    .Where(c => c.IsTemporary)
                    .ToList();

                // Consumers need to be added before the UsingRabbitMq statement to prevent exceptions.
                foreach (var consumer in temporaryConsumers)
                    configure.AddConsumer(consumer.ConsumerType).ExcludeFromConfigureEndpoints();

                configure.UsingRabbitMq((context, configurator) =>
                {
                    var options = context.GetRequiredService<IOptions<MassTransitOptions>>().Value;
                    var instanceNameProvider = context.GetRequiredService<IApplicationInstanceNameProvider>();

                    // Configure RabbitMQ host - ConnectionOptions takes precedence over ConnectionString
                    if (ConnectionOptions != null)
                    {
                        configurator.Host(ConnectionOptions.Host, ConnectionOptions.Port, ConnectionOptions.VirtualHost, h =>
                        {
                            h.Username(ConnectionOptions.Username);
                            h.Password(ConnectionOptions.Password);
                            if (ConnectionOptions.UseSsl)
                                h.UseSsl(s => { });
                        });
                    }
                    else if (!string.IsNullOrEmpty(ConnectionString))
                    {
                        configurator.Host(ConnectionString);
                    }

                    if (options.PrefetchCount is not null)
                        configurator.PrefetchCount = options.PrefetchCount.Value;
                    configurator.ConcurrentMessageLimit = options.ConcurrentMessageLimit;

#pragma warning disable CS0618 // Type or member is obsolete
                    ConfigureServiceBus?.Invoke(configurator);
#pragma warning restore CS0618 // Type or member is obsolete
                    ConfigureTransportBus?.Invoke(context, configurator);

                    foreach (var consumer in temporaryConsumers)
                    {
                        configurator.ReceiveEndpoint($"{instanceNameProvider.GetName()}-{consumer.Name}",
                            endpointConfigurator =>
                            {
                                endpointConfigurator.QueueExpiration = options.TemporaryQueueTtl ?? TimeSpan.FromHours(1);
                                endpointConfigurator.ConcurrentMessageLimit = options.ConcurrentMessageLimit;
                                endpointConfigurator.Durable = false;
                                endpointConfigurator.AutoDelete = true;
                                endpointConfigurator.ConfigureConsumer(context, consumer.ConsumerType);
                            });
                    }

                    if (!massTransitFeature.DisableConsumers)
                    {
                        if (Module.HasFeature<MassTransitWorkflowDispatcherFeature>())
                            configurator.SetupWorkflowDispatcherEndpoints(context);
                    }

                    configurator.ConfigureEndpoints(context, new KebabCaseEndpointNameFormatter("Elsa", false));
                    
                    configurator.ConfigureJsonSerializerOptions(serializerOptions =>
                    {
                        var serializer = context.GetRequiredService<IJsonSerializer>();
                        serializer.ApplyOptions(serializerOptions);
                        return serializerOptions;
                    });
                    
                    configurator.ConfigureTenantMiddleware(context);
                });
            };
        });
    }

    /// <inheritdoc />
    public override void Apply()
    {
        if (TransportOptions != null) Services.Configure(TransportOptions);
    }
}