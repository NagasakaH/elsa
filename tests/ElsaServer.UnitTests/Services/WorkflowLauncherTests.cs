using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using ElsaServer.Messages;
using ElsaServer.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NagasakaEventSystem.WorkflowCatalog;
using NSubstitute;
using Xunit;

namespace ElsaServer.UnitTests.Services;

public class WorkflowLauncherTests
{
    [Fact]
    public async Task Unknown_TaskId_Publishes_Error()
    {
        var (launcher, runner, statusPublisher, _, _) = CreateSut();
        var command = new StartWorkflowCommand { TaskId = "unknown", RunTaskId = "run-1" };

        await launcher.LaunchAsync(command, CancellationToken.None);

        await statusPublisher.Received(1)
            .PublishAsync("unknown", "run-1", WorkflowStatusKind.Error, "Unknown TaskId", Arg.Any<CancellationToken>());
        await runner.DidNotReceiveWithAnyArgs().RunAsync(default(WorkflowGraph)!, default(RunWorkflowOptions)!, default);
    }

    [Fact]
    public async Task Duplicate_RunTaskId_Publishes_Error()
    {
        var catalog = new WorkflowCatalog();
        var entry = CreateCatalogEntry("sample-task");
        catalog.Set("sample-task", entry);

        var runContextStore = new RunContextStore();
        runContextStore.TryAdd("run-1", "sample-task");

        var (launcher, runner, statusPublisher, _, _) = CreateSut(catalog, runContextStore);
        var command = new StartWorkflowCommand { TaskId = "sample-task", RunTaskId = "run-1" };

        await launcher.LaunchAsync(command, CancellationToken.None);

        await statusPublisher.Received(1)
            .PublishAsync("sample-task", "run-1", WorkflowStatusKind.Error, "Duplicated RunTaskId", Arg.Any<CancellationToken>());
        await runner.DidNotReceiveWithAnyArgs().RunAsync(default(WorkflowGraph)!, default(RunWorkflowOptions)!, default);
    }

    [Fact]
    public async Task Launches_Workflow_With_Correlation_And_Properties()
    {
        var catalog = new WorkflowCatalog();
        var entry = CreateCatalogEntry("sample-task");
        catalog.Set("sample-task", entry);

        var runContextStore = new RunContextStore();
        var payloadMapper = Substitute.For<IPayloadMapper>();
        var mappedPayload = new Dictionary<string, object> { { "key", "value" } };
        payloadMapper.Map(Arg.Any<IDictionary<string, object>>()).Returns(mappedPayload);

        var (launcher, runner, statusPublisher, _, _) = CreateSut(catalog, runContextStore, payloadMapper);
        var command = new StartWorkflowCommand { TaskId = "sample-task", RunTaskId = "run-1", Payload = new Dictionary<string, object>() };

        runner
            .RunAsync(entry.WorkflowGraph, Arg.Any<RunWorkflowOptions>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new RunWorkflowResult(null!, null!, entry.WorkflowGraph.Workflow, null, Journal.Empty)));

        await launcher.LaunchAsync(command, CancellationToken.None);

        await statusPublisher.DidNotReceiveWithAnyArgs()
            .PublishAsync(default!, default!, default, default, default);

        var runCall = runner.ReceivedCalls().Single();
        var options = (RunWorkflowOptions)runCall.GetArguments()[1]!;

        options.CorrelationId.Should().Be("run-1");
        options.Properties.Should().ContainKey(WorkflowPropertyKeys.TaskId);
        options.Properties.Should().ContainKey(WorkflowPropertyKeys.RunTaskId);
        runContextStore.Contains("run-1").Should().BeTrue();
    }

    private static (WorkflowLauncher launcher, IWorkflowRunner runner, IWorkflowStatusPublisher statusPublisher, WorkflowCatalog catalog, RunContextStore runContextStore) CreateSut(
        WorkflowCatalog? catalog = null,
        RunContextStore? runContextStore = null,
        IPayloadMapper? payloadMapper = null)
    {
        catalog ??= new WorkflowCatalog();
        runContextStore ??= new RunContextStore();
        payloadMapper ??= Substitute.For<IPayloadMapper>();
        payloadMapper.Map(Arg.Any<IDictionary<string, object>>()).Returns(call => call.ArgAt<IDictionary<string, object>>(0) ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase));

        var runner = Substitute.For<IWorkflowRunner>();
        var statusPublisher = Substitute.For<IWorkflowStatusPublisher>();
        var launcher = new WorkflowLauncher(catalog, runContextStore, payloadMapper, runner, statusPublisher, NullLogger<WorkflowLauncher>.Instance);
        return (launcher, runner, statusPublisher, catalog, runContextStore);
    }

    private static WorkflowCatalogEntry CreateCatalogEntry(string taskId)
    {
        var rootActivity = new DummyActivity { Id = "root" };
        var rootNode = new ActivityNode(rootActivity, "Root");
        var workflow = new Workflow(rootActivity);
        var graph = new WorkflowGraph(workflow, rootNode, new[] { rootNode });

        var model = new Elsa.Workflows.Management.Models.WorkflowDefinitionModel
        {
            DefinitionId = taskId,
            Name = taskId,
            Id = $"{taskId}-v1",
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow,
            IsLatest = true
        };

        return new WorkflowCatalogEntry(taskId, graph, model, "in-memory");
    }

    private class DummyActivity : Activity
    {
    }
}
