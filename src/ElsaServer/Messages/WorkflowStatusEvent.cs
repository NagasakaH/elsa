using System.ComponentModel;

namespace ElsaServer.Messages;

[DisplayName("WorkflowStatusEvent")]
public class WorkflowStatusEvent
{
    public string TaskId { get; set; } = string.Empty;
    public string RunTaskId { get; set; } = string.Empty;
    public WorkflowStatusKind Status { get; set; } = WorkflowStatusKind.Running;
    public string? Detail { get; set; }
        = string.Empty;
    public DateTimeOffset OccurredAtUtc { get; set; } = DateTimeOffset.UtcNow;
}

public enum WorkflowStatusKind
{
    Running,
    Suspended,
    Finished,
    Error
}
