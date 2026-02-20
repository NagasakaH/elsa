namespace ElsaServer.Options;

public class WorkflowBusOptions
{
    public string StartQueueName { get; set; } = "workflow-start";
    public ushort PrefetchCount { get; set; } = 16;
    public int? ConcurrentMessageLimit { get; set; } = 8;
    public int StatusPublishRetryCount { get; set; } = 3;
    public int StatusPublishRetryDelaySeconds { get; set; } = 2;
}
