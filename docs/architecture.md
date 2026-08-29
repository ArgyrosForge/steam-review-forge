# Architecture

This document describes the current architecture of Steam Review Forge and the boundaries contributors should preserve as the application grows.

## Overview

Steam Review Forge is a client-side .NET 10 Blazor WebAssembly application. The browser downloads the application and runs the review-building workflow locally.

The repository currently contains:

- One Blazor WebAssembly application project
- One xUnit test project
- No application server or web API
- No remote database
- No authentication or user accounts
- Browser local storage for draft and theme persistence
- JavaScript interoperability for browser-only capabilities
- Static hosting through GitHub Pages
- GoatCounter for aggregate page-view analytics

The application remains deployable as static web assets and does not require a server-side .NET runtime in production.

## System Context

```text
User
  |
  v
Blazor WebAssembly application
  |
  +--> Review builder UI
  +--> Review templates
  +--> Validation
  +--> BBCode generation
  +--> Rendered preview
  |
  +--> Browser APIs
  |      +--> localStorage
  |      +--> Clipboard API
  |      +--> Preferred color scheme
  |
  +--> GoatCounter
         +--> Aggregate page-view analytics
```

Review draft content remains in the browser unless the user manually copies or exports it. GoatCounter receives page-view analytics, not review draft content.

## Project Structure

```text
SteamReviewForge.slnx
├── src/SteamReviewForge/SteamReviewForge.csproj
└── tests/SteamReviewForge.Tests/SteamReviewForge.Tests.csproj
```

Application source:

```text
src/SteamReviewForge/
├── Models/       # Review state, enums, categories, and validation results
├── Pages/        # Routed Blazor pages and review-builder orchestration
├── Services/     # Templates, validation, persistence, generation, and preview
├── Layout/       # Shared application layout components
├── wwwroot/      # Static assets, JavaScript bridges, themes, and global styles
├── App.razor     # Application router
└── Program.cs    # Blazor host and dependency registration
```

Tests:

```text
tests/SteamReviewForge.Tests/
├── SteamReviewForge.Tests.csproj
└── ReviewValidationResultTests.cs
```

The test project references the application project directly.

## Application Entry Point

`Program.cs` creates the Blazor WebAssembly host, registers the root components, and adds application services to dependency injection.

`App.razor` resolves routes and uses the shared layout. The primary review-builder page is the root route at `/`.

## Primary UI Component

The main review-building workflow currently lives in `Pages/Home.razor`.

It is responsible for:

- Holding the active `ReviewDraft`
- Switching between guided and unguided structured or BBCode editing
- Tracking the selected workflow step
- Applying templates
- Editing categories and structured review components
- Triggering validation
- Generating BBCode and rendered previews
- Saving and restoring drafts
- Copying BBCode to the clipboard
- Managing appearance and new-review interactions

The page acts as the orchestration layer. Deterministic formatting, validation, persistence, template, and preview behavior should remain in services where practical.

As the application grows, independent UI areas should be extracted into focused Blazor components rather than continuing to expand the root page.

## Domain Model

### `ReviewDraft`

`ReviewDraft` is the central mutable state object. It contains the review metadata, selected template and display format, rating configuration, category content, guided responses, structured components, and freeform BBCode content where applicable.

Recommendation, playtime, and received-for-free status are stored with the draft and displayed in the Steam-style preview. They are intentionally excluded from generated BBCode because Steam captures them as review metadata.

### `ReviewCategory`

Each category contains a stable identifier, display name, rating value, optional note, and custom text values for configurable table columns.

### `ReviewTableColumn`

Each rating-table column has a stable identifier, editable heading, and kind. Built-in Category, Rating, and Note columns are unique; custom text columns can be added as needed.

### Enumerations

Bounded choices use enums rather than free-form strings, including review templates, display formats, rating systems, recommendation state, and validation severity/section values.

## Service Responsibilities

### `ReviewTemplateService`

Applies a selected template to an existing `ReviewDraft` while preserving fields that are outside the template's ownership boundary.

### `ReviewDraftValidator`

Validates the current draft and returns a `ReviewValidationResult` containing errors and warnings. Validation is grouped by workflow section and field identifier so the UI can surface actionable messages.

### `SteamBbCodeGenerator`

Transforms a `ReviewDraft` into deterministic Steam-compatible BBCode. It is browser-independent application logic and is a primary target for unit-test coverage.

### `SteamBbCodePreviewRenderer`

Converts the supported Steam BBCode subset into HTML for the in-application preview. User-provided text is HTML encoded before supported formatting is rendered.

The preview is intentionally an approximation of Steam rendering rather than a general-purpose BBCode parser.

### `ReviewDraftStorageService`

Serializes the active `ReviewDraft` as JSON and stores it through a small JavaScript local-storage bridge.

The current storage key is:

```text
steam-review-forge-draft-v1
```

The application currently supports one active locally stored draft.

## Editing Modes

The editor exposes four experiences formed from two independent choices:

- Structured or BBCode editing
- Guided or Unguided flow

