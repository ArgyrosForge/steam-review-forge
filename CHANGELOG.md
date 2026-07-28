# Changelog

All notable changes to Steam Review Forge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Guided five-step workflow for building Steam reviews
- Review setup fields for title, summary, recommendation, and playtime
- Balanced Review, Quick Take, Deep Dive, and Full Custom templates
- Rating Table, Individual Sections, Checklist, and Minimal Verdict display formats
- Customizable review categories, ratings, and notes
- Guided prompts for strengths, weaknesses, and final thoughts
- Steam-compatible BBCode generation
- Live rendered review preview
- Raw BBCode output view
- One-click BBCode clipboard export
- Automatic browser-based draft saving
- Draft restoration when reopening the application
- Confirmation dialog for starting a new review
- Review validation with errors and warnings
- Validation status indicators throughout the review workflow
- Responsive application layout
- Light and dark themes
- Unofficial-project disclaimer
- Project-specific README documentation

### Changed

- Replaced the initial repository-template README with documentation specific to Steam Review Forge
- Improved review-builder navigation with completion, warning, and error states
- Updated review fields to save changes automatically
- Disabled BBCode copying until required review fields are valid
- Improved the generated review preview and export workflow

### Fixed

- Removed duplicate preview declarations
- Corrected draft persistence behavior
- Improved handling when browser draft storage is unavailable

### Security

- Review drafts remain stored locally in the user's browser
- No account, remote database, or server-side review storage is currently used

<!-- Example release:

## [0.1.0] - YYYY-MM-DD

### Added

- Initial usable release of Steam Review Forge.

-->
