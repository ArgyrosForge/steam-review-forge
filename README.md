# Steam Review Forge

Steam Review Forge is a browser-based editor for building Steam reviews and exporting Steam-compatible BBCode. It supports guided workflows, freeform editing, reusable templates, and a Steam-style preview.

## Live Demo

Try Steam Review Forge at [argyrosforge.github.io/steam-review-forge](https://argyrosforge.github.io/steam-review-forge/).

## Project Status

`v0.2.0` is the current pre-release line. It has not reached stable `1.0.0` status.

## Development and Maintenance

This project was **vibe coded with AI assistance**. It is a personal, best-effort tool and will receive **limited maintenance**. Bug fixes, new features, security updates, and responses to issues are not guaranteed. The source is available for anyone who wants to inspect, adapt, or maintain it under its license.

The supported focus is current **desktop Firefox and Chromium-based browsers**. Safari and mobile devices are not release targets. The preview approximates Steam's renderer: check the copied review in Steam before posting. Keep a separate copy of important drafts; browser storage is not a backup.

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

- Guided Structured walks through setup, template, format, and writing while keeping the final preview visible.
- Unguided Structured exposes the complete structured editor without step gates.
- Guided BBCode combines a recommendation and starting template before opening the composer with a live final preview.
- Unguided BBCode opens the raw editor and live preview immediately.

All four modes produce Steam-compatible BBCode. Switching between Guided and Unguided preserves content; switching between Structured and BBCode starts a fresh review because freeform BBCode cannot be converted reliably into structured fields.

Drafts are stored locally in the browser; no account or server-side review storage is required.

## Privacy

Review drafts and their contents are stored locally in browser storage and are not sent to an application server. The application includes no analytics or third-party scripts. GitHub Pages still serves the site and may process ordinary hosting request logs.

Clearing browser storage or starting a new review removes the locally saved draft.

## Testing

Unit tests live in `tests/SteamReviewForge.Tests` and can be run locally with:

```bash
dotnet test tests/SteamReviewForge.Tests/SteamReviewForge.Tests.csproj
```

Firefox and Chromium workflow tests live in `tests/SteamReviewForge.BrowserTests`. Run the **Run Tests** workflow manually from the repository's **Actions** tab before releasing. Tests do not run automatically for pull requests or block deployment. The manual workflow checks a published build with its production security policy. See [the manual release checklist](docs/manual-release-checklist.md) for local commands and Steam formatting checks.

## Roadmap

The roadmap records possible improvements, not commitments or a delivery schedule. See [`ROADMAP.md`](ROADMAP.md) for planned releases and future improvements.

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
