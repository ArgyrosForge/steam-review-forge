# Changelog

All notable changes to Steam Review Forge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

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
