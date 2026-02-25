# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Test Commands

```bash
dotnet build                                    # Build entire solution (Cooney.slnx)
dotnet test                                     # Run all tests
dotnet test Cooney.AI.Test                      # Run AI library tests only
dotnet test Cooney.Geospatial.Test              # Run geospatial tests only
dotnet test DevChat.AutoTest.Unit               # Run DevChat unit tests only
dotnet test --filter "FullyQualifiedName~ClassName.MethodName"  # Run a single test
dotnet run --project DevChat                    # Run DevChat application
dotnet run --project DevMap                     # Run DevMap application
```

Build cleanup: `./Clean-BuildFolders.ps1` removes all bin/ and obj/ directories.

## SDK & Tooling

- .NET 10.0.101 SDK (global.json with `rollForward: latestFeature`)
- Solution version: 2.1.0 (Directory.Build.props)
- Modern `.slnx` solution format
- MSTest 4.x with class-level parallel execution
- Code style: tabs (width 4), Allman braces, CRLF — see .editorconfig

## Repository Structure

**Monorepo** with three NuGet-publishable libraries, two WPF desktop apps, and their test projects.

### Libraries (all .NET Standard 2.0, NuGet-packaged)

- **Cooney.Common** — `Double3` (3D vector), `Double3x3` (3x3 matrix), angle conversions
- **Cooney.Geospatial** — LLA ↔ ECEF ↔ ENU coordinate transformations, WGS84, Haversine. Depends on Cooney.Common
- **Cooney.AI** — Microsoft.Extensions.AI abstractions, `ChatService` (OpenAI provider), AI function tools (Calculator, ReadFile, WriteFile, DeleteFile, SearchReplace, WordCount, Todo). Depends on Cooney.Common

### Applications (WPF, net10.0-windows)

- **DevChat** — AI chat client with EF Core SQLite persistence, MVVM via CommunityToolkit.Mvvm, Markdig markdown rendering. Supports `--test` CLI flag to swap in stub services for automation testing
- **DevMap** — 3D globe viewer using MonoGame + SharpDX Direct3D9 interop hosted in WPF. See [DevMap/CLAUDE.md](DevMap/CLAUDE.md) for detailed architecture

### Test Projects

- **Cooney.AI.Test** — Unit + integration tests (Unit/, Integration/ subdirs)
- **Cooney.Geospatial.Test** — Haversine tests
- **DevChat.AutoTest.Unit** — ChatViewModel persistence tests
- **DevChat.AutoTest** / **DevMap.AutoTest** — Appium UI automation tests (require running app)

## Architecture Patterns

### Dependency Injection & Hosting
All apps use `Host.CreateDefaultBuilder()` with `Microsoft.Extensions.DependencyInjection`. Services, ViewModels, and DbContexts are registered in `App.xaml.cs`. Configuration loaded from `appsettings.json` via `IConfiguration`.

### MVVM
CommunityToolkit.MVVM throughout DevChat. DevMap uses a custom `IMonoGameViewModel` base that replaces MonoGame's `Game` class with MVVM lifecycle methods.

### Persistence (DevChat)
Entity Framework Core with SQLite. Two separate DbContexts (`ChatDbContext`, `TodoDbContext`). Auto-creates database on startup via `EnsureCreatedAsync()`. Database stored at `%APPDATA%/DevChat/`.

### AI Tool Pattern (Cooney.AI)
New tools inherit from the tool base class and integrate via `AIFunctionFactory.Create(tool)` in DI registration. Tools are added to `ChatOptions.Tools`.

### Test Mode (DevChat)
The `--test` argument swaps real services for stubs (`StubChatService`, `StubTodoService`) and creates a temp database at `%TEMP%/DevChat.AutoTest/{GUID}`.
