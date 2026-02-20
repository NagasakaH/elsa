using System.Text.RegularExpressions;
using Elsa.Workflows;
using Elsa.Common.Models;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Management;
using Elsa.Workflows.Management.Filters;
using Elsa.Workflows.Management.Entities;
using Elsa.Workflows.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ElsaServer.Options;

namespace ElsaServer.Services;

public class WorkflowCatalogLoader
{
    private static readonly Regex FileNamePattern = new("^workflow-(?<taskId>[a-zA-Z0-9_-]+)\\.json$", RegexOptions.Compiled);
    private readonly IActivitySerializer _activitySerializer;
    private readonly WorkflowDefinitionMapper _definitionMapper;
    private readonly IWorkflowGraphBuilder _graphBuilder;
    private readonly WorkflowCatalog _catalog;
    private readonly WorkflowCatalogOptions _options;
    private readonly ILogger<WorkflowCatalogLoader> _logger;
    private readonly IHostEnvironment _environment;
    private readonly IWorkflowDefinitionStore _definitionStore;

    public WorkflowCatalogLoader(
        IActivitySerializer activitySerializer,
        WorkflowDefinitionMapper definitionMapper,
        IWorkflowGraphBuilder graphBuilder,
        WorkflowCatalog catalog,
        IOptions<WorkflowCatalogOptions> options,
        ILogger<WorkflowCatalogLoader> logger,
        IHostEnvironment environment,
        IWorkflowDefinitionStore definitionStore)
    {
        _activitySerializer = activitySerializer;
        _definitionMapper = definitionMapper;
        _graphBuilder = graphBuilder;
        _catalog = catalog;
        _logger = logger;
        _options = options.Value;
        _environment = environment;
        _definitionStore = definitionStore;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var basePath = ResolvePath(_options.Directory);
        if (!Directory.Exists(basePath))
        {
            if (_options.Strict)
                throw new DirectoryNotFoundException($"Workflow directory not found: {basePath}");

            _logger.LogWarning("Workflow directory not found: {BasePath}", basePath);
            return;
        }

        var files = Directory.EnumerateFiles(basePath, "workflow-*.json", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            await LoadFileAsync(file, cancellationToken);
        }
    }

    private async Task LoadFileAsync(string filePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(filePath);
        var match = FileNamePattern.Match(fileName);

        if (!match.Success)
        {
            HandleError($"Filename does not follow workflow-<TaskId>.json: {fileName}");
            return;
        }

        var taskId = match.Groups["taskId"].Value;

        try
        {
            var json = await File.ReadAllTextAsync(filePath, cancellationToken);
            var model = _activitySerializer.Deserialize<WorkflowDefinitionModel>(json);

            if (model.Root == null)
            {
                HandleError($"Root activity missing in {fileName}");
                return;
            }

            ApplyDefaults(taskId, model);

            var workflow = _definitionMapper.Map(model);
            var graph = await _graphBuilder.BuildAsync(workflow, cancellationToken);

            _catalog.Set(taskId, new(taskId, graph, model, filePath));
            await UpsertDefinitionAsync(model, json, cancellationToken);
            _logger.LogInformation("Workflow loaded: {TaskId} from {FilePath}", taskId, filePath);
        }
        catch (Exception ex)
        {
            HandleError($"Failed to load {fileName}: {ex.Message}");
        }
    }

    private void ApplyDefaults(string taskId, WorkflowDefinitionModel model)
    {
        model.DefinitionId = string.IsNullOrWhiteSpace(model.DefinitionId) ? taskId : model.DefinitionId.Trim();
        model.Name ??= taskId;
        model.Id ??= $"{model.DefinitionId}-v{model.Version}";
        model.CreatedAt = model.CreatedAt == default ? DateTimeOffset.UtcNow : model.CreatedAt;
        model.IsLatest = true;
    }

    private void HandleError(string message)
    {
        if (_options.Strict)
            throw new InvalidOperationException(message);

        _logger.LogWarning(message);
    }

    private async Task UpsertDefinitionAsync(WorkflowDefinitionModel model, string originalJson, CancellationToken cancellationToken)
    {
        // Purge existing versions to avoid persisting NotFound-serialized activities from prior runs.
        await _definitionStore.DeleteAsync(new WorkflowDefinitionFilter
        {
            DefinitionId = model.DefinitionId,
            TenantAgnostic = true
        }, cancellationToken);

        model.Version = 1;
        model.Id = string.IsNullOrWhiteSpace(model.Id) ? $"{model.DefinitionId}-v{model.Version}" : model.Id;
        model.IsLatest = true;
        model.IsPublished = true;

        var entity = _definitionMapper.MapToWorkflowDefinition(model);
        await _definitionStore.SaveAsync(entity, cancellationToken);
    }

    private string ResolvePath(string directory)
    {
        if (Path.IsPathRooted(directory))
            return EnsureWithinContentRoot(directory);

        var candidates = new[]
        {
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, directory)),
            Path.GetFullPath(Path.Combine(_environment.ContentRootPath, "..", "..", directory))
        };

        var existing = candidates.FirstOrDefault(Directory.Exists);
        return EnsureWithinContentRoot(existing ?? candidates.Last());
    }

    private string EnsureWithinContentRoot(string resolvedPath)
    {
        var root = Path.GetFullPath(_environment.ContentRootPath);
        var parentRoot = Path.GetFullPath(Path.Combine(root, "..", ".."));
        var fullPath = Path.GetFullPath(resolvedPath);
        if (!fullPath.StartsWith(root, StringComparison.OrdinalIgnoreCase) &&
            !fullPath.StartsWith(parentRoot, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException($"Path traversal detected: {resolvedPath} is outside allowed directories.");
        return fullPath;
    }
}
