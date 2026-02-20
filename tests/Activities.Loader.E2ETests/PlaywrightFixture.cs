using Microsoft.Playwright;

namespace Activities.Loader.E2ETests;

public class PlaywrightFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    // テスト用ベースURL（環境変数で上書き可能）
    public string BaseUrl => Environment.GetEnvironmentVariable("ELSA_BASE_URL") ?? "https://localhost:5001";

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
            Args = new[] { "--ignore-certificate-errors" }
        });
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}
