# Steam Review Forge release-readiness review

Reviewed October 3, 2026.

## Stabilization follow-up

The implementation now addresses findings 1–5: empty category lists survive reload, wrong-root JSON enters recovery, the bounded preview parser preserves trailing/multiline formatting and supports Steam's documented tables and links, and desktop selection/dialog accessibility is repaired. Analytics has been removed, production publishing adds a hashed Content Security Policy, and .NET patches have been updated.

Per the owner's direction, automatic release checks will **not** be restored. Checks remain manual, with the published production build covered by the test workflow and [manual checklist](manual-release-checklist.md). Desktop Firefox/Chromium and Steam formatting are the release focus; Safari/mobile are not release requirements. The GitHub README explicitly states that the project was vibe coded with AI assistance and will receive limited maintenance; this disclaimer is not displayed on the website.

Validation after the fixes: 86 unit tests and 24 Firefox/Chromium browser cases passed. The browser suite ran against published static assets with the production security policy. The updated direct/transitive NuGet vulnerability audit reported no vulnerable packages. GitHub Actions references are pinned to immutable commits.

The original assessment below is retained as historical evidence, not a description of the fixed working tree. These fixes have not yet been deployed. Actual Steam-editor rendering remains a manual acceptance check; documentation-based regression tests do not claim to run Steam's renderer.

---

**Assessment: the core application works, but hold the stable release for the reliability, preview, accessibility, and release-process fixes below.** No critical security vulnerability was confirmed in this review. Passing tests and clean scanners do not establish that the application has no vulnerabilities.

## Scope and current status

