using ElsaServer.Messages;
using ElsaServer.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace ElsaServer.Consumers;

public class StartWorkflowConsumer : IConsumer<StartWorkflowCommand>
{
    private readonly IWorkflowLauncher _launcher;
    private readonly ILogger<StartWorkflowConsumer> _logger;

    public StartWorkflowConsumer(IWorkflowLauncher launcher, ILogger<StartWorkflowConsumer> logger)
    {
        _launcher = launcher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StartWorkflowCommand> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received StartWorkflowCommand TaskId={TaskId} RunTaskId={RunTaskId}", message.TaskId, message.RunTaskId);
        await _launcher.LaunchAsync(message, context.CancellationToken);
    }
}
