# Steam Review Forge

Steam Review Forge is a browser-based tool for building structured Steam reviews and exporting them as Steam-compatible BBCode.

Instead of manually formatting headings, rating tables, sections, and recommendations, users complete a guided review workflow and copy the finished BBCode into Steam.

## Project Status

Steam Review Forge is currently under active development.

The core review-building workflow is functional, but the project has not yet reached a stable `1.0.0` release.

## Features

- Guided five-step review builder
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
- Customizable review categories and ratings
- Guided strengths, weaknesses, and final-thought prompts
- Live rendered review preview
- Raw Steam BBCode preview
- Review validation with errors and recommendations
- One-click BBCode clipboard export
- Automatic local draft saving
- Main Blue branding with dark and light modes
- Responsive browser layout

## How It Works

1. Select a review template.
2. Enter the review title, summary, recommendation, and playtime.
3. Choose a display format and configure category ratings.
4. Answer the guided review questions.
5. Review the generated output and copy the BBCode.
6. Paste the BBCode into a Steam review.

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
