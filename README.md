# Steam Review Forge

Steam Review Forge is a browser-based tool for building structured Steam reviews and exporting them as Steam-compatible BBCode.

Instead of manually formatting headings, rating tables, sections, and recommendations, users complete a guided review workflow and copy the finished BBCode into Steam.

## Project Status

Steam Review Forge is currently under active development.

The core review-building workflow is functional, but the project has not yet reached a stable `1.0.0` release.

## Features

- Guided five-step review builder with a required recommendation setup
- Multiple starter templates:
  - Balanced Review
  - Quick Take
  - Deep Dive
  - Full Custom
- Multiple review layouts:
  - Rating Table
  - Individual Sections
  - Checklist
  - Minimal Verdict
- Customizable, reorderable table rows and columns
- Custom text columns with editable per-row values
- Drag-and-drop and keyboard-accessible reorder controls
- Rating systems:
  - 1–10 numbers
  - 1–5 numbers
  - 1–5 stars
  - Customizable text ratings
- Guided strengths, weaknesses, and final-thought prompts
- Centered Steam-style editor with inline category and response editing
- Persistent Steam BBCode preview and copy action
- Built-in Steam formatting reference
- Actionable review validation with errors, recommendations, and field navigation
- One-click BBCode clipboard export
- Automatic local draft saving
- Main Blue branding with dark and light modes
- Responsive desktop and mobile editing layout

## How It Works

1. Choose Recommended or Not Recommended and enter a short summary.
2. Optionally add preview-only playtime and product-received-free metadata.
3. Enter the review title and select a review template.
4. Choose a display format and edit categories directly in the Steam preview.
5. Write the guided responses directly in the Steam preview.
6. Inspect the finalized, read-only Steam Review Preview.
7. Copy the continuously available BBCode and paste it into Steam.

Drafts are stored locally in the browser. Steam Review Forge does not currently require an account or server-side storage.

## Technology

- C#
- .NET 10
- Blazor WebAssembly
- HTML and CSS
- Browser local storage
- JavaScript interoperability

## Getting Started

### Prerequisites

Install the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).

### Clone the Repository

```bash
git clone https://github.com/ArgyrosDev/steam-review-forge.git
cd steam-review-forge
```

### Run the Application

```bash
dotnet restore
dotnet run --project src/SteamReviewForge/SteamReviewForge.csproj
```

Open the local URL displayed in the terminal.

### Build the Project

```bash
dotnet build
```

## Project Structure

```text
steam-review-forge/
├── src/
│   └── SteamReviewForge/
│       ├── Models/       # Review data and validation models
│       ├── Pages/        # Application pages and review builder
│       ├── Services/     # Templates, validation, storage, and BBCode generation
│       └── wwwroot/      # Static browser assets
├── CHANGELOG.md
├── ROADMAP.md
└── SteamReviewForge.slnx
```

## Privacy

Review drafts are saved to the browser's local storage.

Draft content is not currently sent to a remote server. Clearing browser storage or starting a new review removes the locally saved draft.

## Roadmap

See [`ROADMAP.md`](ROADMAP.md) for planned releases and future improvements.

## Contributing

Bug reports and feature requests are welcome through GitHub Issues.

Pull requests should:

- Focus on a clearly defined change
- Avoid unrelated formatting changes
- Preserve the existing project structure
- Include an explanation of how the change was tested
- Successfully run `dotnet build`

## License

This project does not currently declare a license.

Until a license is added, the source code remains copyrighted and is not automatically available for redistribution or reuse. See [`docs/licensing.md`](docs/licensing.md) for additional information.

## Disclaimer

Steam Review Forge is an unofficial community project.

It is not affiliated with, endorsed by, or sponsored by Valve Corporation or Steam.