Switching between Guided and Unguided preserves content. Switching between Structured and BBCode starts a new review because arbitrary BBCode cannot be safely converted back into the typed structured model.

Structured modes operate on typed review data and generate BBCode from that data. BBCode modes expose editable raw BBCode with live rendering.

## Browser Interoperability

Small JavaScript bridges under `wwwroot` provide browser-specific functionality.

### Draft storage

`window.reviewDraftStorage` wraps `localStorage` operations used by `ReviewDraftStorageService`.

### Clipboard

`window.clipboardManager` uses the browser Clipboard API to copy generated BBCode.

### Theme

`window.themeManager` manages brand theme and light/dark appearance settings and persists them in browser storage.

Theme palettes live under `wwwroot/css/themes` and expose a shared semantic token contract consumed by application CSS.

### Drag and editor helpers

Additional JavaScript modules provide table drag behavior, validation navigation, BBCode editor helpers, and structured component drag-and-drop behavior where direct browser APIs are more practical than Blazor-only handling.

JavaScript should remain limited to browser-facing capabilities rather than core review logic.

## Data and Persistence Boundaries

Persistent browser state currently includes:

- Review draft JSON
- Theme preference

There is no account-based or cross-device synchronization. Clearing browser storage removes the locally stored draft and appearance state.

Local storage should not be presented as a durable backup. Future incompatible storage changes should either migrate prior data explicitly or use a new versioned key.

## Privacy and Analytics

The current application has no account system, remote review database, or application API.

Review draft content is stored locally in the browser and is not sent to an application server.

The hosted GitHub Pages site loads GoatCounter to collect aggregate page-view analytics. Review draft content is not intentionally included in GoatCounter analytics.

Contributors should document any future feature that sends user-entered content or additional data outside the browser, including cloud storage, sharing, game lookup, or other external integrations.

## Security

The current design keeps the server-side attack surface narrow because the application is static and client-side.

Important boundaries include:

- User review content is not stored on an application server
- No credentials or application accounts are handled
- Preview input is HTML encoded before rendering
- Generated BBCode remains visible to the user before it is copied to Steam

Validation and previewing reduce mistakes but do not guarantee that Steam will render every character or future tag behavior identically.

## Error Handling

Browser storage and clipboard operations can fail because of browser permissions, privacy modes, unavailable APIs, or storage restrictions.

The application should continue to fail gracefully when optional browser capabilities are unavailable rather than terminating the review session.

## Deployment Model

The application is published as static Blazor WebAssembly assets and deployed through GitHub Pages.

The GitHub Actions Pages workflow:

- Publishes the Blazor project in Release configuration
- Rewrites the application base path for `/steam-review-forge/`
- Adds `.nojekyll`
- Copies `index.html` to `404.html` for static-host routing fallback
- Uploads and deploys the published `wwwroot` directory

The production site is:

`https://argyrosforge.github.io/steam-review-forge/`

No server-side .NET runtime is required after publishing.

## Testing Strategy

A dedicated xUnit project exists under `tests/SteamReviewForge.Tests`.

Current automated coverage begins with `ReviewValidationResult` behavior. Planned coverage includes:

1. `SteamBbCodeGenerator`
2. `ReviewDraftValidator`
3. `ReviewTemplateService`
4. `SteamBbCodePreviewRenderer`, including HTML encoding
5. Draft serialization and restoration
6. Blazor component behavior
7. Browser-level workflow, storage, clipboard, responsive, and keyboard behavior

Tests can be run locally with:

```bash
dotnet test
```

The GitHub Actions test workflow is intentionally manual-only and uses `workflow_dispatch`. Tests do not run automatically on pushes or pull requests.

Critical output tests should eventually use representative drafts for every template and display format.

## Current Architectural Constraints

- `Home.razor` still contains substantial UI orchestration and should be decomposed as features grow
- Only one draft can be stored at a time
- Local storage is synchronous behind the JavaScript bridge and is not intended for large datasets
- The preview renderer supports a bounded Steam BBCode subset
- Automated test coverage is still limited and is being expanded during `v0.2.0`
- There is no application backend for shared links, synchronization, accounts, or remote metadata

## Extension Guidelines

When adding features:

- Keep review data in typed models
- Keep deterministic transformation logic in testable services
- Keep browser API access behind small interoperability boundaries
- Derive previews and output from the draft rather than duplicating state
- Version persisted data when making incompatible changes
- HTML encode user content before rendering it as markup
- Avoid adding server infrastructure unless the feature genuinely requires it
- Extract focused Blazor components before the root page becomes harder to maintain
- Document privacy changes whenever information leaves the browser
- Expand tests deliberately; do not assume the manual GitHub workflow implies automatic CI

## Potential Future Evolution

The current architecture can support several planned features without an application server, including JSON import/export, multiple local drafts, custom local templates, Markdown/plain-text export, and additional BBCode layouts.

Features such as cloud synchronization, public share links, accounts, or centrally managed community templates would introduce a backend boundary. Those changes should be treated as a separate architectural phase with explicit API, storage, authentication, privacy, and deployment decisions.
