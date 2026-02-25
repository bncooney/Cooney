# DevMap

A WPF desktop application that renders an interactive 3D globe with map tile overlays. Built with MonoGame for 3D rendering, hosted inside WPF via Direct3D 9 surface sharing.

## Building & Running

```bash
dotnet build DevMap
dotnet run --project DevMap
```

Requires .NET 10 SDK and Windows (targets `net10.0-windows`).

## Controls

- **Left-click + drag** — Rotate the globe
- **Mouse wheel** — Zoom in/out

## Architecture

### WPF ↔ MonoGame Integration (`MonoGameControls/`)

Adapted from [MonoGame.WpfCore](https://github.com/craftworkgames/MonoGame.WpfCore), implemented locally. WPF's `D3DImage` displays a shared Direct3D 9 surface that MonoGame renders into. The game loop is driven by `CompositionTarget.Rendering` rather than MonoGame's `Game.Run()`.

- **`MonoGameContentControl`** — WPF `ContentControl` that manages the render surface and forwards mouse events to the bound ViewModel.
- **`MonoGameGraphicsDeviceService`** — Creates a SharpDX Direct3D9 `DeviceEx` and a MonoGame `GraphicsDevice`, sharing surfaces between them via shared handles.
- **`MonoGameViewModel`** — Base class replacing MonoGame's `Game`. Provides `GraphicsDevice`, `ContentManager`, and lifecycle methods (`Initialize`, `LoadContent`, `Update`, `Draw`).

Entry point: `App.xaml` → `MainWindow.xaml` → `MonoGameContentControl` with `GlobeViewModel` as DataContext.

### Application Logic

- **`GlobeViewModel`** — Main game logic. Creates the camera and renderer, routes WPF mouse events to the camera.
- **`ArcballCamera`** — Event-driven orbital camera. Not polling-based; responds to `HandleMouseDown`/`HandleMouseMove`/`HandleMouseUp`/`HandleMouseWheel`.
- **`GlobeRenderer`** — Renders a `SphereMesh` with `BasicEffect` and delegates tile management to `TileManager`.

### Tile System (`Tiles/`)

Async pipeline that loads, caches, and uploads map tiles to the GPU:

```
TileManager → TileFetcher (HTTP, 2 concurrent requests to OpenStreetMap)
            → TileCache   (memory LRU + disk at %LOCALAPPDATA%/DevMap/tilecache)
            → TileAtlas   (GPU texture, max 4 blits per frame)
```

Zoom level is auto-selected from camera distance via `MercatorProjection`.

## Dependencies

| Package | Purpose |
|---|---|
| `MonoGame.Framework.WindowsDX` 3.8.x | DirectX rendering backend (required for D3D9 interop; OpenGL won't work) |
| `SharpDX` + `SharpDX.Direct3D9` 4.2.0 | D3DImage surface sharing between WPF and MonoGame |
