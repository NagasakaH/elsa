using Elsa.Features.Services;
using Elsa.ServiceBus.MassTransit.Features;
using Elsa.ServiceBus.MassTransit.Models;
using Elsa.ServiceBus.MassTransit.Services;
using MassTransit;

// ReSharper disable once CheckNamespace
namespace Elsa.Extensions;

/// <summary>
/// Provides extensions to <see cref="IModule"/> that enables and configures MassTransit.
/// </summary>
public static class MassTransitFeatureExtensions
{
    private static readonly object ServiceBusConsumerTypesKey = new();
    private static readonly object MessageTypesKey = new();
    private static readonly object RpcMessageTypesKey = new();

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    public static MassTransitFeature AddConsumer<T>(this MassTransitFeature feature, string? name, bool isTemporary, bool ignoreConsumersDisabled = false) where T : IConsumer =>
        feature.AddConsumer(typeof(T), name, isTemporary, ignoreConsumersDisabled: ignoreConsumersDisabled);

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    /// <typeparam name="T">The consumer type.</typeparam>
    /// <typeparam name="TDefinition">The consumer definition type.</typeparam>
    public static MassTransitFeature AddConsumer<T, TDefinition>(this MassTransitFeature feature, string? name, bool isTemporary, bool ignoreConsumersDisabled = false)
        where T : IConsumer
        where TDefinition : IConsumerDefinition
    {
        return feature.AddConsumer(typeof(T), name, isTemporary, typeof(TDefinition), ignoreConsumersDisabled);
    }

    /// <summary>
    /// Registers the specified type for MassTransit service bus consumer discovery.
    /// </summary>
    public static MassTransitFeature AddConsumer(this MassTransitFeature feature, Type type, string? name, bool isTemporary, Type? consumerDefinitionType = null, bool ignoreConsumersDisabled = false)
    {
        var types = feature.Module.Properties.GetOrAdd(ServiceBusConsumerTypesKey, () => new HashSet<ConsumerTypeDefinition>());
        types.Add(new ConsumerTypeDefinition(type, consumerDefinitionType, name, isTemporary, ignoreConsumersDisabled));
        return feature;
    }

    /// <summary>
    /// Registers a message type which is to be used by the <see cref="MassTransitActivityTypeProvider"/> to dynamically provide activities to send and receive these messages.
    /// </summary>
    public static MassTransitFeature AddMessageType<T>(this MassTransitFeature feature) where T : class => feature.AddMessageType(typeof(T));

    /// <summary>
    /// Registers a message type which is to be used by the <see cref="MassTransitActivityTypeProvider"/> to dynamically provide activities to send and receive these messages.
    /// </summary>
    public static MassTransitFeature AddMessageType(this MassTransitFeature feature, Type type)
    {
        var types = feature.Module.Properties.GetOrAdd(MessageTypesKey, () => new HashSet<Type>());
        types.Add(type);
        return feature;
    }

    /// <summary>
    /// Registers an RPC message type pair (request/response) for request-response operations.
    /// This will create activities for both synchronous and asynchronous RPC calls.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="feature">The MassTransit feature.</param>
    /// <param name="timeout">Optional timeout for the RPC operation. Defaults to 30 seconds.</param>
    /// <returns>The MassTransit feature for chaining.</returns>
    public static MassTransitFeature AddRpcMessageType<TRequest, TResponse>(this MassTransitFeature feature, TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class
    {
        return feature.AddRpcMessageType(typeof(TRequest), typeof(TResponse), timeout, null);
    }

    /// <summary>
    /// Registers an RPC message type pair (request/response) for request-response operations with a specific destination address.
    /// Use this when the RPC consumer is running in a separate process/service.
    /// </summary>
    /// <typeparam name="TRequest">The request message type.</typeparam>
    /// <typeparam name="TResponse">The response message type.</typeparam>
    /// <param name="feature">The MassTransit feature.</param>
    /// <param name="destinationAddress">The queue address where the consumer is listening (e.g., "queue:calculate-consumer").</param>
    /// <param name="timeout">Optional timeout for the RPC operation. Defaults to 30 seconds.</param>
    /// <returns>The MassTransit feature for chaining.</returns>
    public static MassTransitFeature AddRpcMessageType<TRequest, TResponse>(this MassTransitFeature feature, string destinationAddress, TimeSpan? timeout = null)
        where TRequest : class
        where TResponse : class
    {
        return feature.AddRpcMessageType(typeof(TRequest), typeof(TResponse), timeout, new Uri(destinationAddress));
    }

    /// <summary>
    /// Registers an RPC message type pair (request/response) for request-response operations.
    /// This will create activities for both synchronous and asynchronous RPC calls.
    /// </summary>
    /// <param name="feature">The MassTransit feature.</param>
    /// <param name="requestType">The request message type.</param>
    /// <param name="responseType">The response message type.</param>
    /// <param name="timeout">Optional timeout for the RPC operation. Defaults to 30 seconds.</param>
    /// <param name="destinationAddress">Optional destination queue address for the RPC request.</param>
    /// <returns>The MassTransit feature for chaining.</returns>
    public static MassTransitFeature AddRpcMessageType(this MassTransitFeature feature, Type requestType, Type responseType, TimeSpan? timeout = null, Uri? destinationAddress = null)
    {
        var types = feature.Module.Properties.GetOrAdd(RpcMessageTypesKey, () => new HashSet<RpcMessageTypeDefinition>());
        types.Add(new RpcMessageTypeDefinition(requestType, responseType, timeout, destinationAddress));
        return feature;
    }

    /// <summary>
    /// Returns all collected consumer types.
    /// </summary>
    public static IEnumerable<ConsumerTypeDefinition> GetConsumers(this MassTransitFeature feature)
    {
        var definitions = feature.Module.Properties.GetOrAdd(ServiceBusConsumerTypesKey, () => new HashSet<ConsumerTypeDefinition>());
        var disableConsumers = feature.DisableConsumers;
        return !disableConsumers ? definitions : definitions.Where(x => x.IgnoreConsumersDisabled).ToList();
    }

    /// <summary>
    /// Returns all collected message types.
    /// </summary>
    internal static IEnumerable<Type> GetMessages(this MassTransitFeature feature) => feature.Module.Properties.GetOrAdd(MessageTypesKey, () => new HashSet<Type>());

    /// <summary>
    /// Returns all collected RPC message type pairs.
    /// </summary>
    internal static IEnumerable<RpcMessageTypeDefinition> GetRpcMessages(this MassTransitFeature feature) => feature.Module.Properties.GetOrAdd(RpcMessageTypesKey, () => new HashSet<RpcMessageTypeDefinition>());
}