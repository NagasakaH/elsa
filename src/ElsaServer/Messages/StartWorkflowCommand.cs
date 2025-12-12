using System.ComponentModel;

namespace ElsaServer.Messages;

[DisplayName("StartWorkflowCommand")]
public class StartWorkflowCommand
{
    public string TaskId { get; set; } = string.Empty;
    public string RunTaskId { get; set; } = string.Empty;
    public IDictionary<string, object>? Payload { get; set; }
        = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string>? Headers { get; set; }
        = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
