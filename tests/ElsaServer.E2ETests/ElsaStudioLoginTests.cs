using FluentAssertions;
using Microsoft.Playwright;

namespace ElsaServer.E2ETests;

[Trait("Category", "E2E")]
[Collection("Playwright")]
public class ElsaStudioLoginTests : IClassFixture<PlaywrightFixture>
{
    private readonly PlaywrightFixture _fixture;

    public ElsaStudioLoginTests(PlaywrightFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Can_Login_To_ElsaStudio()
    {
        await _fixture.RunWithPageAsync(nameof(Can_Login_To_ElsaStudio), async page =>
        {
            await page.GotoAsync(_fixture.BaseUrl);

            // ログインページが表示されることを確認
            await page.WaitForSelectorAsync("input[type='text'], input[type='password']", new() { Timeout = 10000 });

            // デフォルト管理者でログイン
            // Elsa Studio のデフォルトログイン: admin / password
            await page.FillAsync("input[type='text']", "admin");
            await page.FillAsync("input[type='password']", "password");
            await page.ClickAsync("button[type='submit']");

            // ダッシュボードまたはワークフロー一覧が表示されるまで待機
            await page.WaitForURLAsync(url => !url.Contains("login"), new() { Timeout = 15000 });

            var url = page.Url;
            url.Should().NotContain("login");
        });
    }
}
