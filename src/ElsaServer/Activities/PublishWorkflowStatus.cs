using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Attributes;
using Elsa.Workflows.Models;
using Elsa.Workflows.Runtime;
using ElsaServer.Messages;
using ElsaServer.Services;
using MassTransit;

namespace ElsaServer.Activities;

/// <summary>
/// MassTransitでWorkflowStatusEventをPublishするアクティビティ。
/// RunTaskId/TaskIdは入力が空の場合、WorkflowPropertiesから補完する。
/// </summary>
[Activity("ElsaServer", "Messaging", "Publish workflow status via MassTransit.")]
public class PublishWorkflowStatus : CodeActivity
{
    /// <summary>TaskId。空の場合はWorkflowPropertyから補完。</summary>
    public Input<string>? TaskId { get; set; }

    /// <summary>RunTaskId。空の場合はWorkflowPropertyから補完。</summary>
    public Input<string>? RunTaskId { get; set; }

    /// <summary>通知するStatus。既定はRunning。</summary>
    public Input<WorkflowStatusKind> Status { get; set; } = new(WorkflowStatusKind.Running);

    /// <summary>詳細メッセージ。</summary>
    public Input<string?>? Detail { get; set; }

    protected override async ValueTask ExecuteAsync(ActivityExecutionContext context)
    {
        var publishEndpoint = context.GetService<IPublishEndpoint>() ?? throw new InvalidOperationException("IPublishEndpoint is not registered.");
        var status = context.Get(Status);
        var detail = Detail != null ? context.Get(Detail) : null;

        var taskId = TaskId != null ? context.Get(TaskId) : null;
        var runTaskId = RunTaskId != null ? context.Get(RunTaskId) : null;

        taskId ??= context.GetProperty<string>(WorkflowPropertyKeys.TaskId);
        runTaskId ??= context.GetProperty<string>(WorkflowPropertyKeys.RunTaskId);

        if (string.IsNullOrWhiteSpace(taskId))
            throw new InvalidOperationException("TaskId is required for status publish.");
        if (string.IsNullOrWhiteSpace(runTaskId))
            throw new InvalidOperationException("RunTaskId is required for status publish.");

        var message = new WorkflowStatusEvent
        {
            TaskId = taskId,
            RunTaskId = runTaskId,
            Status = status,
            Detail = detail,
            OccurredAtUtc = DateTimeOffset.UtcNow
        };

        await publishEndpoint.Publish(message, context.CancellationToken);
    }
}
