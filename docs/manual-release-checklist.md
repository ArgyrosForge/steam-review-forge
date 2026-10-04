# Manual desktop release checks

This project was vibe coded with AI assistance and receives limited maintenance. Current desktop Firefox and Chromium-based browsers are the release target. Safari and mobile checks are not release requirements. Test runs are manual; deployment does not wait for them.

## Run the checks

In GitHub, select **Actions → Run Tests → Run workflow** for the exact branch/commit you intend to release. Check that both the unit tests and Firefox/Chromium browser tests pass. That workflow tests the published static output after applying its security policy.

For local verification with the SDK in `global.json` and Python 3 installed:

```bash
dotnet test tests/SteamReviewForge.Tests/SteamReviewForge.Tests.csproj -c Release
dotnet build tests/SteamReviewForge.BrowserTests/SteamReviewForge.BrowserTests.csproj -c Release
```

Install the browser versions used by the test project if necessary:

```powershell
pwsh tests/SteamReviewForge.BrowserTests/bin/Release/net10.0/playwright.ps1 install firefox chromium
```

Publish the app and prepare the same security policy used for GitHub Pages:

```bash
dotnet publish src/SteamReviewForge/SteamReviewForge.csproj -c Release -o publish
python3 scripts/prepare-pages.py publish/wwwroot --base-path /
python3 -m http.server 5080 --bind 127.0.0.1 --directory publish/wwwroot
```

With that server still running, use a second terminal:

```bash
dotnet test tests/SteamReviewForge.BrowserTests/SteamReviewForge.BrowserTests.csproj -c Release --no-build
dotnet list SteamReviewForge.slnx package --vulnerable --include-transitive
```

For deployment under a project path, run `prepare-pages.py` with its default `/steam-review-forge/` path instead. Do not edit inline scripts after preparation without rerunning the script, since their hashes are part of the security policy.

## Desktop smoke pass

- Complete a guided structured review, including a table, then copy its BBCode.
- Remove the table, save, and reload. Confirm it remains absent from the preview and copied output.
- Open, close, and Escape out of dialogs using only the keyboard; confirm focus returns to the invoking button. Arrow keys should change custom radio selections.
- Open an existing raw draft and confirm edits are saved and restored. Check warning and recovery paths in the automated suite.
- Check a long malformed paste remains editable. Inputs over 50,000 characters should show the preview/checks-paused message while retaining the full saved and copied text.
- Confirm the published page loads without Content Security Policy or application errors in browser developer tools.
- Verify the GitHub README retains the vibe-coding/limited-maintenance disclaimer, the website does not display it, and no analytics script is loaded.

## Steam formatting verification

The automated corpus is based on [Valve's review formatting reference](https://steamcommunity.com/comment/Recommendation/formattinghelp), checked October 3, 2026. It covers headings, emphasis, underline, strike, spoilers, literal/code blocks, attributed quotes, links (including hostnames without a scheme), unordered/ordered lists, and multiline tables. The sample is checked by analyzer/preview regression tests; generated templates and layouts are checked separately. Widgets for YouTube, store pages, and community items are represented as links in Forge's preview rather than live third-party embeds.

Before a release, use your own Steam review editor to paste sample output and inspect it without publishing a test review. Actual behavior inside Steam is a separate manual check; automated Forge tests do not claim to run Steam's renderer.

Check all four structured layouts and each rating system, including Unicode stars. Check that copied content matches the selected template and the visible preview. Recommendation, playtime, free-product status, and Early Access are metadata you set separately in Steam; they intentionally are not included in generated BBCode.

Use the sample in [steam-formatting-sample.txt](steam-formatting-sample.txt) to check:

- Text after code, quote, and noparse closing tags remains present.
- Bold and italic formatting can span lines without swallowing later text.
- Links, nested lists, tables, and table options match Steam's supported syntax.
- Spoilers conceal their contents until revealed.
- A plain store URL may become a Steam widget; Forge displays a safe link placeholder.

If Steam changes its supported tags or limits, update the fixture and diagnostics. The preview is an approximation, not a guarantee of identical layout. Keep a separate copy of important drafts and check every review in Steam before posting.

## Record the release

Record the commit, test results, desktop browser versions, and whether the Steam editor pass was completed. Review the latest dependency advisories and GitHub code-scanning alerts. Fix failing checks before manually deploying. Limited maintenance does not mean that this checklist has already been performed for every future change.
