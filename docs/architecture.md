# Architecture

This document describes the current architecture of Steam Review Forge and the boundaries contributors should preserve as the application grows.

## Overview

Steam Review Forge is a client-side .NET 10 Blazor WebAssembly application. The browser downloads the application and runs the review-building workflow locally.

The project currently has:

- One Blazor WebAssembly project
- No application server or web API
- No remote database
- No authentication or user accounts
- Browser local storage for draft and theme persistence
- JavaScript interoperability for browser-only capabilities

This architecture keeps deployment and operation simple: the application can be hosted as static web assets and does not require server-side application infrastructure.

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
         +--> localStorage
         +--> Clipboard API
         +--> Preferred color scheme
```

All review content remains inside the browser unless the user manually copies or exports it.

## Project Structure

```text
src/SteamReviewForge/
├── Models/       # Review state, enums, categories, and validation results
├── Pages/        # Routed Blazor pages and review-builder orchestration
├── Services/     # Templates, validation, persistence, generation, and preview
├── Layout/       # Shared application layout components
├── wwwroot/      # Static assets, JavaScript bridges, and global styles
├── App.razor     # Application router
└── Program.cs    # Blazor host and dependency registration
```

The solution currently contains a single application project:

```text
SteamReviewForge.slnx
└── src/SteamReviewForge/SteamReviewForge.csproj
```

## Application Entry Point

`Program.cs` creates the Blazor WebAssembly host, registers the root components, and adds `ReviewDraftStorageService` to dependency injection.

The application router in `App.razor` resolves routes and uses the shared layout. The primary review-builder page is the root route at `/`.

## Primary UI Component

The main review-building workflow currently lives in `Pages/Home.razor`.

It is responsible for:

- Holding the active `ReviewDraft`
- Tracking the selected workflow step
- Applying templates
- Adding and removing categories
- Triggering validation
- Generating BBCode and rendered previews
- Saving and restoring drafts
- Copying BBCode to the clipboard
- Managing theme and new-review interactions

The page is intentionally the orchestration layer. Formatting, validation, persistence, and template behavior are delegated to services where practical.

As the application grows, large independent UI areas should be extracted into focused Blazor components rather than continuing to expand the root page.

## Domain Model

### `ReviewDraft`

`ReviewDraft` is the central mutable state object for the application. It contains:

- Review title
- Summary
- Recommendation
- Selected template
- Selected display format
- Playtime
- Strengths
- Weaknesses
- Final thoughts
- A collection of review categories

The initial property values form the default Balanced Review experience.

### `ReviewCategory`

Each category contains:

- A generated `Guid` identifier
- A display name
- A rating from 1 through 5
- An optional note

The identifier is used to preserve UI identity and associate validation messages with a specific category.

### Enumerations

The model uses enums rather than free-form strings for bounded choices:

- `ReviewTemplate`: Balanced, Quick Take, Deep Dive, and Custom
- `ReviewDisplayFormat`: Rating Table, Sections, Checklist, and Minimal Verdict
- `ReviewRecommendation`: Recommended, Mixed, and Not Recommended
- Validation section and severity enums

This keeps UI options, service behavior, and generated output aligned through compile-time types.

## Service Responsibilities

### `ReviewTemplateService`

Applies a selected template to an existing `ReviewDraft`.

A template can replace:

- Display format
- Summary text
- Guided-response text
- Final thoughts
- Category collection

Templates mutate the existing draft rather than replacing the object. This allows the page to keep one state reference while updating the template-controlled fields.

### `ReviewDraftValidator`

Validates the current draft and returns a `ReviewValidationResult` containing errors and warnings.

Errors block BBCode copying. Warnings provide guidance but do not make the draft invalid.

Validation is grouped by the Setup, Format, and Questions workflow sections. Field identifiers allow the UI to show messages beside the relevant input and decorate workflow steps with status indicators.

### `SteamBbCodeGenerator`

Transforms a `ReviewDraft` into Steam-compatible BBCode.

Generation is deterministic and has no browser dependencies. Output varies according to the selected display format:

- Rating Table emits Steam table tags
- Sections emits an individual heading for each category
- Checklist emits checked or unchecked category rows
- Minimal Verdict omits category output and focuses on the explanation

The generator also adds the title, optional summary and playtime, questionnaire content, dividers, star ratings, and final recommendation.

Because this service is pure application logic, it should remain independent from UI state and JavaScript. It is a primary target for unit testing.

### `SteamBbCodePreviewRenderer`

Converts the generated subset of Steam BBCode into HTML for the in-application preview.

The renderer supports the tags and structures produced by `SteamBbCodeGenerator`, including:

- Headings
- Bold and italic text
- Dividers
- Tables
- Bullet-style response lines
- Checklist rows

User-provided text is HTML encoded before supported formatting tags are converted. This prevents review text from being treated as arbitrary HTML in the preview.

The preview is an approximation of Steam rendering, not a complete general-purpose BBCode parser.

### `ReviewDraftStorageService`

Serializes the current `ReviewDraft` as JSON and stores it through a small JavaScript local-storage bridge.

The storage key is versioned:

```text
steam-review-forge-draft-v1
```

The service supports loading, saving, and clearing one active draft.

The page debounces ordinary input saves and performs immediate saves after structural changes such as selecting a template or modifying categories.

## Browser Interoperability

Browser-specific behavior is exposed through small JavaScript bridges in
`wwwroot`.

### Draft storage

`window.reviewDraftStorage` wraps `localStorage` operations used by `ReviewDraftStorageService`.

### Clipboard

`window.clipboardManager` copies generated BBCode through the browser Clipboard API. Copy failures are caught by the Blazor page, which tells the user to select the BBCode manually.

### Theme

`window.themeManager`:

- Registers the available brand themes
- Treats brand theme and light/dark color mode as independent settings
- Defaults new users to the Main Blue theme in dark mode
- Migrates the previous light/dark preference when present
- Applies both settings before styles load to prevent a theme flash
- Persists the versioned appearance state in local storage

Theme palettes live under `wwwroot/css/themes`. Each theme defines the shared
semantic token contract for both `dark` and `light` color modes. Application
styles consume only semantic tokens, so future themes can be added without
duplicating component CSS.

JavaScript should remain limited to browser APIs that are not conveniently or reliably available directly through Blazor.

## Data Flow

### Editing and preview flow

```text
User edits a field
  |
  v
