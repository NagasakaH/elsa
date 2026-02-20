using FluentAssertions;
using NagasakaEventSystem.WorkflowCatalog;

namespace WorkflowCatalog.UnitTests;

public class WorkflowCatalogTests
{
    [Fact]
    public void Set_AddsEntryToCatalog()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();
        var entry = CreateEntry("task-1");

        // Act
        catalog.Set("task-1", entry);

        // Assert
        catalog.TryGet("task-1", out var retrieved).Should().BeTrue();
        retrieved.Should().Be(entry);
    }

    [Fact]
    public void TryGet_ReturnsFalse_WhenTaskIdNotFound()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();

        // Act
        var result = catalog.TryGet("nonexistent", out var entry);

        // Assert
        result.Should().BeFalse();
        entry.Should().BeNull();
    }

    [Fact]
    public void TryGet_IsCaseInsensitive()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();
        var entry = CreateEntry("Task-1");
        catalog.Set("Task-1", entry);

        // Act
        var result = catalog.TryGet("TASK-1", out var retrieved);

        // Assert
        result.Should().BeTrue();
        retrieved.Should().Be(entry);
    }

    [Fact]
    public void Remove_RemovesEntryFromCatalog()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();
        var entry = CreateEntry("task-1");
        catalog.Set("task-1", entry);

        // Act
        var result = catalog.Remove("task-1");

        // Assert
        result.Should().BeTrue();
        catalog.TryGet("task-1", out _).Should().BeFalse();
    }

    [Fact]
    public void Remove_ReturnsFalse_WhenTaskIdNotFound()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();

        // Act
        var result = catalog.Remove("nonexistent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void List_ReturnsAllEntries()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();
        var entry1 = CreateEntry("task-1");
        var entry2 = CreateEntry("task-2");
        catalog.Set("task-1", entry1);
        catalog.Set("task-2", entry2);

        // Act
        var entries = catalog.List();

        // Assert
        entries.Should().HaveCount(2);
        entries.Should().Contain(entry1);
        entries.Should().Contain(entry2);
    }

    [Fact]
    public void Set_OverwritesExistingEntry()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();
        var entry1 = CreateEntry("task-1", "/path/v1");
        var entry2 = CreateEntry("task-1", "/path/v2");
        catalog.Set("task-1", entry1);

        // Act
        catalog.Set("task-1", entry2);

        // Assert
        catalog.TryGet("task-1", out var retrieved).Should().BeTrue();
        retrieved!.SourcePath.Should().Be("/path/v2");
        catalog.List().Should().HaveCount(1);
    }

    private static WorkflowCatalogEntry CreateEntry(string taskId, string sourcePath = "/test/path")
    {
        return new WorkflowCatalogEntry(taskId, null!, null!, sourcePath);
    }
}
