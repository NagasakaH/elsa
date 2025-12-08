using Elsa.ServiceBus.MassTransit.Builders;
using Elsa.ServiceBus.MassTransit.Models;

namespace Elsa.ServiceBus.MassTransit.Options;

/// <summary>
/// Provides settings to the RabbitMQ broker for MassTransit.
/// </summary>
public class MassTransitActivityOptions
{
    /// <summary>
    /// A set of message types that can be sent and received in the form of workflow activities.
    /// </summary>
    public ISet<Type> MessageTypes { get; set; } = new HashSet<Type>();

    /// <summary>
    /// A set of RPC message type pairs (request/response) that can be used for request-response operations.
    /// </summary>
    public ISet<RpcMessageTypeDefinition> RpcMessageTypes { get; set; } = new HashSet<RpcMessageTypeDefinition>();

    /// <summary>
    /// A list of custom RPC activity definitions created using the fluent builder.
    /// </summary>
    public IList<RpcActivityDefinition> CustomRpcActivityDefinitions { get; set; } = new List<RpcActivityDefinition>();
}