using Microsoft.Playwright;
using Xunit;

namespace SteamReviewForge.BrowserTests;

public sealed class ReviewWorkflowTests
{
    private static readonly string BaseUrl =
        Environment.GetEnvironmentVariable("STEAM_REVIEW_FORGE_BASE_URL") ??
        "http://127.0.0.1:5080";

    [Fact]
    public async Task Firefox_CompletesPrimaryWorkflowAndRestoresDraft()
    {
        await using var session = await BrowserSession.CreateAsync("firefox");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();
        await page.Locator("#review-summary").FillAsync("Firefox primary workflow review.");
        await page.Locator("#playtime").FillAsync("42.5");
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to format" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to questions" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Open final preview" })
            .ClickAsync();

        await Assertions.Expect(
                page.GetByRole(
                    AriaRole.Heading,
                    new PageGetByRoleOptions { Name = "Final Steam Review" }))
            .ToBeVisibleAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Copy BBCode" })
            .ClickAsync();
        await Assertions.Expect(page.GetByText("Copied to clipboard."))
            .ToBeVisibleAsync();

        var copiedText = await page.EvaluateAsync<string>("window.__copiedText");
        Assert.Contains("[h1]", copiedText);
        Assert.Contains("Firefox primary workflow review.", copiedText);

        await Assertions.Expect(page.GetByText("Saved", new() { Exact = true }))
            .ToBeVisibleAsync();
        await page.ReloadAsync(new PageReloadOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await WaitForApplicationAsync(page);

        await Assertions.Expect(page.Locator("#review-summary"))
            .ToHaveValueAsync("Firefox primary workflow review.");
        await Assertions.Expect(page.GetByText("Draft restored", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Firefox_RecoversCorruptDraftWithoutOverwritingIt()
    {
        await using var session = await BrowserSession.CreateAsync(
            "firefox",
            "localStorage.setItem('steam-review-forge-draft-v2', '{ broken-json');");
        var page = session.Page;

        await OpenApplicationAsync(page);

        await Assertions.Expect(
                page.GetByRole(
                    AriaRole.Dialog,
                    new PageGetByRoleOptions { Name = "Saved draft needs recovery" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(
                page.GetByLabel("Saved draft backup data"))
            .ToHaveValueAsync("{ broken-json");

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Copy saved data" })
            .ClickAsync();
        Assert.Equal(
            "{ broken-json",
            await page.EvaluateAsync<string>("window.__copiedText"));

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Reset saved draft" })
            .ClickAsync();
        await Assertions.Expect(page.GetByRole(AriaRole.Dialog))
            .ToHaveCountAsync(0);

        var stored = await page.EvaluateAsync<string?>(
            "localStorage.getItem('steam-review-forge-draft-v2')");
        Assert.Null(stored);
    }

    [Fact]
    public async Task Firefox_TracksUnsavedChangesWhenStorageFails()
    {
        const string storageFailureScript = """
            const originalSetItem = Storage.prototype.setItem;
            Storage.prototype.setItem = function (key, value) {
                if (key.startsWith('steam-review-forge-draft')) {
                    throw new DOMException('Storage disabled', 'QuotaExceededError');
                }
                return originalSetItem.call(this, key, value);
            };
            """;
        await using var session = await BrowserSession.CreateAsync(
            "firefox",
            storageFailureScript);
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();

        await Assertions.Expect(
                page.GetByText("Draft storage unavailable", new() { Exact = true }))
            .ToBeVisibleAsync();
        Assert.True(await page.EvaluateAsync<bool>(
            "window.reviewDraftLifecycle.hasUnsavedChanges()"));
    }

    [Fact]
    public async Task Firefox_ShowsWarningsWithoutBlockingRawBbCodeCopy()
    {
        await using var session = await BrowserSession.CreateAsync("firefox");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "BBCode" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Start fresh" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Unguided" })
            .ClickAsync();

        const string malformed = "[b][i]Malformed[/b]";
        await page.Locator("#raw-bbcode-editor").FillAsync(malformed);
        await Assertions.Expect(page.GetByText("BBCode compatibility warnings", new()
            {
                Exact = false
            }).First).ToBeVisibleAsync();

        await page.Locator(".bbcode-panel")
            .GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode" })
            .ClickAsync();
        Assert.Equal(
            malformed,
            await page.EvaluateAsync<string>("window.__copiedText"));
        await Assertions.Expect(page.GetByText("Copied with", new() { Exact = false }))
            .ToBeVisibleAsync();
    }

    [Fact]
    public async Task Chromium_LoadsEditsPreviewsPersistsAndCopies()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "BBCode" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Start fresh" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Unguided" })
            .ClickAsync();

        await page.Locator("#raw-bbcode-editor").FillAsync("[h1]Chromium smoke[/h1]");
        await Assertions.Expect(
                page.Locator(".steam-review-content h1"))
            .ToHaveTextAsync("Chromium smoke");
        await page.Locator(".bbcode-panel")
            .GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode" })
            .ClickAsync();

        Assert.Equal(
            "[h1]Chromium smoke[/h1]",
            await page.EvaluateAsync<string>("window.__copiedText"));
        await Assertions.Expect(page.GetByText("Saved", new() { Exact = true }))
            .ToBeVisibleAsync();
    }

    private static async Task OpenApplicationAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await WaitForApplicationAsync(page);
    }

    private static async Task WaitForApplicationAsync(IPage page)
    {
        await Assertions.Expect(
                page.GetByRole(
                    AriaRole.Heading,
                    new PageGetByRoleOptions { Name = "Steam Review Forge" }))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions
            {
                Timeout = 30_000
            });
    }

    private sealed class BrowserSession : IAsyncDisposable
    {
        private BrowserSession(
            IPlaywright playwright,
            IBrowser browser,
            IBrowserContext context,
            IPage page)
        {
            Playwright = playwright;
            Browser = browser;
            Context = context;
            Page = page;
        }

        private IPlaywright Playwright { get; }

        private IBrowser Browser { get; }

        private IBrowserContext Context { get; }

        public IPage Page { get; }

        public static async Task<BrowserSession> CreateAsync(
            string browserName,
            string? additionalInitScript = null)
        {
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browserType = browserName switch
            {
                "firefox" => playwright.Firefox,
                "chromium" => playwright.Chromium,
                _ => throw new ArgumentOutOfRangeException(nameof(browserName))
            };
            var browser = await browserType.LaunchAsync(
                new BrowserTypeLaunchOptions { Headless = true });
            var context = await browser.NewContextAsync(new BrowserNewContextOptions
            {
                ViewportSize = new ViewportSize
                {
                    Width = 1440,
                    Height = 1000
                }
            });

            await context.AddInitScriptAsync("""
                Object.defineProperty(navigator, 'clipboard', {
                    configurable: true,
                    value: {
                        writeText: async function (text) {
                            window.__copiedText = text;
                        }
                    }
                });
                """);

            if (!string.IsNullOrWhiteSpace(additionalInitScript))
            {
                await context.AddInitScriptAsync(additionalInitScript);
            }

            var page = await context.NewPageAsync();

            return new BrowserSession(playwright, browser, context, page);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Browser.DisposeAsync();
            Playwright.Dispose();
        }
    }
}
