#+ Repository Guidelines

## Project Structure & Module Organization
- Root solution: `PngMetadataReaderUI.sln`
- App project: `PngMetadataReaderUI/`
  - Views: `Views/` (XAML + code-behind)
  - ViewModels: `ViewModels/` (MVVM via CommunityToolkit)
  - Models & Helpers: `Models/`, `Helpers/` (extend here as needed)
  - Assets: `Assets/` (icons, resources)
  - Samples: `Sample/` (example PNG/outputs)
  - Publish profiles: `Properties/PublishProfiles/`

## Build, Test, and Development Commands
- Build: `dotnet build`
  - Restores packages and compiles for the current platform.
- Run (desktop): `dotnet run --project PngMetadataReaderUI`
  - Launches the Avalonia app locally.
- Publish (Release): `dotnet publish PngMetadataReaderUI -c Release -r win-x64`
  - Produces binaries under `PngMetadataReaderUI/bin/Release/...` (adjust `-r` for your OS).

## Coding Style & Naming Conventions
- Language: C# (.NET 9), nullable enabled.
- Indentation: 4 spaces, file-scoped namespaces preferred.
- MVVM: Use `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`).
- Naming: `PascalCase` for types/members; `camelCase` for locals; private fields with leading underscore (e.g., `_image`).
- XAML: Keep views thin; bind to ViewModels; prefer compiled bindings (enabled via project settings).

## Testing Guidelines
- No test project is included yet. When adding tests, create `PngMetadataReaderUI.Tests` (xUnit or MSTest) and wire to the solution.
- Run tests: `dotnet test`
- Aim for coverage on ViewModels, helpers, and logic that processes PNG metadata.

## Commit & Pull Request Guidelines
- Commits: short, imperative summaries (e.g., "Add drag-drop validation"). Use a scope prefix when helpful (e.g., `UI:` `Build:` `Docs:`).
- PRs: include a clear description, linked issue(s), and screenshots/GIFs for UI changes. Note platform(s) tested (Windows/Linux/macOS).
- Keep changes focused; update samples or docs when behavior/user flow changes.

## Security & Configuration Tips
- Only process local PNG files; validate paths and extensions.
- Do not commit secrets or machine-specific files. Respect `.gitignore`.
- Large binaries: place in `Sample/` only if essential; otherwise link in the PR.

