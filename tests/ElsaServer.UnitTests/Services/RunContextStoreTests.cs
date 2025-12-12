using ElsaServer.Services;
using FluentAssertions;
using Xunit;

namespace ElsaServer.UnitTests.Services;

public class RunContextStoreTests
{
    [Fact]
    public void TryAdd_Allows_First_Entry()
    {
        var store = new RunContextStore();

        var added = store.TryAdd("run-1", "task-1");

        added.Should().BeTrue();
        store.Contains("run-1").Should().BeTrue();
    }

    [Fact]
    public void TryAdd_Denies_Duplicate_RunTaskId()
    {
        var store = new RunContextStore();
        store.TryAdd("run-1", "task-1");

        var added = store.TryAdd("run-1", "task-2");

        added.Should().BeFalse();
        store.Contains("run-1").Should().BeTrue();
    }

    [Fact]
    public void TryRemove_Removes_And_Returns_TaskId()
    {
        var store = new RunContextStore();
        store.TryAdd("run-1", "task-1");

        var removed = store.TryRemove("run-1", out var taskId);

        removed.Should().BeTrue();
        taskId.Should().Be("task-1");
        store.Contains("run-1").Should().BeFalse();
    }
}
