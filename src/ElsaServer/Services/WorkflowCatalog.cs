using System.Collections.Concurrent;
using Elsa.Workflows.Models;

namespace ElsaServer.Services;

public class WorkflowCatalog
{
    private readonly ConcurrentDictionary<string, WorkflowCatalogEntry> _entries = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string taskId, WorkflowCatalogEntry entry) => _entries[taskId] = entry;
    public bool TryGet(string taskId, out WorkflowCatalogEntry? entry) => _entries.TryGetValue(taskId, out entry);
    public IReadOnlyCollection<WorkflowCatalogEntry> List() => _entries.Values.ToList();
}

public record WorkflowCatalogEntry(
    string TaskId,
    WorkflowGraph WorkflowGraph,
    Elsa.Workflows.Management.Models.WorkflowDefinitionModel Model,
    string SourcePath);
