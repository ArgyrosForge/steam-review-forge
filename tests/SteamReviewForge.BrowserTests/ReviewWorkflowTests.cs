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
        await Assertions.Expect(page.GetByText(
                "Editable preview locked",
                new PageGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions
                {
                    Name = "Short review summary",
                    Exact = true
                }))
            .ToHaveCountAsync(0);
        await page.Locator("#playtime").FillAsync("42.5");
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();
        await Assertions.Expect(page.Locator(
                ".preview-panel .steam-preview.setup-locked-preview"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator(".rating-system-choice-grid"))
            .ToHaveCountAsync(0);
        await page.Locator(
                ".preview-panel input[aria-label='Review title']")
            .FillAsync("Firefox primary workflow review");
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
        Assert.Contains("Firefox primary workflow review", copiedText);
        Assert.DoesNotContain("[i]", copiedText);

        await Assertions.Expect(page.GetByText("Saved", new() { Exact = true }))
            .ToBeVisibleAsync();
        await page.ReloadAsync(new PageReloadOptions
        {
            WaitUntil = WaitUntilState.DOMContentLoaded
        });
        await WaitForApplicationAsync(page);

        await Assertions.Expect(page.GetByText(
                "Editable preview locked",
                new PageGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();
        await Assertions.Expect(page.Locator(
                ".preview-panel input[aria-label='Review title']"))
            .ToHaveValueAsync("Firefox primary workflow review");
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
                new LocatorGetByRoleOptions { Name = "View BBCode" })
            .ClickAsync();

        var bbCodeViewer = page.GetByRole(
            AriaRole.Dialog,
            new PageGetByRoleOptions { Name = "View BBCode" });
        await Assertions.Expect(bbCodeViewer).ToBeVisibleAsync();
        await Assertions.Expect(
                bbCodeViewer.GetByLabel("Generated Steam BBCode"))
            .ToHaveValueAsync("[h1]Chromium smoke[/h1]");
        await bbCodeViewer.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Copy BBCode" })
            .ClickAsync();

        Assert.Equal(
            "[h1]Chromium smoke[/h1]",
            await page.EvaluateAsync<string>("window.__copiedText"));
        await Assertions.Expect(
                bbCodeViewer.GetByText("Copied to clipboard."))
            .ToBeVisibleAsync();
        await bbCodeViewer.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Close", Exact = true })
            .ClickAsync();
        await Assertions.Expect(bbCodeViewer).ToHaveCountAsync(0);
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
        var openComposer = page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Open composer" });
        await Assertions.Expect(openComposer).ToBeDisabledAsync();
        await Assertions.Expect(page.GetByText(
                "Select Blank document or a template to unlock the editor.",
                new PageGetByTextOptions { Exact = true }))
            .ToHaveCountAsync(2);
        await Assertions.Expect(page.Locator("#raw-bbcode-editor"))
            .ToHaveAttributeAsync(
                "aria-describedby",
                "bbcode-editor-lock-message");
        await Assertions.Expect(page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { Name = "Blank document" }))
            .ToHaveAttributeAsync("aria-checked", "false");

        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { Name = "Blank document" })
            .ClickAsync();
        await Assertions.Expect(openComposer).ToBeEnabledAsync();
        await Assertions.Expect(page.GetByText(
                "Ready to open composer",
                new PageGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await openComposer.ClickAsync();
        await Assertions.Expect(page.Locator("#raw-bbcode-editor"))
            .ToBeEditableAsync();

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

        var inlineEditorLayout = await page.EvaluateAsync<double[]>("""
            () => {
                const preview = document.querySelector(
                    '.preview-panel .steam-preview').getBoundingClientRect();
                const review = document.querySelector(
                    '.preview-panel .steam-review-page').getBoundingClientRect();
                const section = document.querySelector(
                    '.preview-panel .steam-editable-section').getBoundingClientRect();
                const heading = document.querySelector(
                    '.preview-panel .steam-editable-section-header input')
                    .getBoundingClientRect();
                const controls = document.querySelector(
                    '.preview-panel .steam-editable-section-header ' +
                    '.steam-category-controls').getBoundingClientRect();

                return [
                    preview.width,
                    review.width,
                    section.right,
                    controls.right,
                    heading.top,
                    heading.bottom,
                    controls.top,
                    controls.bottom
                ];
            }
            """);

        Assert.InRange(
            inlineEditorLayout[0] - inlineEditorLayout[1],
            0,
            12);
        Assert.True(inlineEditorLayout[3] <= inlineEditorLayout[2]);
        Assert.True(inlineEditorLayout[6] < inlineEditorLayout[5]);
        Assert.True(inlineEditorLayout[7] > inlineEditorLayout[4]);

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
    public async Task Chromium_FullCustomKeepsEditorFinalPreviewAndBbCodeInSync()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Full Custom") })
            .ClickAsync();

        var editablePreview = page.Locator(".preview-panel");
        var finalPreview = page.Locator(".final-preview-panel");

        await Assertions.Expect(
                editablePreview.Locator("input[aria-label='Review title']"))
            .ToHaveValueAsync("[Game Title] Review");
        await Assertions.Expect(
                editablePreview.GetByLabel("Short review summary"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(editablePreview.GetByLabel("Category name"))
            .ToHaveValueAsync("New Category");
        await Assertions.Expect(editablePreview.GetByLabel("What Works heading"))
            .ToHaveValueAsync("What Works");
        await Assertions.Expect(
                editablePreview.GetByLabel("What Could Be Better heading"))
            .ToHaveValueAsync("What Could Be Better");
        await Assertions.Expect(
                editablePreview.GetByLabel("Final Thoughts heading"))
            .ToHaveValueAsync("Final Thoughts");

        await Assertions.Expect(finalPreview.GetByRole(
                AriaRole.Heading,
                new LocatorGetByRoleOptions { Name = "[Game Title] Review" }))
            .ToBeVisibleAsync();
        await Assertions.Expect(finalPreview.GetByText(
                "New Category",
                new LocatorGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        foreach (var heading in new[]
                 {
                     "What Works",
                     "What Could Be Better",
                     "Final Thoughts"
                 })
        {
            await Assertions.Expect(finalPreview.GetByRole(
                    AriaRole.Heading,
                    new LocatorGetByRoleOptions { Name = heading }))
                .ToBeVisibleAsync();
        }

        await Assertions.Expect(
                finalPreview.Locator(".preview-summary"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(editablePreview.Locator("hr"))
            .ToHaveCountAsync(2);
        await Assertions.Expect(finalPreview.Locator("hr"))
            .ToHaveCountAsync(2);

        await finalPreview.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "View BBCode" })
            .ClickAsync();
        var bbCode = await page.GetByLabel("Generated Steam BBCode")
            .InputValueAsync();

        Assert.Contains("[h1][Game Title] Review[/h1]", bbCode);
        Assert.DoesNotContain("[i]", bbCode);
        Assert.Contains("[b]New Category[/b]", bbCode);
        Assert.Contains("[h2]What Works[/h2]", bbCode);
        Assert.Contains("[h2]What Could Be Better[/h2]", bbCode);
        Assert.Contains("[h2]Final Thoughts[/h2]", bbCode);
        Assert.Equal(2, CountOccurrences(bbCode, "[hr][/hr]"));
    }

    [Fact]
    public async Task Chromium_FormatsAndReordersStructuredBodyContent()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();

        var plainTextButton = page.GetByRole(
            AriaRole.Button,
            new PageGetByRoleOptions
            {
                Name = "Format What Works as plain text",
                Exact = true
            });
        await Assertions.Expect(plainTextButton)
            .ToHaveAttributeAsync("aria-pressed", "true");
        await Assertions.Expect(page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions
                {
                    Name = "What Works content",
                    Exact = true
                }))
            .ToHaveValueAsync(
                "Satisfying core gameplay\n" +
                "Lots of content to discover\n" +
                "Strong visual identity");

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions
                {
                    Name = "Format What Works as a bulleted list",
                    Exact = true
                })
            .ClickAsync();
        await Assertions.Expect(page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions
                {
                    Name = "What Works content",
                    Exact = true
                }))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.GetByLabel(
                new System.Text.RegularExpressions.Regex(
                    "^What Works content item")))
            .ToHaveCountAsync(3);

        await page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions
                {
                    Name = "What Works content item 1",
                    Exact = true
                })
            .FillAsync("Alpha");
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions
                {
                    Name = "Move What Works content item 1 down",
                    Exact = true
                })
            .ClickAsync();

        var thirdItemHandle = page.GetByLabel(
            "Drag What Works content item 3 to reorder");
        var firstItemRow = page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions
                {
                    Name = "What Works content item 1",
                    Exact = true
                })
            .Locator("xpath=ancestor::*[contains(@class, 'steam-list-item-row')]");
        await thirdItemHandle.DragToAsync(firstItemRow);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions
                {
                    Name = "Format What Works as a numbered list",
                    Exact = true
                })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "+ Add item", Exact = true })
            .First
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions
                {
                    Name = "What Works content item 4",
                    Exact = true
                })
            .FillAsync("Fourth");
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions
                {
                    Name = "Remove What Works content item 2",
                    Exact = true
                })
            .ClickAsync();

        var finalList = page.Locator(
            ".final-preview-panel ol.preview-bbcode-list");
        await Assertions.Expect(finalList).ToHaveCountAsync(1);
        await Assertions.Expect(finalList.Locator("li"))
            .ToHaveTextAsync(["Strong visual identity", "Alpha", "Fourth"]);

        await page.Locator(".final-preview-panel").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "View BBCode" })
            .ClickAsync();
        var bbCode = await page.GetByLabel("Generated Steam BBCode")
            .InputValueAsync();
        Assert.Contains(
            "[olist]\n[*]Strong visual identity\n[*]Alpha\n[*]Fourth\n[/olist]",
            bbCode);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Close BBCode viewer" })
            .ClickAsync();

        await page.SetViewportSizeAsync(480, 1000);
        var narrowLayout = await page.EvaluateAsync<double[]>("""
            () => {
                const review = document.querySelector(
                    '.preview-panel .steam-review-page').getBoundingClientRect();
                const heading = document.querySelector(
                    'input[aria-label="What Works heading"]');
                const block = heading.closest('.steam-writing-block');
                const formats = block.querySelector(
                    '.steam-text-format-controls').getBoundingClientRect();
                const row = block.querySelector(
                    '.steam-list-item-row').getBoundingClientRect();
                const rowControls = block.querySelector(
                    '.steam-list-item-controls').getBoundingClientRect();
                const itemInput = block.querySelector(
                    '.steam-list-item-input').getBoundingClientRect();
                return [
                    review.left,
                    review.right,
                    formats.left,
                    formats.right,
                    row.left,
                    row.right,
                    rowControls.right,
                    itemInput.width
                ];
            }
            """);
        Assert.InRange(narrowLayout[2], narrowLayout[0], narrowLayout[1]);
        Assert.InRange(narrowLayout[3], narrowLayout[0], narrowLayout[1]);
        Assert.InRange(narrowLayout[4], narrowLayout[0], narrowLayout[1]);
        Assert.InRange(narrowLayout[5], narrowLayout[0], narrowLayout[1]);
        Assert.InRange(narrowLayout[6], narrowLayout[0], narrowLayout[1]);
        Assert.True(narrowLayout[7] > 80);

        await plainTextButton.ClickAsync();
        await Assertions.Expect(page.GetByRole(
                AriaRole.Textbox,
                new PageGetByRoleOptions
                {
                    Name = "What Works content",
                    Exact = true
                }))
            .ToHaveValueAsync("Strong visual identity\nAlpha\nFourth");
        await Assertions.Expect(page.Locator(
                ".final-preview-panel ol.preview-bbcode-list"))
            .ToHaveCountAsync(0);
    }

    [Fact]
    public async Task Chromium_KeepsChecklistAndCustomComponentControlsInline()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;

        await OpenApplicationAsync(page);
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Unguided", Exact = true })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Open editor", Exact = true })
            .ClickAsync();
        await page.Locator("#unguided-format")
            .SelectOptionAsync(new SelectOptionValue { Label = "Checklist" });

        var checklistLayout = await page.EvaluateAsync<double[]>("""
            () => {
                const review = document.querySelector(
                    '.preview-panel .steam-review-page').getBoundingClientRect();
                const itemElement = document.querySelector(
                    '.preview-panel .steam-editable-check-item');
                const item = itemElement.getBoundingClientRect();
                const heading = itemElement.querySelector(
                    '.steam-check-edit-heading input').getBoundingClientRect();
                const controls = itemElement.querySelector(
                    '.steam-category-controls').getBoundingClientRect();
                const add = document.querySelector(
                    '.preview-panel .steam-add-category-row button')
                    .getBoundingClientRect();
                return [
                    review.left,
                    review.right,
                    item.left,
                    item.right,
                    controls.right,
                    heading.top,
                    heading.bottom,
                    controls.top,
                    controls.bottom,
                    add.left,
                    add.right
                ];
            }
            """);

        Assert.True(checklistLayout[2] >= checklistLayout[0]);
        Assert.True(checklistLayout[3] <= checklistLayout[1]);
        Assert.True(checklistLayout[4] <= checklistLayout[3]);
        Assert.True(checklistLayout[7] < checklistLayout[6]);
        Assert.True(checklistLayout[8] > checklistLayout[5]);
        Assert.InRange(checklistLayout[9], checklistLayout[0], checklistLayout[1]);
        Assert.InRange(checklistLayout[10], checklistLayout[0], checklistLayout[1]);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { NameRegex = new("^Rating category") })
            .ClickAsync();

        var componentLayout = await page.Locator(".structured-review-component")
            .Last
            .EvaluateAsync<double[]>("""
                component => {
                    const componentBox = component.getBoundingClientRect();
                    const heading = component.querySelector(
                        '.structured-review-component-header input')
                        .getBoundingClientRect();
                    const controls = component.querySelector(
                        '.structured-review-component-controls')
                        .getBoundingClientRect();
                    return [
                        componentBox.right,
                        controls.right,
                        heading.top,
                        heading.bottom,
                        controls.top,
                        controls.bottom
                    ];
                }
                """);

        Assert.True(componentLayout[1] <= componentLayout[0]);
        Assert.True(componentLayout[4] < componentLayout[3]);
        Assert.True(componentLayout[5] > componentLayout[2]);

        var ratingComponent = page.Locator(".structured-review-component").Last;
        var ratingTextFormat = ratingComponent.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions
            {
                Name = "Format New Rating as plain text",
                Exact = true
            });
        await Assertions.Expect(ratingTextFormat)
            .ToHaveAttributeAsync("aria-pressed", "true");
        await ratingComponent.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions
                {
                    Name = "Format New Rating as a bulleted list",
                    Exact = true
                })
            .ClickAsync();
        await Assertions.Expect(ratingComponent.GetByRole(
                AriaRole.Textbox,
                new LocatorGetByRoleOptions
                {
                    Name = "New Rating content item 1",
                    Exact = true
                }))
            .ToHaveValueAsync("Add your notes");
        await Assertions.Expect(page.Locator(
                ".final-preview-panel ul.preview-bbcode-list"))
            .ToContainTextAsync("Add your notes");
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
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();

        foreach (var blockName in new[]
                 {
                     "Remove review title",
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
    public async Task Chromium_InitialWorkspaceFitsViewportAndPreviewsFollowContent()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;
        await page.SetViewportSizeAsync(2048, 1000);

        await OpenApplicationAsync(page);

        var layout = await page.EvaluateAsync<double[]>("""
            () => {
                const editablePanel = document.querySelector('.preview-panel')
                    .getBoundingClientRect();
                const editablePreview = document.querySelector(
                    '.preview-panel .steam-preview').getBoundingClientRect();
                const editableReview = document.querySelector(
                    '.preview-panel .steam-review-page').getBoundingClientRect();
                const finalPanel = document.querySelector('.final-preview-panel')
                    .getBoundingClientRect();
                const finalPreview = document.querySelector(
                    '.final-preview-panel .steam-preview').getBoundingClientRect();
                const finalReview = document.querySelector(
                    '.final-preview-panel .steam-review-page').getBoundingClientRect();

            const header = document.querySelector('.app-header')
                .getBoundingClientRect();
            const workspace = document.querySelector('.builder-layout')
                .getBoundingClientRect();
            const workflow = document.querySelector('.workflow-column')
                .getBoundingClientRect();
            const footer = document.querySelector('.app-footer')
                .getBoundingClientRect();

            return [
                document.documentElement.scrollHeight,
                window.innerHeight,
                editablePreview.bottom - editableReview.bottom,
                editablePanel.bottom - editablePreview.bottom,
                finalPreview.bottom - finalReview.bottom,
                finalPanel.bottom - finalPreview.bottom,
                header.bottom,
                workspace.top,
                workspace.bottom,
                workflow.bottom,
                footer.top,
                footer.bottom
            ];
        }
        """);

        Assert.True(
            layout[0] <= layout[1] + 1,
            $"Document {layout[0]}px; viewport {layout[1]}px; " +
            $"header bottom {layout[6]}px; workspace {layout[7]}-{layout[8]}px; " +
            $"workflow bottom {layout[9]}px; footer {layout[10]}-{layout[11]}px.");
        Assert.InRange(layout[2], 0, 2);
        Assert.InRange(layout[3], 0, 2);
        Assert.InRange(layout[4], 0, 2);
        Assert.InRange(layout[5], 0, 2);
    }

    [Fact]
    public async Task Chromium_HeaderPathAndBlockersStayConsistentAcrossModes()
    {
        await using var session = await BrowserSession.CreateAsync("chromium");
        var page = session.Page;
        await page.SetViewportSizeAsync(2048, 1000);

        await OpenApplicationAsync(page);
        var primaryPreview = page.Locator(
            ".preview-panel .steam-preview");
        var ratingSystemGrid = page.Locator(".rating-system-choice-grid");
        await Assertions.Expect(ratingSystemGrid.GetByRole(AriaRole.Radio))
            .ToHaveCountAsync(4);
        await Assertions.Expect(ratingSystemGrid.GetByLabel(
                "Star rating. Example: four out of five stars"))
            .ToContainTextAsync("★★★★☆");
        Assert.True(await ratingSystemGrid.EvaluateAsync<bool>(
            "element => element.scrollWidth <= element.clientWidth"));
        await Assertions.Expect(primaryPreview)
            .ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-preview"));
        Assert.NotEqual(
            "none",
            await primaryPreview.Locator(".steam-review-page")
                .EvaluateAsync<string>(
                    "element => getComputedStyle(element).filter"));
        await AssertHeaderPathAsync(
            page,
            "Setup",
            "Template",
            "Format",
            "Questions");
        await Assertions.Expect(page.Locator(
                ".workflow-navigation-panel .step-list"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#header-progress-status"))
            .ToContainTextAsync(
                "Choose a recommendation to unlock Template, Format, and Questions.");
        await Assertions.Expect(page.Locator(".header-progress").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { NameRegex = new("Template$") }))
            .ToBeDisabledAsync();

        var desktopLayout = await page.EvaluateAsync<double[]>("""
            () => {
                const brand = document.querySelector('.brand-lockup')
                    .getBoundingClientRect();
                const path = document.querySelector('.header-progress')
                    .getBoundingClientRect();
                const actions = document.querySelector('.header-actions')
                    .getBoundingClientRect();
                return [brand.right, path.left, path.right, actions.left];
            }
            """);
        Assert.True(desktopLayout[1] >= desktopLayout[0]);
        Assert.True(desktopLayout[2] <= desktopLayout[3]);

        var mainBlueAccent = await page.Locator(
                ".header-progress-item.active .header-progress-node")
            .EvaluateAsync<string>(
                "element => getComputedStyle(element).backgroundColor");
        await page.GetByLabel("Color theme").SelectOptionAsync("nord");
        await page.WaitForFunctionAsync(
            "previous => getComputedStyle(document.querySelector('.header-progress-item.active .header-progress-node')).backgroundColor !== previous",
            mainBlueAccent);
        var nordAccent = await page.Locator(
                ".header-progress-item.active .header-progress-node")
            .EvaluateAsync<string>(
                "element => getComputedStyle(element).backgroundColor");
        Assert.NotEqual(mainBlueAccent, nordAccent);

        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { NameRegex = new("Yes.*Recommended") })
            .ClickAsync();
        await Assertions.Expect(page.Locator("#header-progress-status"))
            .ToContainTextAsync(
                "Setup is complete. Select Continue to template to unlock the editor.");
        await Assertions.Expect(page.GetByText(
                "Editable preview locked",
                new PageGetByTextOptions { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions.Expect(page.Locator(".header-progress").GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { NameRegex = new("Template$") }))
            .ToBeDisabledAsync();
        await Assertions.Expect(primaryPreview)
            .ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-preview"));
        var guidedSetupWidth = await page.Locator(".workflow-column")
            .EvaluateAsync<double>(
                "element => element.getBoundingClientRect().width");

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Unguided", Exact = true })
            .ClickAsync();
        await Assertions.Expect(page.Locator(".builder-layout"))
            .ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "wide-workflow-layout"));
        var unguidedSetupWidth = await page.Locator(".workflow-column")
            .EvaluateAsync<double>(
                "element => element.getBoundingClientRect().width");
        Assert.InRange(
            Math.Abs(unguidedSetupWidth - guidedSetupWidth),
            0,
            1);
        await AssertHeaderPathAsync(
            page,
            "Setup",
            "Edit Review",
            "Final Preview");
        await Assertions.Expect(ratingSystemGrid).ToBeVisibleAsync();
        await Assertions.Expect(page.GetByLabel("Short review summary"))
            .ToHaveCountAsync(0);
        await Assertions.Expect(page.Locator("#header-progress-status"))
            .ToContainTextAsync("Select Open editor to unlock");
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Open editor", Exact = true })
            .ClickAsync();
        await Assertions.Expect(primaryPreview)
            .Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-preview"));
        var editablePalette = await primaryPreview.EvaluateAsync<string[]>("""
            preview => {
                const reviewPage = preview.querySelector('.steam-review-page');
                const reviewCard = preview.querySelector('.steam-review-card');
                return [
                    getComputedStyle(preview).backgroundColor,
                    getComputedStyle(reviewPage).backgroundColor,
                    getComputedStyle(reviewCard).backgroundColor
                ];
            }
            """);
        var finalPalette = await page.Locator(
                ".final-preview-panel .steam-preview")
            .EvaluateAsync<string[]>("""
                preview => {
                    const reviewPage = preview.querySelector('.steam-review-page');
                    const reviewCard = preview.querySelector('.steam-review-card');
                    return [
                        getComputedStyle(preview).backgroundColor,
                        getComputedStyle(reviewPage).backgroundColor,
                        getComputedStyle(reviewCard).backgroundColor
                    ];
                }
                """);
        Assert.Equal(finalPalette, editablePalette);
        Assert.Equal("rgb(27, 40, 56)", editablePalette[0]);
        Assert.Equal("rgb(27, 40, 56)", editablePalette[1]);
        Assert.Equal("rgb(13, 19, 27)", editablePalette[2]);
        await Assertions.Expect(page.Locator("#header-progress-status"))
            .ToContainTextAsync("Final Preview and Copy BBCode are ready");

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Guided", Exact = true })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "BBCode", Exact = true })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Start fresh" })
            .ClickAsync();
        await Assertions.Expect(primaryPreview)
            .Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-preview"));
        var bbCodeEditorSurface = page.Locator(".bbcode-editor-surface");
        await Assertions.Expect(bbCodeEditorSurface)
            .ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-editor-surface"));
        Assert.NotEqual(
            "none",
            await page.Locator("#raw-bbcode-editor").EvaluateAsync<string>(
                "element => getComputedStyle(element).filter"));
        await Assertions.Expect(ratingSystemGrid)
            .ToBeVisibleAsync();
        await AssertHeaderPathAsync(
            page,
            "Setup",
            "Composer",
            "Final Preview");
        await Assertions.Expect(page.Locator("#header-progress-status"))
            .ToContainTextAsync(
                "Choose a recommendation and select a starting point to unlock the editor.");
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { Name = "Recommended", Exact = true })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Radio,
                new PageGetByRoleOptions { Name = "Blank document" })
            .ClickAsync();
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Open composer", Exact = true })
            .ClickAsync();
        await Assertions.Expect(bbCodeEditorSurface)
            .Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-editor-surface"));
        await Assertions.Expect(ratingSystemGrid)
            .ToHaveCountAsync(0);

        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Unguided", Exact = true })
            .ClickAsync();
        await Assertions.Expect(primaryPreview)
            .Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-preview"));
        await Assertions.Expect(bbCodeEditorSurface)
            .Not.ToHaveClassAsync(new System.Text.RegularExpressions.Regex(
                "setup-locked-editor-surface"));
        await AssertHeaderPathAsync(
            page,
            "Write BBCode",
            "Final Preview");
        await Assertions.Expect(page.Locator("#header-progress-status"))
            .ToContainTextAsync(
                "Enter BBCode to unlock Final Preview and Copy BBCode.");
        await page.Locator("#raw-bbcode-editor").FillAsync("[b]Ready[/b]");
        await Assertions.Expect(page.Locator("#header-progress-status"))
            .ToContainTextAsync("Final Preview and Copy BBCode are ready");

        await page.SetViewportSizeAsync(1200, 1000);
        var wrappedLayout = await page.EvaluateAsync<double[]>("""
            () => {
                const brand = document.querySelector('.brand-lockup')
                    .getBoundingClientRect();
                const path = document.querySelector('.header-progress')
                    .getBoundingClientRect();
                return [brand.bottom, path.top];
            }
            """);
        Assert.True(wrappedLayout[1] >= wrappedLayout[0]);
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
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Continue to template" })
            .ClickAsync();

        var editablePreview = page.Locator(".preview-panel");
        var finalPreview = page.Locator(".final-preview-panel");

        await Assertions.Expect(
                editablePreview.Locator(".steam-table-actions-column"))
            .ToHaveCountAsync(0);

        var tableDeleteLayout = await editablePreview.EvaluateAsync<double[]>("""
            preview => {
                const lastColumnDelete = preview.querySelector(
                    '.steam-editable-table thead th:last-child .steam-remove-column')
                    .getBoundingClientRect();
                const tableDelete = preview.querySelector(
                    '.steam-remove-table').getBoundingClientRect();
                const table = preview.querySelector(
                    '.steam-editable-table').getBoundingClientRect();
                const addRow = preview.querySelector(
                    'button[aria-label="Add rating table row"]')
                    .getBoundingClientRect();
                return [
                    lastColumnDelete.left,
                    lastColumnDelete.right,
                    lastColumnDelete.top,
                    lastColumnDelete.bottom,
                    tableDelete.left,
                    tableDelete.right,
                    tableDelete.top,
                    tableDelete.bottom,
                    table.left,
                    table.right,
                    table.top,
                    table.bottom,
                    addRow.top,
                    addRow.bottom
                ];
            }
            """);
        Assert.True(
            tableDeleteLayout[6] >= tableDeleteLayout[3],
            "The whole-table delete action must sit below the column controls.");
        Assert.InRange(
            tableDeleteLayout[4],
            tableDeleteLayout[8],
            tableDeleteLayout[9]);
        Assert.InRange(
            tableDeleteLayout[5],
            tableDeleteLayout[8],
            tableDeleteLayout[9]);
        Assert.InRange(
            tableDeleteLayout[6],
            tableDeleteLayout[10],
            tableDeleteLayout[11]);
        Assert.InRange(
            tableDeleteLayout[7],
            tableDeleteLayout[10],
            tableDeleteLayout[11]);
        Assert.True(
            tableDeleteLayout[6] < tableDeleteLayout[13] &&
            tableDeleteLayout[7] > tableDeleteLayout[12],
            "Add-row and remove-table controls must share the table footer row.");
        var removeTableButton = editablePreview.GetByRole(
            AriaRole.Button,
            new LocatorGetByRoleOptions
            {
                Name = "Remove rating table",
                Exact = true
            });
        await removeTableButton.HoverAsync();
        await Assertions.Expect(editablePreview.GetByRole(AriaRole.Tooltip))
            .ToHaveCSSAsync("opacity", "1");

        var widthToggle = editablePreview.GetByRole(
            AriaRole.Switch,
            new LocatorGetByRoleOptions
            {
                Name = "Rating table cell width mode: Equal",
                Exact = true
            });
        await Assertions.Expect(widthToggle)
            .ToHaveAttributeAsync("aria-checked", "true");
        await widthToggle.ClickAsync();
        await Assertions.Expect(editablePreview.GetByRole(
                AriaRole.Switch,
                new LocatorGetByRoleOptions
                {
                    Name = "Rating table cell width mode: Auto",
                    Exact = true
                }))
            .ToHaveAttributeAsync("aria-checked", "false");
        await Assertions.Expect(finalPreview.Locator(
                "table.preview-table-equal"))
            .ToHaveCountAsync(0);

        await editablePreview.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Table Editor", Exact = true })
            .ClickAsync();
        var tableEditor = page.GetByRole(
            AriaRole.Dialog,
            new PageGetByRoleOptions { Name = "Table Editor", Exact = true });
        await Assertions.Expect(tableEditor.GetByRole(
                AriaRole.Switch,
                new LocatorGetByRoleOptions
                {
                    Name = "Table Editor cell width mode: Auto",
                    Exact = true
                }))
            .ToBeVisibleAsync();
        await tableEditor.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "Done", Exact = true })
            .ClickAsync();

        await finalPreview.GetByRole(
                AriaRole.Button,
                new LocatorGetByRoleOptions { Name = "View BBCode", Exact = true })
            .ClickAsync();
        Assert.Contains(
            "[table]\n",
            await page.GetByLabel("Generated Steam BBCode").InputValueAsync());
        await page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "Close BBCode viewer", Exact = true })
            .ClickAsync();
        var tableRowEditorLayout = await editablePreview
            .Locator(".steam-editable-table tbody tr")
            .First
            .EvaluateAsync<double[]>("""
                row => {
                    const cell = row.querySelector('.steam-category-column')
                        .getBoundingClientRect();
                    const input = row.querySelector('.steam-category-name')
                        .getBoundingClientRect();
                    const controls = row.querySelector(
                        '.steam-category-controls').getBoundingClientRect();
                    return [
                        cell.right,
                        controls.right,
                        input.top,
                        input.bottom,
                        controls.top,
                        controls.bottom
                    ];
                }
                """);
        Assert.True(tableRowEditorLayout[1] <= tableRowEditorLayout[0]);
        Assert.True(tableRowEditorLayout[4] < tableRowEditorLayout[3]);
        Assert.True(tableRowEditorLayout[5] > tableRowEditorLayout[2]);

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

    private static async Task AssertHeaderPathAsync(
        IPage page,
        params string[] expectedLabels)
    {
        var headerPath = page.Locator(".app-header > .header-progress");
        await Assertions.Expect(headerPath).ToBeVisibleAsync();

        var labels = await headerPath
            .Locator(".header-progress-label")
            .AllTextContentsAsync();
        Assert.Equal(
            expectedLabels,
            labels.Select(label => label.Trim()).ToArray());
    }

    private static async Task AssertSteamSizedFinalPreviewAsync(IPage page)
    {
        await Assertions.Expect(page.GetByRole(
                AriaRole.Button,
                new PageGetByRoleOptions { Name = "View BBCode" }))
            .ToHaveCountAsync(1);

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

    private static int CountOccurrences(
        string value,
        string token) =>
        value.Split(
            token,
            StringSplitOptions.None).Length - 1;

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
