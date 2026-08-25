# Changelog

All notable changes to Steam Review Forge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Guided five-step workflow for building Steam reviews
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
- Persistent raw BBCode output and copy action
- Built-in Steam formatting reference
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
- Updated review fields to save changes automatically
- Kept BBCode output and copying available throughout the workflow while surfacing validation guidance
- Improved editable and final review previews to more closely match Steam's presentation
- Kept recommendation, playtime, and product-received-free values as preview-only Steam metadata rather than generated BBCode

### Fixed

- Removed duplicate preview declarations
- Corrected draft persistence behavior
- Improved handling when browser draft storage is unavailable
- Added normalization and backward-compatible restoration for saved drafts created before configurable table columns and rating systems

### Security

- Review drafts remain stored locally in the user's browser
- No account, remote database, or server-side review storage is currently used

<!-- Example release:

## [0.1.0] - YYYY-MM-DD

### Added

- Initial usable release of Steam Review Forge.

-->
