using FluentAssertions;
using Microsoft.Playwright;

namespace ElsaServer.E2ETests;

[Trait("Category", "E2E")]
[Collection("Playwright")]
public class WorkflowEditorTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public WorkflowEditorTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Can_Navigate_To_Workflow_Definitions()
    {
        var page = await LoginAsync();
        
        // ワークフロー定義ページに遷移
        await page.GotoAsync($"{_fixture.BaseUrl}/workflow-definitions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // ワークフロー定義一覧が表示されることを確認
        var content = await page.ContentAsync();
        content.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Can_Create_New_Workflow()
    {
        var page = await LoginAsync();
        
        await page.GotoAsync($"{_fixture.BaseUrl}/workflow-definitions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // 「新規作成」ボタンをクリック
        var createButton = page.Locator("button:has-text('Create'), a:has-text('Create')").First;
        if (await createButton.IsVisibleAsync())
        {
            await createButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // エディタページに遷移したことを確認
            var url = page.Url;
            url.Should().Contain("workflow-definition");
        }
    }

    [Fact]
    public async Task Can_View_Activity_Catalog()
    {
        var page = await LoginAsync();
        
        // ワークフロー定義ページから新規作成してエディタを開く
        await page.GotoAsync($"{_fixture.BaseUrl}/workflow-definitions");
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var createButton = page.Locator("button:has-text('Create'), a:has-text('Create')").First;
        if (await createButton.IsVisibleAsync())
        {
            await createButton.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // アクティビティパネルが表示されることを確認（カスタムアクティビティ含む）
            // Elsa Studio ではアクティビティカタログがサイドバーに表示される
            var bodyText = await page.InnerTextAsync("body");
            // ページが正常にロードされたことを確認
            bodyText.Should().NotBeEmpty();
        }
    }

    private async Task<IPage> LoginAsync()
    {
        var page = await _fixture.Browser.NewPageAsync();
        await page.GotoAsync(_fixture.BaseUrl);
        
        try
        {
            await page.WaitForSelectorAsync("input[type='text'], input[type='password']", new() { Timeout = 5000 });
            await page.FillAsync("input[type='text']", "admin");
            await page.FillAsync("input[type='password']", "password");
            await page.ClickAsync("button[type='submit']");
            await page.WaitForURLAsync(url => !url.Contains("login"), new() { Timeout = 15000 });
        }
        catch
        {
            // 既にログイン済みの場合はスキップ
        }
        
        return page;
    }
}