- Local application code: `b4c7143`.
- Published branch: `origin/main` at `8ae43b5`. Its application source is identical to the local checkout. Differences are in workflows and documentation; the checkout was not reset or merged.
- Latest published release is still the `v0.2.0` prerelease. [The v1.0 issue](https://github.com/ArgyrosForge/steam-review-forge/issues/32) remains open.
- The site is a static .NET 10 Blazor WebAssembly application. It has no application backend, database, credentials, or account system. Drafts use browser local storage; analytics loads a third-party script.
- Application files were not changed during this review. This report is the only added project file. The pre-existing untracked `.codex/` directory was left untouched.

## Verification results

| Check | Result |
| --- | --- |
| Release unit tests | 61 passed, 0 failed, 0 skipped |
| Firefox/Chromium suite against local Release app | 15 passed, 0 failed, 0 skipped |
| Release publish | Succeeded; build advises installing optional `wasm-tools` for additional optimization |
| Firefox/Chromium suite against published static output | 15 passed, 0 failed, 0 skipped |
| NuGet vulnerability audit, direct and transitive packages | No vulnerable packages reported by the current feed for all three projects |
| GitHub CodeQL | Latest inspected scheduled scan succeeded; open code-scanning alerts API returned an empty list |
| Live site | Loaded; structured editing, raw editing, preview, saving/restoring, and actual clipboard export exercised |
| Basic preview injection probes | HTML encoded; `javascript:` and `data:` links not rendered as clickable links |
| Live browser logs | No warning/error entries during the inspected interactions |
| Narrow viewport | 390×844 CSS viewport fit without document-level horizontal overflow in the inspected BBCode workspace |

The local SDK is 10.0.112; CI pins 10.0.110 and application packages pin 10.0.10. Local test results therefore are not proof of identical runtime behavior on every deployed/browser combination. Browser automation mocks the clipboard; the separate live check verified real copied text.

## Findings to address before v1.0

### 1. Deleted rating tables return after reload — P2

**Confirmed on the live site and in a storage round-trip probe.** Choose Recommended, continue to Template, remove the rating table, wait for Saved, and reload. A table containing `New Category` with a three-star rating returns.

`NormalizeCategories` treats an intentionally empty category list as invalid and inserts a default row. This changes the user's saved review and generated output. It also affects removing the last category in other non-minimal layouts.

Source: [ReviewDraftStorageService.cs:343](../src/SteamReviewForge/Services/ReviewDraftStorageService.cs#L343).

**Fix:** preserve an explicitly empty list in current-schema drafts. Only apply legacy defaults when a field is missing or a specific migration requires it. Add a regression covering remove → save → reload → copy, including a Full Custom draft.

### 2. Preview silently drops text after code, quote, and noparse blocks — P2

**Confirmed on the live site and directly against the renderer.** For example:

```text
[code]First part[/code] THIS TEXT MUST REMAIN
```

The preview displays only `First part`. The trailing text remains in the editor and copied BBCode, so the preview misrepresents what will be posted. The analyzer reports zero diagnostics for this example. The same happens with `[quote]` and `[noparse]`, including text following a closing tag on a later line.

Source: [SteamBbCodePreviewRenderer.cs:320](../src/SteamReviewForge/Services/SteamBbCodePreviewRenderer.cs#L320) and line 342.

**Fix:** continue parsing the remainder after a closing block tag rather than discarding it. Test same-line and multiline closing tags, consecutive blocks, and trailing prose. The inline renderer also emits invalid paragraph nesting for formatting spanning multiple lines, which should be included in parser tests.

### 3. Malformed pasted BBCode can stall the editor — P2

**Measured, not just inferred from code.** Repeating `[url=https://example.com]` without closing tags triggers increasingly expensive regular-expression searches. The pattern has no explicit timeout. Unclosed block tags also repeatedly scan the remaining lines. Rendering runs synchronously during UI updates; only saving is debounced.

Native Release probe results on this machine:

| Input | Render time |
| --- | ---: |
| 6,250 characters of unclosed URL tags | 15 ms |
| 12,500 characters | 61 ms |
| 25,000 characters | 230 ms |
| 50,000 characters | 778 ms |

On the live WebAssembly site, the browser fill operation took approximately **0.24 seconds at 6,250 characters and 2.90 seconds at 25,000 characters**. These are automation round-trip measurements, not isolated render timings. The larger input also produced 1,000 warnings in each of two panels. Ordinary short sample reviews were responsive.

Sources: [SteamBbCodePreviewRenderer.cs:9](../src/SteamReviewForge/Services/SteamBbCodePreviewRenderer.cs#L9), [block scanning at line 328](../src/SteamReviewForge/Services/SteamBbCodePreviewRenderer.cs#L328), [Home.razor:2477](../src/SteamReviewForge/Pages/Home.razor#L2477).

**Fix:** use bounded or linear parsing, limit rendered diagnostics, cache/debounce preview generation, and define a supported input-size budget. Handle oversized saved drafts gracefully too. This is a browser availability concern triggered by pasted/restored input; no remote drive-by exploit was demonstrated.

### 4. Some malformed saved drafts bypass recovery protection — P2

**Confirmed in a direct storage-service probe.** With `[]`, `null`, `42`, or a JSON string as the current saved payload, `LoadAsync` throws `InvalidOperationException`. Invalid JSON syntax correctly returns a recoverable result, but valid JSON of the wrong root type does not.

`LoadCurrent` calls `TryGetProperty` without first checking that the root is an object. Its catch clauses do not handle the resulting exception. The page's general restore catch labels this “Draft storage unavailable” and enables saving, allowing the next edit to overwrite the existing payload without the recovery dialog.

Sources: [ReviewDraftStorageService.cs:138](../src/SteamReviewForge/Services/ReviewDraftStorageService.cs#L138), [Home.razor:5704](../src/SteamReviewForge/Pages/Home.razor#L5704).

**Fix:** validate root type and return `Invalid` with the raw backup intact. Keep persistence disabled until recovery is explicitly resolved. Test wrong-root JSON as well as malformed syntax. Ordinary application saves produce objects, so this finding concerns damaged or incompatible storage rather than the normal save path.

### 5. Selection states and confirmation dialogs need accessibility fixes — P2

**Confirmed in the live DOM and keyboard interaction.** After selecting Recommended, its radio exposes `aria-checked=""`; the unselected option omits the attribute. This is not the required explicit `true`/`false` state. Other raw Boolean bindings, including star ratings and editing-mode buttons, deserve the same correction.

Opening New Review leaves focus on the underlying New Review button. Pressing Escape does not dismiss the dialog. The modal markup does not move/trap focus or make the underlying editor inert. Keyboard and assistive-technology users cannot reliably follow the visual interaction.

Sources: [Home.razor:256](../src/SteamReviewForge/Pages/Home.razor#L256), [mode controls at line 100](../src/SteamReviewForge/Pages/Home.razor#L100), [dialog at line 1594](../src/SteamReviewForge/Pages/Home.razor#L1594), [star controls at line 4307](../src/SteamReviewForge/Pages/Home.razor#L4307).

**Fix:** emit explicit ARIA strings, supply appropriate radio-group keyboard behavior, and use one consistent accessible dialog implementation with focus entry, containment, Escape handling, and focus restoration. Add a keyboard-only workflow check and assertions on selected states.

### 6. The deployed branch no longer gates releases on tests — release-process priority

The local checkout still contains the test gate, but current `origin/main` removed `pull_request` and `workflow_call` from the test workflow and removed the Pages `quality` job and `needs: quality`. Tests are now manual only. Scheduled CodeQL scanning remains active, but does not replace functional tests.

Sources: [published tests workflow](https://github.com/ArgyrosForge/steam-review-forge/blob/8ae43b5/.github/workflows/tests.yml), [published Pages workflow](https://github.com/ArgyrosForge/steam-review-forge/blob/8ae43b5/.github/workflows/pages.yml).

**Fix:** restore automatic unit tests and a small critical browser suite before publishing. Prefer testing the published static output, since trimming and deployment asset handling can differ from the development host. Update architecture documentation to reflect the final policy; it currently describes a gate that the deployed branch no longer has.

## Security and maintenance assessment

The static architecture is a useful advantage: there is no application server to patch or user credential store to protect. The preview encodes HTML and restricts rendered links to HTTP(S). No direct application-source injection vulnerability was confirmed by inspection and the probes performed. NuGet and GitHub CodeQL returned no current findings.

The clearest hardening opportunity is the externally hosted GoatCounter script in [index.html:42](../src/SteamReviewForge/wwwroot/index.html#L42). It is loaded without an integrity hash, and the live page has no Content Security Policy in either its response headers or HTML. This is **a trust/supply-chain exposure, not evidence of compromise**: a replaced analytics script would execute in the page and could access local drafts. For minimal future maintenance, removing analytics eliminates this dependency; alternatively, self-host or pin reviewed script bytes with integrity protection and add a tested CSP. [MDN's integrity guidance](https://developer.mozilla.org/en-US/docs/Web/Security/Defenses/Subresource_Integrity) and [CSP guidance](https://developer.mozilla.org/en-US/docs/Web/Security/Practical_implementation_guides/CSP) explain these protections.

The application references .NET packages 10.0.10. Microsoft's support page lists 10.0.12 as the current patch at review time and .NET 10 support through November 14, 2028. Update the SDK and package patch versions before release, then plan occasional security servicing. A static WebAssembly site still ships framework code to browsers. [Microsoft support policy](https://dotnet.microsoft.com/en-us/platform/support/policy).

Pin third-party CI actions to reviewed commit hashes and enable dependency-update notifications if not already enabled in repository settings. No `dependabot.yml` is tracked; repository-side Dependabot settings were not audited. GitHub account access, branch protection, secrets configuration, and infrastructure permissions were outside this source/browser review.

## Performance and durability follow-ups

- The live native WebAssembly runtime is served with gzip, approximately 1.20 MB transferred for that asset alone. Compression is working. This is not the entire initial download.
- A local publish produced about 7.10 MB of uncompressed `.wasm` files and approximately 3.10 MB of `.br` files across all compressed assets, plus about 0.30 MB of font files. These are artifact totals, not a measured cold page load; the browser does not necessarily load every artifact.
- Profile a cold load on a throttled mobile connection before release. The optional `wasm-tools` optimization is worth measuring; full AOT is not automatically a size/performance improvement for this app.
- `Home.razor` is approximately 6,000 lines. Avoid expanding it for a long feature backlog before stabilization. Focused extraction of dialogs and preview/persistence coordination would reduce future repair risk, but a full rewrite is unnecessary for release.
- Local storage is a single active draft, not a backup. A JSON download/import path would provide recovery and portability. Multi-tab conflicts are not handled by the inspected persistence code; decide whether to warn about another active tab or detect revisions before overwriting.

## Short path to stable release

1. Freeze the feature set for v1.0. The open composer/component backlog need not all ship to provide a dependable review editor.
2. Fix the five confirmed functional/accessibility findings and add targeted regressions, including save/reload after deleting content.
3. Restore the release test gate; update framework patches and choose whether to keep third-party analytics.
4. Verify Safari/WebKit and real iOS/Android editing, keyboard-only use, storage failure/recovery, large pastes, and an actual Steam paste/formatting check. Do not submit a public Steam review merely for testing.
5. Publish a release candidate, perform the final smoke pass on the deployed assets, then tag v1.0 with a short maintenance policy and an explicit supported-browser list.

## Limits of this assessment

This was a source review, dependency/scanner check, targeted adversarial input exercise, automated Firefox/Chromium workflow run, and live-site inspection. It was not an exhaustive penetration test, a formal accessibility audit, a real-device compatibility matrix, or a throttled load benchmark. Safari/WebKit, mobile OS lifecycle behavior, and fidelity inside Steam itself remain unverified. No source changes were deployed and no Steam review was submitted.
