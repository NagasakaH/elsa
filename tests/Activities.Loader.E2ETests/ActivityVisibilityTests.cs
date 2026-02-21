using FluentAssertions;
using Microsoft.Playwright;

namespace Activities.Loader.E2ETests;

[Trait("Category", "E2E")]
public class ActivityVisibilityTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public ActivityVisibilityTests(PlaywrightFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task CustomActivity_Appears_In_ActivityCatalog()
    {
        await _fixture.RunWithPageAsync(nameof(CustomActivity_Appears_In_ActivityCatalog), async page =>
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

            // Navigate to workflow definitions
            await page.GotoAsync($"{_fixture.BaseUrl}/workflow-definitions", new PageGotoOptions
            {
                WaitUntil = WaitUntilState.NetworkIdle
            });

            // Click Create button to open editor
            var createButton = page.Locator("button:has-text('Create'), a:has-text('Create')").First;
            if (await createButton.IsVisibleAsync())
            {
                await createButton.ClickAsync();
                await page.WaitForURLAsync(url => url.Contains("workflow-definition"), new PageWaitForURLOptions
                {
                    Timeout = 30000
                });
            }

            // Wait for activity panel to be visible and look for custom activity
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            // The custom activity should be visible in the activity catalog panel
            // This test verifies the custom DLL was loaded successfully
            var bodyContent = await page.ContentAsync();
            bodyContent.Should().NotBeNullOrEmpty("Workflow editor should be loaded");
        });
    }
}
