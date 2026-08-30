# Changelog

All notable changes to Steam Review Forge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Changed

- Made Structured template headings, dividers, rating-table headings, and rating-table cells directly editable or removable while retaining the persistent Final Preview
- Standardized the Final Preview width and body typography across every editing mode to match Steam's full reviews, and added Early Access Review as locally saved preview metadata

## [0.2.0] - 2026-08-29

This pre-release focuses on a desktop-first editing workspace, more flexible structured reviews, and stronger draft and release reliability.

### Added

- Dedicated xUnit test project under `tests/SteamReviewForge.Tests`
- Initial automated coverage for review validation behavior
- GitHub Actions test workflow
- GoatCounter aggregate page-view analytics for the hosted GitHub Pages site
- Live GitHub Pages demo link in the README
- Versioned draft persistence with automatic migration from v0.1 draft data
- Recovery dialog that preserves invalid or newer saved data for copying before reset
- Non-destructive Steam BBCode compatibility diagnostics with line and column guidance
- Automatic unit and browser test coverage for pull requests and deployments
- Firefox primary-workflow tests and Chromium smoke tests
- A persistent Final Preview beside both structured and BBCode editors
- A desktop-first, three-column workspace with workflow choices stacked in the left rail
- Empty-BBCode safeguards that keep copy actions disabled until review content exists

### Changed

- Updated repository documentation to reflect the published `v0.1.0` pre-release, current testing setup, and analytics behavior
- Expanded unit coverage across draft persistence, BBCode generation and preview, templates, validation, and playtime formatting
- Improved multiline preview rendering for code, no-parse, and quote blocks
- Allowed comma decimal input when normalizing playtime
- Required the complete test suite to pass before GitHub Pages deployment
- Updated the GitHub Pages pipeline to the Node 24-based action releases
- Made the editor the central focus while keeping setup controls compact and constraining the preview to a Steam-like width
- Simplified Deep Dive to six removable starter sections without fixed strengths, improvements, or final-thought blocks
- Made every structured preview block removable, including content supplied by starter templates
- Reduced Guided Structured to Setup, Template, Format, and Questions while keeping Final Preview visible throughout
- Removed Short Summary from guided Setup and from BBCode workflows; summaries remain optional structured content
- Featured Blank Document in BBCode template choices and condensed the remaining starter templates
- Kept Copy BBCode in the editor and removed the duplicate action from the BBCode Final Preview

### Fixed

- Preserved category identifiers when drafts are restored
- Normalized invalid, duplicate, and null saved-draft values without crashing the application
- Prevented invalid saved drafts from being mislabeled as unavailable storage or overwritten automatically
- Warned users before leaving while edits are awaiting persistence or storage has failed
- Prevented workflow labels and controls from spilling outside their desktop columns
- Reduced unnecessary desktop scrolling in setup, template, and composer views

## [0.1.0] - 2026-08-29

Initial public pre-release of Steam Review Forge. The core review-building workflow is usable, but the project remains under active development and has not reached stable `1.0.0` status.

### Added

- Guided five-step workflow for building Steam reviews
- Guided and unguided workflows for both structured and freeform BBCode editing
- Cursor-aware templates for every option in Steam's Recommendation formatting help, including all headings, inline styles, lists, table variants, quotes, code, links, and embeddable URLs
- Independent drag-and-drop structured review components for ratings, bulleted strengths or improvements, and text sections, with click and keyboard alternatives
- Required recommendation setup and review fields for title, summary, playtime, and product-received-free metadata
- Balanced Review, Quick Take, Deep Dive, and Full Custom templates
- Rating Table, Individual Sections, Checklist, and Minimal Verdict display formats
- Customizable, reorderable review categories and rating-table columns
- Custom text columns with editable per-category values
- 1–10, 1–5, 1–5 star, and customizable text rating systems
- Drag-and-drop and keyboard-accessible row and column ordering controls
- Guided prompts for strengths, weaknesses, and final thoughts
- Steam-compatible BBCode generation
- Centered Steam-style editing workspace with inline category and response editing
- Read-only final Steam review preview
- One-click structured-review copying and a dedicated raw editor in BBCode modes
- Built-in Steam formatting reference
- Live rendering for Steam BBCode headings, inline styles, lists, quotes, code, table variants, safe links, and embeddable URL previews
- One-click BBCode clipboard export
- Automatic browser-based draft saving
- Draft restoration when reopening the application
- Confirmation dialog for starting a new review
- Review validation with errors and warnings
- Validation status indicators throughout the review workflow
- Responsive application layout
- Main Blue branding with light and dark modes
- Unofficial-project disclaimer
- Project-specific README documentation

### Changed

- Replaced the initial repository-template README with documentation specific to Steam Review Forge
- Improved review-builder navigation with completion, warning, and error states
- Made validation summaries actionable with navigation and focus for the relevant editor field
- Improved mobile editing with compact workflow navigation, in-flow category controls, and viewport-sized output and table-editor panels
- Expanded the Steam preview in Structured modes by hiding the dedicated raw BBCode panel while retaining one-click copying
- Reordered BBCode modes around the raw editor, with BBCode in the center and its rendered Steam preview in the right column
- Simplified every guided Final Preview to the Steam preview and its Copy BBCode action, with no raw editor panel
- Constrained Final Preview to the common Steam store review width instead of stretching it across the editing workspace
- Added an Undo action to the guided BBCode composer for typing, pasting, and inserted formatting blocks
- Moved Undo into the BBCode editor header and added an undoable Clear Code action
- Reworked Guided Structured into progress, active choices, and editable Steam preview columns
- Updated review fields to save changes automatically
- Kept BBCode output and copying available throughout the workflow while surfacing validation guidance
- Improved editable and final review previews to more closely match Steam's presentation
- Kept recommendation, playtime, and product-received-free values as preview-only Steam metadata rather than generated BBCode

### Fixed

- Removed duplicate preview declarations
- Removed the inactive structured-component drop overlay artifact from the Steam preview
- Made palette drag-and-drop use native capture-phase browser events so drops reliably create components alongside click-to-add
- Assigned unique HTTP ports to the HTTP and HTTPS development profiles to prevent run-configuration collisions
- Corrected draft persistence behavior
- Improved handling when browser draft storage is unavailable
- Added normalization and backward-compatible restoration for saved drafts created before configurable table columns and rating systems

### Security

- Review drafts remain stored locally in the user's browser
- No account, remote database, or server-side review storage is currently used
