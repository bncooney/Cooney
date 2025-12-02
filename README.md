# Cooney

> [!WARNING]
> This library is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A collection of .NET Standard 2.1 libraries for geospatial calculations, mathematical utilities, and AI image generation.

## Libraries

### [Cooney.Common](Cooney.Common)
Fundamental mathematical types and utilities including double-precision 3D vectors (`Double3`), 3x3 matrices (`Double3x3`), and matrix operations.

### [Cooney.Geospatial](Cooney.Geospatial)
Coordinate transformations between geographic coordinates (Lat/Lon/Alt) and local Cartesian systems. Based on the WGS84 ellipsoid model with ECEF and ENU coordinate system support.

### [Cooney.AI](Cooney.AI)
ComfyUI API client implementing Microsoft.Extensions.AI abstractions for image generation. Includes workflow management and source generation for strongly-typed workflows.

See individual library READMEs for detailed documentation and examples.
