using Elsa.Workflows;
using Elsa.Workflows.Models;
using Elsa.Workflows.State;
using FluentAssertions;
using NagasakaEventSystem.Activities.Testing;
using NagasakaEventSystem.Activities.Templates.CustomActivityTemplate;
using Xunit;
using Xunit.Abstractions;

namespace Activities.Templates.CustomActivityTemplate.UnitTests;

public class SampleCustomActivityTests
{
    private readonly ITestOutputHelper _output;

    public SampleCustomActivityTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public async Task Executes_And_Sets_Result()
    {
        var fixture = new ActivityTestFixture(_output)
            .AddActivitiesFrom<SampleCustomActivity>();

        var activity = new SampleCustomActivity
        {
            Text = new Input<string>("Hello")
        };

        var result = await fixture.RunActivityAsync(activity);

        result.WorkflowState.Status.Should().Be(WorkflowStatus.Finished);

        var activityCtx = result.Journal.ActivityExecutionContexts.Single(x => x.Activity.Id == activity.Id);
        var outputValue = activityCtx.GetOutputs()[nameof(SampleCustomActivity.Result)];
        outputValue.Should().Be("Echo:Hello");
    }
}
