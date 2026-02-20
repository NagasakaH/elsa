using System.Collections.Concurrent;

namespace ElsaServer.Services;

public class RunContextStore
{
    private readonly ConcurrentDictionary<string, string> _runs = new(StringComparer.OrdinalIgnoreCase);

    public bool TryAdd(string runTaskId, string taskId) => _runs.TryAdd(runTaskId, taskId);
    public bool Contains(string runTaskId) => _runs.ContainsKey(runTaskId);
    public bool TryRemove(string runTaskId, out string? taskId)
    {
        var removed = _runs.TryRemove(runTaskId, out var value);
        taskId = value;
        return removed;
    }
}
