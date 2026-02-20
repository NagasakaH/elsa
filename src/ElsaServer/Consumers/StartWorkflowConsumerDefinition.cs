using System;
using ElsaServer.Options;
using MassTransit;
using Microsoft.Extensions.Options;

namespace ElsaServer.Consumers;

public class StartWorkflowConsumerDefinition : ConsumerDefinition<StartWorkflowConsumer>
{
    private readonly WorkflowBusOptions _options;

    public StartWorkflowConsumerDefinition(IOptions<WorkflowBusOptions> options)
    {
        _options = options.Value;
        EndpointName = _options.StartQueueName;
        ConcurrentMessageLimit = _options.ConcurrentMessageLimit;
    }

    [Obsolete("Base signature is obsolete in MassTransit; kept for compatibility until upgrade.")]
    protected override void ConfigureConsumer(IReceiveEndpointConfigurator endpointConfigurator, IConsumerConfigurator<StartWorkflowConsumer> consumerConfigurator)
    {
        if (endpointConfigurator is IRabbitMqReceiveEndpointConfigurator rabbit)
        {
            rabbit.PrefetchCount = _options.PrefetchCount;
        }
    }
}
