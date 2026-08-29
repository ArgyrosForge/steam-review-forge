# Steam Review Forge

Steam Review Forge is a browser-based editor for building Steam reviews and exporting Steam-compatible BBCode. It supports guided workflows, freeform editing, reusable templates, and a Steam-style preview.

## Project Status

The core review workflow is functional, but the project remains under active development and has not reached `1.0.0`.

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

Drafts are stored locally in the browser; no account or server-side storage is required.

## Privacy

Draft content is not sent to a remote server. Clearing browser storage or starting a new review removes the locally saved draft.

## Roadmap

See [`ROADMAP.md`](ROADMAP.md) for planned releases and future improvements.

## Contributing

- [Report a bug](https://github.com/ArgyrosForge/steam-review-forge/issues/new?template=bug-report.yml)
- [Request a feature](https://github.com/ArgyrosForge/steam-review-forge/issues/new?template=feature-request.yml)

## License

This project does not currently declare a license.

Until a license is added, the source code remains copyrighted and is not automatically available for redistribution or reuse. See [`docs/licensing.md`](docs/licensing.md) for additional information.

## Disclaimer

Steam Review Forge is an unofficial community project.

It is not affiliated with, endorsed by, or sponsored by Valve Corporation or Steam.
