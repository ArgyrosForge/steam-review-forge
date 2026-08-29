# Roadmap

Steam Review Forge uses [Semantic Versioning](https://semver.org/) with version numbers in the `MAJOR.MINOR.PATCH` format.

The roadmap represents the current direction of the project and may change as development continues.

## v0.1.0 — Core Review Builder

The first usable version of Steam Review Forge.

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

## v0.2.0 — Reliability and Compatibility

Improve confidence in saved drafts, generated reviews, and release quality.

- [x] Add unit-test coverage for generation, templates, validation, formatting, preview rendering, BBCode diagnostics, and draft persistence
- [x] Add Firefox-first browser tests for the primary workflow and Chromium smoke coverage
- [x] Warn before leaving while a save is pending or browser storage is unavailable
- [x] Add versioned draft migration and recovery for invalid or newer saved drafts
- [x] Add non-destructive BBCode compatibility diagnostics and multiline preview support
- [x] Run tests automatically for pull requests and gate GitHub Pages deployment on them

## v0.3.0 — Import, Export, and Sharing

Make reviews easier to back up, reuse, and move between browsers.

- [ ] Export a review draft as JSON
- [ ] Import a previously exported review draft
- [ ] Add plain-text and Markdown export options
- [ ] Add reusable custom templates
- [ ] Allow users to duplicate an existing draft
- [ ] Add a printable review preview
- [ ] Add optional shareable links without exposing private drafts by default

## v0.4.0 — Expanded Review Tools

Add more control for users who want advanced review formats.

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
- [ ] Complete accessibility and keyboard-navigation review
- [ ] Complete broad desktop and mobile browser compatibility testing
- [ ] Finalize user and contributor documentation
- [ ] Resolve all known release-blocking defects
- [ ] Publish a stable hosted version
- [ ] Publish the first stable release

## Future Ideas

Ideas not currently assigned to a release:

- Multiple locally saved reviews
- Optional cloud synchronization
- Community-created templates
- Localization
- Steam game lookup and metadata import
- Screenshot or image-assisted review planning
- Review statistics and rating summaries

## Completed Milestones

Completed releases and major milestones should be moved here after they are published.
