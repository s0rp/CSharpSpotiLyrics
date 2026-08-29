# CSharpSpotiLyrics - AI Assistant Guidelines
Github:https://github.com/s0rp/CSharpSpotiLyrics
NuGet:https://www.nuget.org/packages/CSharpSpotiLyrics/

## Commands
- Build project: `dotnet build`
- Run unit tests: `dotnet test`
- Format code: `dotnet format`

## Technology Stack
- Target Frameworks: .NET Standard 2.0  (Lowest) Or .NET 8.0
- Dependencies: Minimal (System.Text.Json, System.Net.Http.Json). Absolutely NO heavy headless browsers (no Playwright/Selenium).

## Code Style & Conventions
- **Language**: C# 12 features on .NET 8 targets, C# 8 features on .NET Standard 2.0.
- **Naming**: PascalCase for public APIs, camelCase with underscore prefix (`_camelCase`) for private fields.
- **JSON Handling**: Always parse JSON directly from Response Streams (e.g., `ReadAsStreamAsync()`) to prevent unnecessary memory allocations.
- **Error Handling**: Throw specific custom exceptions defined in `CSharpSpotiLyrics.Core.Exceptions` rather than generic `Exception`.
- **Parallel Requests**: Always throttle concurrent operations with `SemaphoreSlim` (default limit 10).