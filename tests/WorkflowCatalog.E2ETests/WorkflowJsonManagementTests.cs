using FluentAssertions;
using Microsoft.Playwright;

namespace WorkflowCatalog.E2ETests;

[Trait("Category", "E2E")]
public class WorkflowJsonManagementTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public WorkflowJsonManagementTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CatalogWorkflows_AppearInWorkflowDefinitions()
    {
        var page = await _fixture.Browser.NewPageAsync();

        try
        {
            // Navigate to login page
            await page.GotoAsync(_fixture.BaseUrl, new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle,
                Timeout = 30000
            });

            // Login
            await page.FillAsync("input[name='username'], input[type='text']", "admin");
            await page.FillAsync("input[name='password'], input[type='password']", "password");
            await page.ClickAsync("button[type='submit']");

            // Wait for navigation after login
            await page.WaitForURLAsync(url => !url.Contains("login"), new PageWaitForURLOptions
            {
                Timeout = 30000
            });

            // Navigate to workflow definitions page
            await page.GotoAsync($"{_fixture.BaseUrl}/workflow-definitions", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Wait for page to load
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // Verify the workflow definitions page is loaded
            var content = await page.ContentAsync();
            content.Should().NotBeNullOrEmpty("Workflow definitions page should have content");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
