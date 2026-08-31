# Roadmap

Steam Review Forge uses [Semantic Versioning](https://semver.org/) with version numbers in the `MAJOR.MINOR.PATCH` format.

The roadmap represents the current direction of the project and may change as development continues.

## v0.1.0 — Core Review Builder

The first usable version of Steam Review Forge was published as a public pre-release on 2026-08-29.

### Completed

- [x] Create the guided five-step review workflow
- [x] Add Balanced, Quick Take, Deep Dive, and Full Custom templates
- [x] Add Rating Table, Individual Sections, Checklist, and Minimal Verdict layouts
- [x] Support customizable categories, ratings, and notes
- [x] Add guided strengths, weaknesses, and final-thought prompts
- [x] Generate Steam-compatible BBCode
- [x] Add rendered review and raw BBCode previews
- [x] Add clipboard export
- [x] Save and restore review drafts through browser local storage
- [x] Add review validation, warnings, and step status indicators
- [x] Add new-review confirmation and draft clearing
- [x] Add responsive styling and light and dark themes
- [x] Replace the initial repository documentation templates
- [x] Complete the initial changelog
- [x] Allow categories to be reordered
- [x] Improve validation messages and navigation to invalid fields
- [x] Improve mobile editing and preview behavior
- [x] Add guided and unguided structured and BBCode editing modes
- [x] Publish the first usable release

### Deferred beyond v0.1.0

- Perform a full accessibility and keyboard-navigation review
- Complete broader testing across major desktop and mobile browsers

## v0.2.0 — Reliability and Compatibility

Improve confidence in saved drafts, generated reviews, and release quality.

- [x] Add unit-test coverage for generation, templates, validation, formatting, preview rendering, BBCode diagnostics, and draft persistence
- [x] Add Firefox-first browser tests for the primary workflow and Chromium smoke coverage
- [x] Warn before leaving while a save is pending or browser storage is unavailable
- [x] Add versioned draft migration and recovery for invalid or newer saved drafts
- [x] Add non-destructive BBCode compatibility diagnostics and multiline preview support
- [x] Run tests automatically for pull requests and gate GitHub Pages deployment on them
- [x] Rework the desktop workspace around a focused editor with persistent final preview
- [x] Reduce setup and composer scrolling with compact, stacked workflow controls
- [x] Make structured template content removable and simplify the Deep Dive starter layout
- [x] Remove Short Summary from guided Setup and BBCode workflows

## v0.3.0 — Review Functionality and UX

Improve the experience of creating one review at a time and add more control over its format.

- [ ] Streamline navigation and reduce friction across the review workflows
- [ ] Refine editor and preview interactions for desktop and mobile
- [ ] Improve editing feedback and controls for mouse, keyboard, and touch input
- [ ] Add more built-in review templates
- [ ] Allow review sections to be enabled or disabled individually
- [ ] Add optional pros-and-cons sections
- [ ] Add spoiler formatting controls
- [ ] Add configurable headings and labels
- [ ] Add additional rating scales
- [ ] Add optional game metadata fields
- [ ] Add BBCode presets for short and long Steam reviews

## v1.0.0 — Stable Release

Prepare Steam Review Forge for dependable public use.

- [ ] Finalize the supported review formats
- [ ] Complete automated test coverage for critical workflows
- [ ] Finalize user and contributor documentation
- [ ] Resolve all known release-blocking defects
- [ ] Publish a stable hosted version
- [ ] Publish the first stable release

## Future Ideas

Ideas not currently assigned to a release:

- Localization
- Steam game lookup and metadata import
- Screenshot or image-assisted review planning
- Review statistics and rating summaries

## Completed Milestones

- `v0.1.0` — Core Review Builder public pre-release, published 2026-08-29
- `v0.2.0` — Reliability, compatibility, and desktop workspace pre-release, prepared 2026-08-29
