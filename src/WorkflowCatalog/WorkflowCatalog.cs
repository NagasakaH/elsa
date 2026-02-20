using System.Collections.Concurrent;

namespace NagasakaEventSystem.WorkflowCatalog;

public class WorkflowCatalog
{
    private readonly ConcurrentDictionary<string, WorkflowCatalogEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string taskId, WorkflowCatalogEntry entry) => _entries[taskId] = entry;
    public bool TryGet(string taskId, out WorkflowCatalogEntry? entry) => _entries.TryGetValue(taskId, out entry);
    public bool Remove(string taskId) => _entries.TryRemove(taskId, out _);
    public IReadOnlyCollection<WorkflowCatalogEntry> List() => _entries.Values.ToList();
}