ReviewDraft is updated
  |
  +--> ReviewDraftValidator.Validate
  |
  +--> SteamBbCodeGenerator.Generate
  |       |
  |       +--> Raw BBCode output
  |       |
  |       +--> SteamBbCodePreviewRenderer.Render
  |               |
  |               +--> Rendered HTML preview
  |
  +--> Debounced local-storage save
```

Generated BBCode, validation results, and rendered preview are derived from the current draft rather than stored as separate persistent state.

### Startup flow

```text
Application starts
  |
  +--> Theme is read and applied
  |
  +--> Saved draft is loaded from localStorage
          |
          +--> No saved draft: keep defaults
          |
          +--> Saved draft: restore state and normalize categories
```

### New-review flow

```text
User selects New review
  |
  v
Confirmation dialog
  |
  v
Clear saved draft
  |
  v
Create a new default ReviewDraft
  |
  v
Return to the Template step
```

## State and Persistence Boundaries

The application currently supports one locally stored draft.

Persistent state:

- Review draft JSON
- Theme preference

Transient state:

- Current workflow step
- Selected preview tab
- Copy-status message
- Save-status message
- Confirmation-dialog visibility

There is no cross-device synchronization. Clearing browser data removes the draft and saved theme. The application should not claim that local storage is a durable backup.

Future storage format changes should either remain backward compatible or introduce a new versioned key with explicit migration behavior.

## Privacy and Security

The current client-only model provides a narrow data boundary:

- Review content is not sent to an application server
- There are no credentials or accounts
- Draft data is stored in the user's browser
- Preview input is HTML encoded before rendering

Contributors should preserve these properties unless a future feature intentionally introduces a server component. Any cloud storage, sharing, analytics, or game-lookup integration must document what data leaves the browser and obtain appropriate user consent.

Generated BBCode is intended for the user to inspect before pasting into Steam. Validation and previewing reduce mistakes but do not guarantee that Steam will render every character or future tag behavior identically.

## Error Handling

Browser storage and clipboard operations can fail because of browser permissions, privacy modes, unavailable APIs, or storage restrictions.

The UI currently handles these failures without terminating the review session:

- Storage failures display an unavailable status
- Clipboard failures leave the raw BBCode available for manual selection
- Missing drafts fall back to the default in-memory draft

Service and UI changes should continue to fail gracefully when optional browser capabilities are unavailable.

## Deployment Model

Blazor WebAssembly compiles into static files that can be hosted by a static-site provider or conventional web server.

A deployment must:

- Serve the generated `wwwroot` assets
- Preserve the configured application base path
- Return the application entry point for client-side routes when additional routes are introduced
- Use HTTPS for reliable Clipboard API behavior in production

No server-side .NET runtime is required after publishing the current application.

## Testing Strategy

Automated tests have not yet been added. The architecture separates several deterministic services specifically so they can be tested without rendering the Blazor UI.

Recommended test layers:

1. Unit tests for `SteamBbCodeGenerator`
2. Unit tests for `ReviewDraftValidator`
3. Unit tests for `ReviewTemplateService`
4. Unit tests for `SteamBbCodePreviewRenderer`, including HTML encoding
5. Serialization tests for stored drafts
6. Blazor component tests for workflow and validation behavior
7. Browser tests for storage, clipboard fallback, responsive behavior, and keyboard navigation

Critical output tests should use representative drafts for every template and display format.

## Current Architectural Constraints

The current design is intentionally small, but several constraints should guide future changes:

- `Home.razor` contains substantial UI orchestration and should be decomposed as features grow
- Only one draft can be stored at a time
- Local storage is synchronous behind the JavaScript bridge and is not intended for large datasets
- The preview renderer supports only the generated BBCode subset
- No migration layer currently exists for saved draft schema changes
- There is no server boundary for shared links, synchronization, or remote metadata
- Core behavior does not yet have automated test coverage

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

## Potential Future Evolution

The current architecture can support several planned features without a server:

- JSON import and export
- Multiple drafts stored under separate local keys
- Custom local templates
- Markdown and plain-text export
- Additional BBCode layouts

Features such as cloud synchronization, public share links, accounts, or centrally managed community templates would introduce a backend boundary. Those changes should be treated as a separate architectural phase with explicit API, storage, authentication, privacy, and deployment decisions.
