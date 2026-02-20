using Elsa.Mediator.Contracts;
using Elsa.Workflows;
using Elsa.Workflows.Notifications;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ElsaServer.Messages;
using ElsaServer.Options;

namespace ElsaServer.Services;

public interface IWorkflowStatusPublisher
{
    Task PublishAsync(string taskId, string runTaskId, WorkflowStatusKind status, string? detail, CancellationToken cancellationToken);
}

public class WorkflowStatusPublisher :
    IWorkflowStatusPublisher,
    INotificationHandler<WorkflowStarted>,
    INotificationHandler<WorkflowExecuted>,
    INotificationHandler<WorkflowFinished>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly RunContextStore _runContextStore;
    private readonly WorkflowBusOptions _options;
    private readonly ILogger<WorkflowStatusPublisher> _logger;

    public WorkflowStatusPublisher(
        IPublishEndpoint publishEndpoint,
        RunContextStore runContextStore,
        IOptions<WorkflowBusOptions> options,
        ILogger<WorkflowStatusPublisher> logger)
    {
        _publishEndpoint = publishEndpoint;
        _runContextStore = runContextStore;
        _logger = logger;
        _options = options.Value;
    }

    public async Task PublishAsync(string taskId, string runTaskId, WorkflowStatusKind status, string? detail, CancellationToken cancellationToken)
    {
        var message = new WorkflowStatusEvent
        {
            TaskId = taskId,
            RunTaskId = runTaskId,
            Status = status,
            Detail = detail,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        await PublishWithRetryAsync(message, cancellationToken);
    }

    public async Task HandleAsync(WorkflowStarted notification, CancellationToken cancellationToken)
    {
        if (!TryGetIdentifiers(notification.WorkflowExecutionContext, out var taskId, out var runTaskId))
            return;

        await PublishAsync(taskId, runTaskId, WorkflowStatusKind.Running, "Started", cancellationToken);
    }

    public async Task HandleAsync(WorkflowExecuted notification, CancellationToken cancellationToken)
    {
        if (!TryGetIdentifiers(notification.WorkflowExecutionContext, out var taskId, out var runTaskId))
            return;

        var subStatus = notification.WorkflowState.SubStatus;
        if (subStatus == WorkflowSubStatus.Suspended)
        {
            await PublishAsync(taskId, runTaskId, WorkflowStatusKind.Suspended, "Suspended", cancellationToken);
            _runContextStore.TryRemove(runTaskId, out _);
        }
        else if (subStatus == WorkflowSubStatus.Faulted)
        {
            var detail = notification.WorkflowState.Incidents.FirstOrDefault()?.Message;
            await PublishAsync(taskId, runTaskId, WorkflowStatusKind.Error, detail, cancellationToken);
            _runContextStore.TryRemove(runTaskId, out _);
        }
    }

    public async Task HandleAsync(WorkflowFinished notification, CancellationToken cancellationToken)
    {
        if (!TryGetIdentifiers(notification.WorkflowExecutionContext, out var taskId, out var runTaskId))
            return;

        await PublishAsync(taskId, runTaskId, WorkflowStatusKind.Finished, "Finished", cancellationToken);
        _runContextStore.TryRemove(runTaskId, out _);
    }

    private async Task PublishWithRetryAsync(WorkflowStatusEvent message, CancellationToken cancellationToken)
    {
        var attempts = Math.Max(1, _options.StatusPublishRetryCount);
        for (var i = 1; i <= attempts; i++)
        {
            try
            {
                await _publishEndpoint.Publish(message, cancellationToken);
                return;
            }
            catch (Exception ex) when (i < attempts)
            {
                _logger.LogWarning(ex, "Failed to publish status event (attempt {Attempt}/{Attempts})", i, attempts);
                await Task.Delay(TimeSpan.FromSeconds(_options.StatusPublishRetryDelaySeconds), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Final attempt to publish status event failed (attempt {Attempt}/{Attempts})", i, attempts);
                throw;
            }
        }
    }

    private bool TryGetIdentifiers(WorkflowExecutionContext context, out string taskId, out string runTaskId)
    {
        taskId = context.GetProperty<string>(WorkflowPropertyKeys.TaskId) ?? string.Empty;
        runTaskId = context.GetProperty<string>(WorkflowPropertyKeys.RunTaskId) ?? string.Empty;
        return !string.IsNullOrWhiteSpace(taskId) && !string.IsNullOrWhiteSpace(runTaskId);
    }
}
