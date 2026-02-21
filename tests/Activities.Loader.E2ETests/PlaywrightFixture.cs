using Microsoft.Playwright;
using System.Text.RegularExpressions;

namespace Activities.Loader.E2ETests;

public class PlaywrightFixture : IAsyncLifetime
{
    private static readonly Regex InvalidFileNameCharsRegex = new(
        $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]",
        RegexOptions.Compiled);

    public IPlaywright Playwright { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    // テスト用ベースURL（環境変数で上書き可能）
    public string BaseUrl => Environment.GetEnvironmentVariable("ELSA_BASE_URL") ?? "https://localhost:5001";
    public string ScreenshotRootDirectory => Environment.GetEnvironmentVariable("E2E_SCREENSHOT_DIR")
        ?? Path.Combine(AppContext.BaseDirectory, "artifacts", "screenshots");
    private string? ChromeNovncCdpUrl => Environment.GetEnvironmentVariable("E2E_CHROME_NOVNC_CDP_URL");

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        Browser = !string.IsNullOrWhiteSpace(ChromeNovncCdpUrl)
            ? await Playwright.Chromium.ConnectOverCDPAsync(ChromeNovncCdpUrl)
            : await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = GetBooleanEnvironmentVariable("E2E_HEADLESS", true),
                Args = new[] { "--ignore-certificate-errors" }
            });
    }

    public async Task RunWithPageAsync(string testName, Func<IPage, Task> testBody)
    {
        var context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            IgnoreHTTPSErrors = true,
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 }
        });

        var page = await context.NewPageAsync();
        try
        {
            await testBody(page);
            await CaptureScreenshotAsync(page, testName, "success");
        }
        catch
        {
            await CaptureScreenshotAsync(page, testName, "failure");
            throw;
        }
        finally
        {
            await context.CloseAsync();
        }
    }

    public async Task CaptureScreenshotAsync(IPage page, string testName, string state)
    {
        Directory.CreateDirectory(ScreenshotRootDirectory);
        var safeTestName = InvalidFileNameCharsRegex.Replace(testName, "_");
        var filePath = Path.Combine(ScreenshotRootDirectory, $"{safeTestName}-{state}-{DateTime.UtcNow:yyyyMMddHHmmssfff}.png");
        await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = filePath,
            FullPage = true
        });
    }

    private static bool GetBooleanEnvironmentVariable(string name, bool defaultValue)
    {
        var value = Environment.GetEnvironmentVariable(name);
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        Playwright.Dispose();
    }
}
