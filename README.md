# Steam Review Forge

Steam Review Forge is a browser-based editor for building Steam reviews and exporting Steam-compatible BBCode. It supports guided workflows, freeform editing, reusable templates, and a Steam-style preview.

## Live Demo

Try Steam Review Forge at [argyrosforge.github.io/steam-review-forge](https://argyrosforge.github.io/steam-review-forge/).

## Project Status

`v0.1.0` is available as a public pre-release. The project remains under active development and has not reached stable `1.0.0` status.

## Features

- Guided and unguided Structured and BBCode editing modes
- Balanced, Quick Take, Deep Dive, and Full Custom starter templates
- Rating Table, Individual Sections, Checklist, and Minimal Verdict layouts
- Numeric, star, and customizable text rating systems
- Editable and reorderable tables, categories, and independent review components
- Click, drag-and-drop, keyboard, and touch-friendly editing controls
- Raw BBCode editing with templates, preview, history, and formatting help
- Actionable validation, automatic local draft saving, and one-click BBCode copy
- Responsive Steam-style preview with Main Blue, Nord, and Catppuccin themes

## How It Works

Choose Structured or BBCode editing, then choose Guided or Unguided:

- Guided Structured walks through setup, template, format, writing, and final preview.
- Unguided Structured exposes the complete structured editor without step gates.
- Guided BBCode combines required metadata and a starting template before opening the composer and final preview.
- Unguided BBCode opens the raw editor and live preview immediately.

All four modes produce Steam-compatible BBCode. Switching between Guided and Unguided preserves content; switching between Structured and BBCode starts a fresh review because freeform BBCode cannot be converted reliably into structured fields.

Drafts are stored locally in the browser; no account or server-side review storage is required.

## Privacy

Review drafts and their contents are stored locally in browser storage and are not sent to an application server. The hosted site uses GoatCounter for aggregate page-view analytics; review draft content is not included in those analytics.

Clearing browser storage or starting a new review removes the locally saved draft.

## Testing

Automated tests live in `tests/SteamReviewForge.Tests` and can be run locally with:

```bash
dotnet test
```

The GitHub Actions test workflow is manual-only and runs only when explicitly started from the repository's **Actions** tab.

## Roadmap

See [`ROADMAP.md`](ROADMAP.md) for planned releases and future improvements.

## Contributing

- [Report a bug](https://github.com/ArgyrosForge/steam-review-forge/issues/new?template=bug-report.yml)
- [Request a feature](https://github.com/ArgyrosForge/steam-review-forge/issues/new?template=feature-request.yml)

## License

Steam Review Forge is licensed under the **GNU Affero General Public License v3.0 only** (`AGPL-3.0-only`).

You may use, modify, and redistribute the project under the terms of that license. Modified versions offered to users over a network must also make their corresponding source code available as required by the AGPLv3.

See [`LICENSE`](LICENSE) for the full license text and [`docs/licensing.md`](docs/licensing.md) for project-specific licensing notes.

## Disclaimer

Steam Review Forge is an unofficial community project.

It is not affiliated with, endorsed by, or sponsored by Valve Corporation or Steam.
