namespace Elsa.ServiceBus.MassTransit.Models;

/// <summary>
/// Defines a request-response message type pair for RPC operations.
/// </summary>
/// <param name="RequestType">The request message type.</param>
/// <param name="ResponseType">The response message type.</param>
/// <param name="Timeout">The timeout for the RPC operation. Defaults to 30 seconds if not specified.</param>
/// <param name="DestinationAddress">The destination queue address for the RPC request. Required when the consumer is in a separate process.</param>
public record RpcMessageTypeDefinition(Type RequestType, Type ResponseType, TimeSpan? Timeout = null, Uri? DestinationAddress = null);
