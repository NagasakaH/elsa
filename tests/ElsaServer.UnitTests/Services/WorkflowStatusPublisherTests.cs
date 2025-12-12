using ElsaServer.Messages;
using ElsaServer.Options;
using ElsaServer.Services;
using MassTransit;
using Microsoft.Extensions.Logging.Abstractions;
using MicrosoftOptions = Microsoft.Extensions.Options.Options;
using NSubstitute;
using Xunit;

namespace ElsaServer.UnitTests.Services;

public class WorkflowStatusPublisherTests
{
    [Fact]
    public async Task PublishAsync_Sends_Status_Event()
    {
        var publishEndpoint = Substitute.For<IPublishEndpoint>();
        var store = new RunContextStore();
        var options = MicrosoftOptions.Create(new WorkflowBusOptions { StatusPublishRetryCount = 1, StatusPublishRetryDelaySeconds = 0 });
        var publisher = new WorkflowStatusPublisher(publishEndpoint, store, options, NullLogger<WorkflowStatusPublisher>.Instance);

        await publisher.PublishAsync("task-1", "run-1", WorkflowStatusKind.Running, "detail", CancellationToken.None);

        await publishEndpoint.Received(1).Publish(
            Arg.Is<WorkflowStatusEvent>(m =>
                m.TaskId == "task-1" &&
                m.RunTaskId == "run-1" &&
                m.Status == WorkflowStatusKind.Running &&
                m.Detail == "detail"),
            Arg.Any<CancellationToken>());
    }
}
