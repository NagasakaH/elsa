using Elsa.Workflows;
using Elsa.Workflows.Options;
using ElsaServer.Messages;
using Microsoft.Extensions.Logging;
using NagasakaEventSystem.WorkflowCatalog;

namespace ElsaServer.Services;

public interface IWorkflowLauncher
{
    Task LaunchAsync(StartWorkflowCommand command, CancellationToken cancellationToken);
}

public class WorkflowLauncher : IWorkflowLauncher
{
    private readonly WorkflowCatalog _catalog;
    private readonly RunContextStore _runContextStore;
    private readonly IPayloadMapper _payloadMapper;
    private readonly IWorkflowRunner _workflowRunner;
    private readonly IWorkflowStatusPublisher _statusPublisher;
    private readonly ILogger<WorkflowLauncher> _logger;

    public WorkflowLauncher(
        WorkflowCatalog catalog,
        RunContextStore runContextStore,
        IPayloadMapper payloadMapper,
        IWorkflowRunner workflowRunner,
        IWorkflowStatusPublisher statusPublisher,
        ILogger<WorkflowLauncher> logger)
    {
        _catalog = catalog;
        _runContextStore = runContextStore;
        _payloadMapper = payloadMapper;
        _workflowRunner = workflowRunner;
        _statusPublisher = statusPublisher;
        _logger = logger;
    }

    public async Task LaunchAsync(StartWorkflowCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.TaskId) || string.IsNullOrWhiteSpace(command.RunTaskId))
        {
            _logger.LogWarning("Invalid command: TaskId or RunTaskId is null/empty");
            await _statusPublisher.PublishAsync(command.TaskId ?? "", command.RunTaskId ?? "", WorkflowStatusKind.Error, "TaskId and RunTaskId are required", cancellationToken);
            return;
        }

        if (!_catalog.TryGet(command.TaskId, out var entry))
        {
            await _statusPublisher.PublishAsync(command.TaskId, command.RunTaskId, WorkflowStatusKind.Error, "Unknown TaskId", cancellationToken);
            return;
        }

        var workflowEntry = entry ?? throw new InvalidOperationException($"Workflow entry not found for TaskId={command.TaskId}");

        if (!_runContextStore.TryAdd(command.RunTaskId, command.TaskId))
        {
            await _statusPublisher.PublishAsync(command.TaskId, command.RunTaskId, WorkflowStatusKind.Error, "Duplicated RunTaskId", cancellationToken);
            return;
        }

        try
        {
            var options = new RunWorkflowOptions
            {
                CorrelationId = command.RunTaskId,
                Input = _payloadMapper.Map(command.Payload),
                Properties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    [WorkflowPropertyKeys.TaskId] = command.TaskId,
                    [WorkflowPropertyKeys.RunTaskId] = command.RunTaskId
                }
            };

            await _workflowRunner.RunAsync(workflowEntry.WorkflowGraph, options, cancellationToken);
        }
        catch (Exception ex)
        {
            _runContextStore.TryRemove(command.RunTaskId, out _);
            await _statusPublisher.PublishAsync(command.TaskId, command.RunTaskId, WorkflowStatusKind.Error, ex.Message, cancellationToken);
            _logger.LogError(ex, "Failed to launch workflow TaskId={TaskId} RunTaskId={RunTaskId}", command.TaskId, command.RunTaskId);
        }
    }
}
