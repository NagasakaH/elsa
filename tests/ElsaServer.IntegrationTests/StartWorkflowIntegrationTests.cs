using System.Linq;
using Elsa.Workflows;
using Elsa.Workflows.Activities;
using Elsa.Workflows.Management.Models;
using Elsa.Workflows.Models;
using Elsa.Workflows.Options;
using Elsa.Workflows.State;
using ElsaServer.Consumers;
using ElsaServer.Messages;
using ElsaServer.Options;
using ElsaServer.Services;
using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NagasakaEventSystem.WorkflowCatalog;
using Xunit;

namespace ElsaServer.IntegrationTests;

[Trait("Category", "Integration")]
public class StartWorkflowIntegrationTests
{
    [Fact]
    public async Task StartWorkflowCommand_Publishes_Status_Events()
    {
        var services = new ServiceCollection();

        services.AddSingleton<IOptions<WorkflowBusOptions>>(Microsoft.Extensions.Options.Options.Create(new WorkflowBusOptions
        {
            StartQueueName = "workflow-start",
            StatusPublishRetryCount = 1,
            StatusPublishRetryDelaySeconds = 0
        }));

        services.AddSingleton<RunContextStore>();
        services.AddSingleton<WorkflowCatalog>();
        services.AddSingleton<IPayloadMapper, DefaultPayloadMapper>();
        services.AddSingleton<IWorkflowStatusPublisher, WorkflowStatusPublisher>();
        services.AddSingleton<WorkflowStatusPublisher>();
        services.AddSingleton<ILogger<WorkflowStatusPublisher>>(_ => NullLogger<WorkflowStatusPublisher>.Instance);
        services.AddSingleton<WorkflowLauncher>();
        services.AddSingleton<IWorkflowLauncher>(sp => sp.GetRequiredService<WorkflowLauncher>());
        services.AddSingleton<FakeWorkflowRunner>();
        services.AddSingleton<IWorkflowRunner>(sp => sp.GetRequiredService<FakeWorkflowRunner>());
        services.AddLogging();

        // Catalog entry for sample task.
        var catalog = new WorkflowCatalog();
        var entry = CreateCatalogEntry("sample-basic-flow");
        catalog.Set("sample-basic-flow", entry);
        services.AddSingleton(catalog);

        services.AddMassTransit(cfg =>
        {
            cfg.AddConsumer<StartWorkflowConsumer>();
            cfg.UsingInMemory((context, busCfg) =>
            {
                busCfg.ReceiveEndpoint("workflow-start", endpoint =>
                {
                    endpoint.ConfigureConsumer<StartWorkflowConsumer>(context);
                });
            });
        });

        await using var provider = services.BuildServiceProvider();
        var busControl = provider.GetRequiredService<IBusControl>();
        var bus = provider.GetRequiredService<IBus>();

        await busControl.StartAsync();

        try
        {
            // Publish StartWorkflowCommand.
            var endpoint = await bus.GetSendEndpoint(new Uri("queue:workflow-start"));

            await endpoint.Send(new StartWorkflowCommand
            {
                TaskId = "sample-basic-flow",
                RunTaskId = "run-1",
                Payload = new Dictionary<string, object>()
            });

            // Wait for consumer execution.
            var statusEvents = provider.GetRequiredService<FakeWorkflowRunner>().Events;

            for (var i = 0; i < 20 && statusEvents.Count < 2; i++)
            {
                await Task.Delay(100);
            }

            statusEvents.Select(x => x.Status).Should().Contain(new[] { WorkflowStatusKind.Running, WorkflowStatusKind.Finished });
        }
        finally
        {
            await busControl.StopAsync();
        }
    }

    private static WorkflowCatalogEntry CreateCatalogEntry(string taskId)
    {
        var rootActivity = new DummyActivity { Id = "root" };
        var rootNode = new ActivityNode(rootActivity, "Root");
        var workflow = new Workflow(rootActivity);
        var graph = new WorkflowGraph(workflow, rootNode, new[] { rootNode });

        var model = new WorkflowDefinitionModel
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

    private class FakeWorkflowRunner : IWorkflowRunner
    {
        private readonly IWorkflowStatusPublisher _statusPublisher;

        public List<WorkflowStatusEvent> Events { get; } = new();

        public FakeWorkflowRunner(IWorkflowStatusPublisher statusPublisher)
        {
            _statusPublisher = statusPublisher;
        }

        public Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default)
        {
            var properties = options?.Properties ?? new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var taskId = properties.TryGetValue(WorkflowPropertyKeys.TaskId, out var t) ? t?.ToString() : null;
            var runTaskId = properties.TryGetValue(WorkflowPropertyKeys.RunTaskId, out var r) ? r?.ToString() : null;

            if (!string.IsNullOrWhiteSpace(taskId) && !string.IsNullOrWhiteSpace(runTaskId))
            {
                var running = new WorkflowStatusEvent { TaskId = taskId!, RunTaskId = runTaskId!, Status = WorkflowStatusKind.Running, Detail = "Started", OccurredAtUtc = DateTimeOffset.UtcNow };
                var finished = new WorkflowStatusEvent { TaskId = taskId!, RunTaskId = runTaskId!, Status = WorkflowStatusKind.Finished, Detail = "Finished", OccurredAtUtc = DateTimeOffset.UtcNow };

                Events.Add(running);
                Events.Add(finished);

                _ = _statusPublisher.PublishAsync(taskId!, runTaskId!, WorkflowStatusKind.Running, "Started", cancellationToken);
                _ = _statusPublisher.PublishAsync(taskId!, runTaskId!, WorkflowStatusKind.Finished, "Finished", cancellationToken);
            }

            return Task.FromResult(new RunWorkflowResult(null!, null!, workflowGraph.Workflow, null, Journal.Empty));
        }

        #region Unused overloads
        public Task<RunWorkflowResult> RunAsync(Elsa.Workflows.IActivity activity, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default)
            => RunAsync(CreateGraph(activity), options, cancellationToken);

        public Task<RunWorkflowResult> RunAsync(IWorkflow workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<RunWorkflowResult<TResult>> RunAsync<TResult>(WorkflowBase<TResult> workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public Task<RunWorkflowResult> RunAsync<T>(RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) where T : IWorkflow, new()
            => throw new NotImplementedException();

        public Task<TResult> RunAsync<T, TResult>(RunWorkflowOptions? options = null, CancellationToken cancellationToken = default) where T : WorkflowBase<TResult>, new()
            => throw new NotImplementedException();

        public Task<RunWorkflowResult> RunAsync(Workflow workflow, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default)
            => RunAsync(CreateGraph(workflow.Root), options, cancellationToken);

        public Task<RunWorkflowResult> RunAsync(Workflow workflow, WorkflowState workflowState, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default)
            => RunAsync(CreateGraph(workflow.Root), options, cancellationToken);

        public Task<RunWorkflowResult> RunAsync(WorkflowGraph workflowGraph, WorkflowState workflowState, RunWorkflowOptions? options = null, CancellationToken cancellationToken = default)
            => RunAsync(workflowGraph, options, cancellationToken);

        public Task<RunWorkflowResult> RunAsync(WorkflowExecutionContext workflowExecutionContext)
            => throw new NotImplementedException();

        private static WorkflowGraph CreateGraph(Elsa.Workflows.IActivity activity)
        {
            var rootNode = new ActivityNode(activity, "Root");
            var workflow = new Workflow(activity);
            return new WorkflowGraph(workflow, rootNode, new[] { rootNode });
        }
        #endregion
    }
}
