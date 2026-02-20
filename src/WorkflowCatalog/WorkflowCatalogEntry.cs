using Elsa.Workflows.Models;
using Elsa.Workflows.Management.Models;

namespace NagasakaEventSystem.WorkflowCatalog;

public record WorkflowCatalogEntry(
    string TaskId,
    WorkflowGraph WorkflowGraph,
    WorkflowDefinitionModel Model,
    string SourcePath);
