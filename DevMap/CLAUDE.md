# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Run

```bash
dotnet build                    # Build the project
dotnet run --project DevMap  # Run the application
```

No test framework is configured. Content pipeline uses MonoGame Content Builder (`Content/Content.mgcb`, currently empty).

## Architecture

WPF application hosting a MonoGame 3D globe renderer via Direct3D 9 interop. Single project targeting `net10.0-windows`.

### WPF ↔ MonoGame Integration (`MonoGameControls/`)

Adapted from [MonoGame.WpfCore](https://github.com/craftworkgames/MonoGame.WpfCore), implemented locally. The integration works by:

1. **`MonoGameContentControl`** — WPF `ContentControl` that renders MonoGame output via `D3DImage`. Drives the game loop through `CompositionTarget.Rendering` (not MonoGame's `Game.Run()`). Forwards WPF mouse events to the bound ViewModel.
2. **`MonoGameGraphicsDeviceService`** — Creates both a SharpDX Direct3D9 `DeviceEx` (required by WPF's `D3DImage`) and a MonoGame `GraphicsDevice`. Shares surfaces between them via shared handles.
3. **`MonoGameViewModel`** — Base class replacing `Game`. Provides `GraphicsDevice`, `ContentManager`, and lifecycle methods (`Initialize`, `LoadContent`, `Update`, `Draw`). Uses MVVM pattern with `INotifyPropertyChanged`.

Entry point: `App.xaml` → `MainWindow.xaml` → `MonoGameContentControl` with `GlobeViewModel` as DataContext.

### Application Logic

- **`GlobeViewModel`** — Main game logic. Creates `ArcballCamera` and `GlobeRenderer`, routes WPF mouse events to camera.
- **`ArcballCamera`** — Event-driven orbital camera (not polling-based). Call `HandleMouseDown`/`HandleMouseMove`/`HandleMouseUp`/`HandleMouseWheel`, then `UpdateMatrices` each frame.
- **`GlobeRenderer`** — Renders a `SphereMesh` with `BasicEffect`. Delegates tile management to `TileManager`.

### Tile System (`Tiles/`)

Async tile loading pipeline: `TileManager` → `TileFetcher` (HTTP from OpenStreetMap, 2 concurrent) → `TileCache` (memory LRU + disk at `%LOCALAPPDATA%/DevMap/tilecache`) → `TileAtlas` (GPU texture, max 4 blits/frame). Zoom level auto-selected from camera distance via `MercatorProjection`.

## Key Dependencies

- `MonoGame.Framework.WindowsDX` 3.8.x — DirectX backend (required for D3D9 interop; OpenGL won't work)
- `SharpDX` + `SharpDX.Direct3D9` 4.2.0 — D3DImage surface sharing
