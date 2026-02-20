using FluentAssertions;

namespace WorkflowCatalog.IntegrationTests;

[Trait("Category", "Integration")]
public class WorkflowCatalogLoadTests
{
    [Fact]
    public void TestDataFile_Exists()
    {
        // Arrange
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "workflow-test.json");

        // Act & Assert
        File.Exists(testDataPath).Should().BeTrue($"Test data file should exist at {testDataPath}");
    }

    [Fact]
    public void TestDataFile_ContainsValidJson()
    {
        // Arrange
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData", "workflow-test.json");

        // Act
        var json = File.ReadAllText(testDataPath);

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("definitionId");
        json.Should().Contain("root");
    }

    [Fact]
    public void WorkflowCatalog_CanStoreAndRetrieve_EntryWithNullGraph()
    {
        // Arrange
        var catalog = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalog();
        var entry = new NagasakaEventSystem.WorkflowCatalog.WorkflowCatalogEntry(
            "test-task",
            null!,
            null!,
            "/test/path/workflow-test.json"
        );

        // Act
        catalog.Set("test-task", entry);
        var success = catalog.TryGet("test-task", out var retrieved);

        // Assert
        success.Should().BeTrue();
        retrieved.Should().NotBeNull();
        retrieved!.TaskId.Should().Be("test-task");
        retrieved.SourcePath.Should().Be("/test/path/workflow-test.json");
    }
}
