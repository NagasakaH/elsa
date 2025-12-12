using System.Text.RegularExpressions;
using Elsa.Workflows;
using Elsa.Workflows.Management.Mappers;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
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

    public WorkflowCatalogLoader(
        IActivitySerializer activitySerializer,
        WorkflowDefinitionMapper definitionMapper,
        IWorkflowGraphBuilder graphBuilder,
        WorkflowCatalog catalog,
        IOptions<WorkflowCatalogOptions> options,
        ILogger<WorkflowCatalogLoader> logger)
    {
        _activitySerializer = activitySerializer;
        _definitionMapper = definitionMapper;
        _graphBuilder = graphBuilder;
        _catalog = catalog;
        _logger = logger;
        _options = options.Value;
    }

    public async Task LoadAsync(CancellationToken cancellationToken)
    {
        var basePath = Path.GetFullPath(_options.Directory);
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
}
