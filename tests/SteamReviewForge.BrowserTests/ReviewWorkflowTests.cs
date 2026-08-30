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
        await Assertions.Expect(page.Locator("#review-summary"))
            .ToHaveCountAsync(0);
        await page.GetByLabel("Short review summary")
            .FillAsync("Firefox primary workflow review.");
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

        await Assertions.Expect(
                page.Locator(".final-preview-panel").GetByRole(
                    AriaRole.Heading,
                    new LocatorGetByRoleOptions { Name = "Final Preview" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Final Preview" }))
            .ToHaveCountAsync(0);
        await page.Locator(".final-preview-panel").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode" })
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

        await Assertions.Expect(page.GetByLabel("Short review summary"))
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
                new PageGetByRoleOptions { Name = "BBCode", Exact = true })
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
                new PageGetByRoleOptions { Name = "BBCode", Exact = true })
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

    [Fact]
    public async Task Chromium_BbCodeWorkspaceStacksToolsBesideEditorAndPreview()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;
        await page.SetViewportSizeAsync(1600, 900);

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "BBCode", Exact = true })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Start fresh" })
            .ClickAsync();
        await Assertions.Expect(page.Locator(".bbcode-panel"))
            .ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".preview-panel").GetByRole(
                AriaRole.Heading,
                new LocatorGetByRoleOptions { Name = "Final Preview" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".bbcode-panel").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode" }))
            .ToBeDisabledAsync();

        var setupLayout = await page.EvaluateAsync<double[]>("""
            () => {
                const workflow = document.querySelector('.workflow-column')
                    .getBoundingClientRect();
                const cards = [...document.querySelectorAll(
                    '.bbcode-start-grid .bbcode-start-card')]
                    .map(card => card.getBoundingClientRect());
                return [
                    workflow.width,
                    cards[0].width,
                    cards[1].width,
                    cards[0].height,
                    cards[1].height,
                    cards[0].top,
                    cards[1].top
                ];
            }
            """);

        Assert.InRange(setupLayout[0], 360, 420);
        Assert.True(setupLayout[1] > setupLayout[2] * 3.5);
        Assert.True(setupLayout[3] > setupLayout[4]);
        Assert.True(setupLayout[6] > setupLayout[5]);

        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { Name = "Recommended", Exact = true })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Open composer" })
            .ClickAsync();

        await Assertions.Expect(page.Locator(".bbcode-block-grid button"))
            .ToHaveCountAsync(21);
        Assert.Equal(0, await page.EvaluateAsync<double>("window.scrollY"));

        var layout = await page.EvaluateAsync<double[]>("""
            () => {
                const workflow = document.querySelector('.workflow-column');
                const navigation = document.querySelector(
                    '.workflow-navigation-panel');
                const options = document.querySelector(
                    '.workflow-options-panel');
                const editor = document.querySelector('.bbcode-panel');
                const preview = document.querySelector('.preview-panel');
                const grid = document.querySelector('.bbcode-block-grid');
                const cards = [...grid.querySelectorAll('button')];
                const first = cards[0].getBoundingClientRect();
                const third = cards[2].getBoundingClientRect();
                const fourth = cards[3].getBoundingClientRect();
                const workflowBox = workflow.getBoundingClientRect();
                const navigationBox = navigation.getBoundingClientRect();
                const optionsBox = options.getBoundingClientRect();
                const editorBox = editor.getBoundingClientRect();
                const previewBox = preview.getBoundingClientRect();

                return [
                    workflowBox.width,
                    navigationBox.bottom,
                    optionsBox.top,
                    editorBox.left,
                    workflowBox.right,
                    previewBox.left,
                    editorBox.right,
                    previewBox.width,
                    editorBox.width,
                    first.top,
                    third.top,
                    fourth.top
                ];
            }
            """);

        Assert.InRange(layout[0], 290, 340);
        Assert.True(layout[2] > layout[1]);
        Assert.True(layout[3] > layout[4]);
        Assert.True(layout[5] > layout[6]);
        Assert.InRange(layout[7], 590, 620);
        Assert.InRange(layout[8], 500, 620);
        Assert.Equal(layout[9], layout[10], precision: 1);
        Assert.True(layout[11] > layout[9]);

        await page.Locator(".bbcode-block-grid button").First.ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Next", Exact = true })
            .ClickAsync();
        await Assertions.Expect(page.Locator(".preview-panel").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode" }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".bbcode-panel").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode" }))
            .ToBeEnabledAsync();
    }

    [Fact]
    public async Task Chromium_DeepDiveUsesRemovableSectionOnlyContent()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();
        Assert.True(await page.Locator(".workflow-options-panel .step-content")
            .EvaluateAsync<bool>("element => element.scrollWidth <= element.clientWidth"));
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Deep Dive") })
            .ClickAsync();

        await Assertions.Expect(page.Locator(".steam-editable-section"))
            .ToHaveCountAsync(6);
        await Assertions.Expect(page.GetByText("What Works", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("What Could Be Better", new() { Exact = true }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("Final Thoughts", new() { Exact = true }))
            .ToHaveCountAsync(0);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Remove review title" })
            .ClickAsync();
        await Assertions.Expect(page.GetByLabel("Review title"))
            .ToHaveCountAsync(0);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Remove category Gameplay" })
            .ClickAsync();
        await Assertions.Expect(page.Locator(".steam-editable-section"))
            .ToHaveCountAsync(5);
    }

    [Fact]
    public async Task Chromium_RemovesAllBuiltInBalancedContent()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();

        foreach (var blockName in new[]
                 {
                     "Remove review title",
                     "Remove short summary",
                     "Remove rating table",
                     "Remove What Works",
                     "Remove What Could Be Better",
                     "Remove final thoughts"
                 })
        {
            await page.GetByRole(
                    AriaRole.Button,
                    new PageGetByRoleOptions { Name = blockName, Exact = true })
                .ClickAsync();
        }

        await Assertions.Expect(page.GetByLabel("Review title"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByLabel("Short review summary"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByText("Rating table removed.", new()
            {
                Exact = true
            }))
            .ToBeVisibleAsync();

        await Assertions.Expect(page.Locator(".final-preview-panel").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode", Exact = true }))
            .ToBeDisabledAsync();
    }

    [Fact]
    public async Task Chromium_FinalPreviewKeepsSteamSizingAcrossEditingModes()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;
        await page.SetViewportSizeAsync(1600, 1000);

        await OpenApplicationAsync(page);
        await AssertSteamSizedFinalPreviewAsync(page);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Unguided", Exact = true })
            .ClickAsync();
        await Assertions.Expect(page.Locator(".editor-mode-summary strong"))
            .ToHaveTextAsync("Unguided Structured");
        await AssertSteamSizedFinalPreviewAsync(page);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "BBCode", Exact = true })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Start fresh" })
            .ClickAsync();
        await Assertions.Expect(page.Locator(".editor-mode-summary strong"))
            .ToHaveTextAsync("Unguided BBCode");
        await AssertSteamSizedFinalPreviewAsync(page);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Guided", Exact = true })
            .ClickAsync();
        await Assertions.Expect(page.Locator(".editor-mode-summary strong"))
            .ToHaveTextAsync("Guided BBCode");
        await AssertSteamSizedFinalPreviewAsync(page);
    }

    [Fact]
    public async Task Chromium_EditsTemplateHeadingsTableAndDividersInPreview()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();
        await page.GetByLabel("Early Access Review").CheckAsync();

        var editablePreview = page.Locator(".preview-panel");
        var finalPreview = page.Locator(".final-preview-panel");

        await AssertSteamSizedFinalPreviewAsync(page);

        await editablePreview.GetByLabel("What Works heading")
            .FillAsync("Highlights");
        await editablePreview.GetByLabel("What Could Be Better heading")
            .FillAsync("Rough Edges");
        await editablePreview.GetByLabel("Final Thoughts heading")
            .FillAsync("Verdict");
        await editablePreview.GetByLabel("Category data column heading")
            .FillAsync("Aspect");
        await editablePreview.GetByLabel("Category name").First
            .FillAsync("Combat");
        await editablePreview.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Remove category Story" })
            .ClickAsync();

        await editablePreview.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions
                {
                    Name = "Remove divider before review format"
                })
            .ClickAsync();
        await editablePreview.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions
                {
                    Name = "Remove divider before guided writing"
                })
            .ClickAsync();

        await Assertions.Expect(finalPreview.GetByRole(
                AriaRole.Heading,
                new LocatorGetByRoleOptions { Name = "Highlights" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(finalPreview.GetByText(
                "Early Access Review",
                new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(finalPreview.GetByRole(
                AriaRole.Heading,
                new LocatorGetByRoleOptions { Name = "Rough Edges" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(finalPreview.GetByRole(
                AriaRole.Heading,
                new LocatorGetByRoleOptions { Name = "Verdict" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(finalPreview.GetByRole(
                AriaRole.Columnheader,
                new LocatorGetByRoleOptions { Name = "Aspect" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(finalPreview.GetByText("Combat", new()
            {
                Exact = true
            }))
            .ToBeVisibleAsync();
        await Assertions.Expect(finalPreview.GetByText("Story", new()
            {
                Exact = true
            }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(finalPreview.Locator("hr"))
            .ToHaveCountAsync(0);
    }

    private static async Task OpenApplicationAsync(IPage page)
    {
        await page.GotoAsync(BaseUrl, new PageGotoOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await WaitForApplicationAsync(page);
    }

    private static async Task AssertSteamSizedFinalPreviewAsync(IPage page)
    {
        var finalPreview = page.Locator(".steam-sized-preview-panel");
        await Assertions.Expect(finalPreview).ToBeVisibleAsync();
        await Assertions.Expect(finalPreview).ToHaveCountAsync(1);

        var finalPreviewWidth = await finalPreview.EvaluateAsync<double>(
            "element => element.getBoundingClientRect().width");
        Assert.InRange(finalPreviewWidth, 590, 620);

        var finalTypography = await finalPreview
            .Locator(".steam-review-content")
            .EvaluateAsync<string[]>("""
                element => {
                    const style = getComputedStyle(element);
                    return [style.fontSize, style.lineHeight];
                }
                """);
        Assert.Equal(["13px", "18px"], finalTypography);
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
