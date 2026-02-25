# Cooney

> [!WARNING]
> This repository is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A .NET 10 monorepo containing reusable libraries and WPF desktop applications for geospatial math, AI tooling, and visualization.

## Libraries

All libraries target .NET Standard 2.0 and are configured for NuGet publishing. See individual READMEs for detailed documentation and examples.

### [Cooney.Common](Cooney.Common)
Fundamental mathematical types and utilities including double-precision 3D vectors (`Double3`), 3x3 matrices (`Double3x3`), and matrix operations.

### [Cooney.Geospatial](Cooney.Geospatial)
Coordinate transformations between geographic coordinates (Lat/Lon/Alt) and local Cartesian systems. Based on the WGS84 ellipsoid model with ECEF and ENU coordinate system support.

### [Cooney.AI](Cooney.AI)
AI tooling and abstractions built on Microsoft.Extensions.AI. Provides a `ChatService` backed by OpenAI-compatible endpoints and pre-built AI function tools (Calculator, ReadFile, WriteFile, DeleteFile, SearchReplace, WordCount, Todo).

## Applications

### [DevChat](DevChat)
WPF AI chat client with conversation persistence via Entity Framework Core (SQLite), markdown rendering (Markdig), and MVVM architecture (CommunityToolkit.Mvvm). Connects to a configurable OpenAI-compatible endpoint.

### [DevMap](DevMap)
WPF 3D globe viewer built on MonoGame and SharpDX Direct3D9 interop. Features an async tile loading pipeline with LRU caching (memory + disk) and OpenStreetMap tile fetching.

## Building

Requires [.NET 10 SDK](https://dotnet.microsoft.com/download). DevMap and DevChat target `net10.0-windows`.

```bash
dotnet build                        # Build entire solution
dotnet test                         # Run all tests
dotnet run --project DevChat        # Run DevChat
dotnet run --project DevMap         # Run DevMap
```

## License

[MIT](LICENSE.md)
