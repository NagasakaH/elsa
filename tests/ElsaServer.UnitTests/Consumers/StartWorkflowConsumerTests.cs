using ElsaServer.Consumers;
using ElsaServer.Messages;
using ElsaServer.Services;
using MassTransit;
using NSubstitute;
using Xunit;

namespace ElsaServer.UnitTests.Consumers;

public class StartWorkflowConsumerTests
{
    [Fact]
    public async Task Consume_Invokes_WorkflowLauncher()
    {
        var launcher = Substitute.For<IWorkflowLauncher>();
        var consumer = new StartWorkflowConsumer(launcher, Substitute.For<Microsoft.Extensions.Logging.ILogger<StartWorkflowConsumer>>());
        var message = new StartWorkflowCommand { TaskId = "task", RunTaskId = "run" };
        var context = Substitute.For<ConsumeContext<StartWorkflowCommand>>();
        context.Message.Returns(message);

        await consumer.Consume(context);

        await launcher.Received(1).LaunchAsync(message, Arg.Any<CancellationToken>());
    }
}
