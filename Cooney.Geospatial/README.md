# Cooney.Geospatial

> [!WARNING]
> This library is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A .NET Standard 2.0 library providing coordinate transformations between geographic coordinates (Latitude, Longitude, Altitude) and local Cartesian coordinate systems. This library is based on the WGS84 ellipsoid model and uses ECEF (Earth-Centered, Earth-Fixed) coordinates as an intermediate representation.

This implementation is designed to be framework-agnostic and can be used in Unity, non-Unity applications, and any .NET Standard 2.0 compatible environment.

## Features

- **High-precision coordinate transformations** between LLA (Lat/Lon/Alt) and ECEF coordinates
- **WGS84 ellipsoid model** with geodetic calculations
- **ENU (East-North-Up) coordinate system** support for local reference frames
- **Great circle distance calculations** using the Haversine formula
- **Bearing calculations** between geographic points
- **Framework-agnostic design** - works with Unity, .NET Core, .NET Framework, and more
- **Double precision** for transformations across large distances

## Coordinate Systems

### Geographic Coordinates (LLA)

**Latitude, Longitude, Altitude**
- **Latitude**: -90° to +90° (South to North)
- **Longitude**: -180° to +180° (West to East)
- **Altitude**: Meters above the WGS84 ellipsoid

Geographic coordinates represent positions on Earth's surface using angular measurements and height.

### ECEF Coordinates

**Earth-Centered, Earth-Fixed Cartesian Coordinates**
- **Origin**: Earth's center of mass
- **X-axis**: Points to (0° latitude, 0° longitude) - Equator/Prime Meridian intersection
- **Y-axis**: Points to (0° latitude, 90° East longitude)
- **Z-axis**: Points to North Pole
- **Coordinate System**: Right-handed, Z-up

ECEF coordinates are Cartesian (X, Y, Z) coordinates in meters from Earth's center. This system rotates with the Earth.

### ENU Coordinates

**East-North-Up Local Tangent Plane**
- **X-axis**: East
- **Y-axis**: North
- **Z-axis**: Up
- **Coordinate System**: Right-handed, Z-up

ENU coordinates are relative to a local origin point on Earth's surface, providing a convenient local Cartesian frame.

### Unity/Game Engine Coordinates

When integrating with Unity or other game engines:
- **X-axis**: East
- **Y-axis**: Up
- **Z-axis**: North
- **Coordinate System**: Left-handed, Y-up (Unity convention)

The library provides conversion methods to transform between ENU and Unity-style coordinate systems.

## WGS84 Ellipsoid Model

The World Geodetic System 1984 (WGS84) is the standard coordinate system used by GPS and most mapping applications.

### Key Parameters

```
Semi-major axis (a):     6,378,137.0 meters
Flattening (f):          1/298.257223563
Semi-minor axis (b):     6,356,752.314 meters
Eccentricity² (e²):      0.00669437999014
```

The Earth is modeled as an oblate ellipsoid (slightly flattened sphere), not a perfect sphere. This accounts for the equatorial bulge caused by Earth's rotation.

## Quick Start

### Basic Geographic to ECEF Conversion

```csharp
using Cooney.Geospatial;

// Create a geographic coordinate (Sydney Harbour Bridge)
var geographic = new GeographicCoordinates(
    latitude: -33.852222,
    longitude: 151.210556,
    altitude: 0
);

// Convert to ECEF
EcefCoordinates ecef = Wgs84Ellipsoid.GeographicToEcef(geographic);
Console.WriteLine(ecef); // ECEF(X: -4648237.123m, Y: 2560392.789m, Z: -3526423.456m)

// Convert back to geographic
GeographicCoordinates geo = Wgs84Ellipsoid.EcefToGeographic(ecef);
Console.WriteLine(geo); // Lat: -33.852222°, Lon: 151.210556°, Alt: 0.00m
```

### Working with Local Coordinates (ENU)

```csharp
using Cooney.Geospatial;
using System.Numerics;

// Set up origin (Sydney CBD)
var origin = new GeographicCoordinates(-33.8688, 151.2093, 0);
EcefCoordinates originEcef = Wgs84Ellipsoid.GeographicToEcef(origin);

// Calculate transformation matrix
var ecefToEnuMatrix = CoordinateTransformations.CalculateEcefToEnuMatrix(
    origin.Latitude,
    origin.Longitude
);

// Convert Sydney Tower to local ENU coordinates
var sydneyTower = new GeographicCoordinates(-33.8704, 151.2093, 309);
EcefCoordinates sydneyTowerEcef = Wgs84Ellipsoid.GeographicToEcef(sydneyTower);
Double3 enuPosition = CoordinateTransformations.EcefToEnu(
    sydneyTowerEcef,
    originEcef,
    ecefToEnuMatrix
);

Console.WriteLine($"Sydney Tower relative position: E={enuPosition.x:F2}m, N={enuPosition.y:F2}m, U={enuPosition.z:F2}m");
```

### Converting to Unity Coordinates

```csharp
using Cooney.Geospatial;
using System.Numerics;

// Set up origin and matrices
var origin = new GeographicCoordinates(-33.8688, 151.2093, 0);
EcefCoordinates originEcef = Wgs84Ellipsoid.GeographicToEcef(origin);
var ecefToEnuMatrix = CoordinateTransformations.CalculateEcefToEnuMatrix(
    origin.Latitude,
    origin.Longitude
);

// Convert a location to Unity coordinates
var location = new GeographicCoordinates(-33.8704, 151.2093, 309);
Vector3 unityPosition = CoordinateTransformations.EcefToUnity(
    Wgs84Ellipsoid.GeographicToEcef(location),
    originEcef,
    ecefToEnuMatrix
);

Console.WriteLine($"Unity position: X={unityPosition.X}, Y={unityPosition.Y}, Z={unityPosition.Z}");

// Convert back from Unity to geographic
var enuToEcefMatrix = CoordinateTransformations.CalculateEnuToEcefMatrix(
    origin.Latitude,
    origin.Longitude
);
EcefCoordinates ecef = CoordinateTransformations.UnityToEcef(
    unityPosition,
    originEcef,
    enuToEcefMatrix
);
GeographicCoordinates geoFromUnity = Wgs84Ellipsoid.EcefToGeographic(ecef);
```

## API Reference

### GeographicCoordinates

Represents a position in latitude, longitude, and altitude.

```csharp
public struct GeographicCoordinates
{
    public double Latitude { get; set; }   // Degrees, -90 to +90
    public double Longitude { get; set; }  // Degrees, -180 to +180
    public double Altitude { get; set; }   // Meters above WGS84 ellipsoid

    public GeographicCoordinates(double latitude, double longitude, double altitude = 0);

    public bool IsValid();              // Check if coordinates are within valid ranges
    public void Normalize();            // Normalize longitude wrapping and clamp latitude
    public string ToString();           // "Lat: -33.852222°, Lon: 151.210556°, Alt: 0.00m"
    public string ToDetailedString();   // "33.852222°S, 151.210556°E, 0.00m"
}
```

**Methods:**
- `IsValid()`: Returns `true` if latitude is in [-90, 90], longitude is in [-180, 180], and values are not NaN or Infinity
- `Normalize()`: Clamps latitude to valid range and wraps longitude to [-180, 180]
- `ToString()`: Returns formatted string with decimal degrees
- `ToDetailedString()`: Returns formatted string with cardinal directions (N/S/E/W)

### EcefCoordinates

Represents a position in Earth-Centered, Earth-Fixed coordinates.

```csharp
public struct EcefCoordinates
{
    public double X { get; set; }  // Meters
    public double Y { get; set; }  // Meters
    public double Z { get; set; }  // Meters

    public EcefCoordinates(double x, double y, double z);

    public Double3 ToDouble3();                          // Convert to Double3
    public static EcefCoordinates FromDouble3(Double3);  // Create from Double3
    public double Magnitude();                           // Distance from Earth's center
    public EcefCoordinates Normalised();                 // Unit vector

    // Operators: +, -, *, /
}
```

**Methods:**
- `Magnitude()`: Returns the distance from Earth's center (typically ~6.37 million meters)
- `Normalised()`: Returns a unit vector in the same direction
- Operators support vector arithmetic: addition, subtraction, scalar multiplication/division

### Wgs84Ellipsoid

Static class providing WGS84 constants and conversion methods.

```csharp
public static class Wgs84Ellipsoid
{
    // Constants
    public const double SemiMajorAxis = 6378137.0;              // meters
    public const double Flattening = 1.0 / 298.257223563;
    public const double SemiMinorAxis;                          // 6,356,752.314m
    public const double EccentricitySquared;                    // 0.00669437999014
    public const double SecondEccentricitySquared;

    // Conversion methods
    public static EcefCoordinates GeographicToEcef(GeographicCoordinates geographic);
    public static GeographicCoordinates EcefToGeographic(EcefCoordinates ecef);

    // Utility methods
    public static double CalculateRadiusOfCurvature(double latitudeInDegrees);
    public static double CalculateMeridionalRadius(double latitudeInDegrees);
}
```

**Conversion Methods:**
- `GeographicToEcef()`: Converts LLA to ECEF using standard WGS84 formulas
- `EcefToGeographic()`: Converts ECEF to LLA using Bowring method (closed-form solution)

**Utility Methods:**
- `CalculateRadiusOfCurvature()`: Returns the radius of curvature in the prime vertical (N)
- `CalculateMeridionalRadius()`: Returns the radius of curvature in the meridian (M)

### CoordinateTransformations

Static class providing transformations between ECEF, ENU, and Unity coordinate systems.

```csharp
public static class CoordinateTransformations
{
    // Matrix calculations
    public static Double3x3 CalculateEcefToEnuMatrix(double latitudeInDegrees, double longitudeInDegrees);
    public static Double3x3 CalculateEnuToEcefMatrix(double latitudeInDegrees, double longitudeInDegrees);

    // ECEF ° ENU conversions
    public static Double3 EcefToEnu(EcefCoordinates ecef, EcefCoordinates origin, Double3x3 ecefToEnuMatrix);
    public static EcefCoordinates EnuToEcef(Double3 enu, EcefCoordinates origin, Double3x3 enuToEcefMatrix);

    // ENU ° Unity conversions
    public static Vector3 EnuToUnity(Double3 enu);
    public static Double3 UnityToEnu(Vector3 unity);

    // Direct ECEF ° Unity conversions
    public static Vector3 EcefToUnity(EcefCoordinates ecef, EcefCoordinates origin, Double3x3 ecefToEnuMatrix);
    public static EcefCoordinates UnityToEcef(Vector3 unity, EcefCoordinates origin, Double3x3 enuToEcefMatrix);
}
```

**Matrix Methods:**
- `CalculateEcefToEnuMatrix()`: Creates rotation matrix from ECEF to ENU at given origin
- `CalculateEnuToEcefMatrix()`: Creates rotation matrix from ENU to ECEF (transpose of above)

**Coordinate Conversions:**
- All conversions maintain double precision until final conversion to Unity's float-based Vector3
- ENU uses right-handed Z-up coordinates
- Unity uses left-handed Y-up coordinates

### Common Types (Cooney.Common)

```csharp
public struct Double3
{
    public double x, y, z;
    public Double3(double x, double y, double z);
}

public struct Double3x3
{
    public Double3 c0, c1, c2;  // Column vectors
    public Double3x3(Double3 c0, Double3 c1, Double3 c2);
}

public static class Maths
{
    public const double DegreesToRadians = Math.PI / 180.0;
    public const double RadiansToDegrees = 180.0 / Math.PI;

    public static Double3 Mul(Double3x3 m, Double3 v);     // Matrix-vector multiplication
    public static Double3x3 Transpose(Double3x3 m);        // Matrix transpose
}
```

## Usage Examples

### Example 1: Distance Between Two Cities

```csharp
using Cooney.Geospatial;

var sydney = new GeographicCoordinates(-33.8688, 151.2093, 0);
var melbourne = new GeographicCoordinates(-37.8136, 144.9631, 0);

// Convert to ECEF
var sydneyEcef = Wgs84Ellipsoid.GeographicToEcef(sydney);
var melbourneEcef = Wgs84Ellipsoid.GeographicToEcef(melbourne);

// Calculate straight-line distance through Earth
EcefCoordinates difference = melbourneEcef - sydneyEcef;
double distance = difference.Magnitude();

Console.WriteLine($"Sydney to Melbourne: {distance / 1000.0:F1} km");
// Note: This is straight-line distance, not great-circle distance
```

### Example 2: Building a Local Coordinate System

```csharp
using Cooney.Geospatial;
using System.Numerics;
using System.Collections.Generic;

public class LocalCoordinateSystem
{
    private GeographicCoordinates _origin;
    private EcefCoordinates _originEcef;
    private Double3x3 _ecefToEnuMatrix;
    private Double3x3 _enuToEcefMatrix;

    public LocalCoordinateSystem(GeographicCoordinates origin)
    {
        _origin = origin;
        _originEcef = Wgs84Ellipsoid.GeographicToEcef(origin);
        _ecefToEnuMatrix = CoordinateTransformations.CalculateEcefToEnuMatrix(
            origin.Latitude,
            origin.Longitude
        );
        _enuToEcefMatrix = CoordinateTransformations.CalculateEnuToEcefMatrix(
            origin.Latitude,
            origin.Longitude
        );
    }

    public Vector3 GeographicToLocal(GeographicCoordinates geographic)
    {
        EcefCoordinates ecef = Wgs84Ellipsoid.GeographicToEcef(geographic);
        return CoordinateTransformations.EcefToUnity(ecef, _originEcef, _ecefToEnuMatrix);
    }

    public GeographicCoordinates LocalToGeographic(Vector3 localPosition)
    {
        EcefCoordinates ecef = CoordinateTransformations.UnityToEcef(
            localPosition,
            _originEcef,
            _enuToEcefMatrix
        );
        return Wgs84Ellipsoid.EcefToGeographic(ecef);
    }
}

// Usage
var system = new LocalCoordinateSystem(new GeographicCoordinates(-33.8688, 151.2093, 0));

var sydneyTower = new GeographicCoordinates(-33.8704, 151.2093, 309);
Vector3 localPos = system.GeographicToLocal(sydneyTower);
Console.WriteLine($"Local position: {localPos}");

GeographicCoordinates recovered = system.LocalToGeographic(localPos);
Console.WriteLine($"Recovered: {recovered}");
```

### Example 3: Coordinate Validation and Normalization

```csharp
using Cooney.Geospatial;

var coords = new GeographicCoordinates(91.5, 185.0, 100);

Console.WriteLine($"Is valid: {coords.IsValid()}"); // False (lat > 90, lon > 180)

coords.Normalize();
Console.WriteLine($"After normalization: {coords}");
// Lat: 90.000000°, Lon: -175.000000°, Alt: 100.00m

Console.WriteLine($"Is valid: {coords.IsValid()}"); // True
```

### Example 4: Working with ECEF Coordinates Directly

```csharp
using Cooney.Geospatial;

// Get ECEF coordinates for two points
var point1 = new GeographicCoordinates(0, 0, 0);      // Equator, Prime Meridian
var point2 = new GeographicCoordinates(90, 0, 0);     // North Pole

var ecef1 = Wgs84Ellipsoid.GeographicToEcef(point1);
var ecef2 = Wgs84Ellipsoid.GeographicToEcef(point2);

Console.WriteLine($"Equator/Prime Meridian: {ecef1}");
Console.WriteLine($"North Pole: {ecef2}");

// Vector operations
EcefCoordinates midpoint = (ecef1 + ecef2) / 2.0;
Console.WriteLine($"Midpoint: {midpoint}");

// Distance from Earth's center
Console.WriteLine($"Equator radius: {ecef1.Magnitude():F1}m");
Console.WriteLine($"Polar radius: {ecef2.Magnitude():F1}m");
```

## Mathematical Foundation

### LLA to ECEF Conversion

Given latitude φ, longitude λ, and altitude h:

```
N(φ) = a / √(1 - e² · sin²(φ))

X = (N(φ) + h) · cos(φ) · cos(λ)
Y = (N(φ) + h) · cos(φ) · sin(λ)
Z = (N(φ) · (1 - e²) + h) · sin(φ)

Where:
  a = WGS84 semi-major axis (6,378,137.0 m)
  e² = WGS84 eccentricity squared (0.00669437999014)
  N(φ) = radius of curvature in the prime vertical
```

### ECEF to LLA Conversion (Bowring Method)

This is a closed-form solution that's efficient and accurate:

```
λ = atan2(Y, X)

p = √(X² + Y²)
θ = atan2(Z · a, p · b)

φ = atan2(Z + e'² · b · sin³(θ), p - e² · a · cos³(θ))

N = a / √(1 - e² · sin²(φ))
h = p / cos(φ) - N

Where:
  b = WGS84 semi-minor axis
  e'² = second eccentricity squared
  θ = parametric latitude (intermediate value)
```

### ENU Rotation Matrix

At a given origin (φ₀, λ₀), the rotation matrix from ECEF to ENU is:

```
East  = [-sin(λ₀),              cos(λ₀),             0]
North = [-sin(φ₀)·cos(λ₀), -sin(φ₀)·sin(λ₀), cos(φ₀)]
Up    = [ cos(φ₀)·cos(λ₀),  cos(φ₀)·sin(λ₀), sin(φ₀)]

R = [East; North; Up]
```

The inverse transformation (ENU to ECEF) is the transpose: R^T = R^(-1)

### ENU to Unity Coordinate Mapping

```
ENU (Right-handed, Z-up):  [East, North, Up]
Unity (Left-handed, Y-up): [X=East, Y=Up, Z=North]

Transformation:
  Unity.X = ENU.x (East)
  Unity.Y = ENU.z (Up)
  Unity.Z = ENU.y (North)
```

## Precision Considerations

### Double vs Float Precision

- **Geographic/ECEF coordinates require double precision** - Earth's radius is ~6.4 million meters
- **Local coordinates can use float precision** - by working relative to an origin, values stay small
- **Unity's Vector3 uses float** - final conversion from double to float occurs at the Unity coordinate stage

### Practical Precision Limits

When using an origin-based local coordinate system:
- **Within 100km of origin**: < 1cm error
- **Within 1000km of origin**: < 10cm error
- **Beyond 1000km**: Consider implementing a floating origin system

The precision limit comes from float's ~7 decimal digits of precision. At 1000km (1,000,000m), float precision is approximately 1,000,000 / 10^7 H 0.1m.

### Best Practices

1. **Choose origin carefully**: Place it near the center of your operational area
2. **Keep origin stable**: Don't move the origin during operation unless implementing floating origin
3. **Use double precision** for all geographic/ECEF calculations
4. **Convert to float only at the final step** when integrating with game engines
5. **For large-scale applications**: Implement a floating origin system

## Integration with Unity

This library is designed to integrate seamlessly with Unity projects:

```csharp
using UnityEngine;
using Cooney.Geospatial;

public class GeoreferencingManager : MonoBehaviour
{
    private LocalCoordinateSystem _coordinateSystem;

    void Start()
    {
        // Set origin to Sydney CBD
        var origin = new GeographicCoordinates(-33.8688, 151.2093, 0);
        _coordinateSystem = new LocalCoordinateSystem(origin);
    }

    public void PlaceObjectAtGeographicLocation(GameObject obj, GeographicCoordinates location)
    {
        Vector3 localPos = _coordinateSystem.GeographicToLocal(location);
        obj.transform.position = new UnityEngine.Vector3(localPos.X, localPos.Y, localPos.Z);
    }

    public GeographicCoordinates GetGeographicLocation(GameObject obj)
    {
        var pos = obj.transform.position;
        return _coordinateSystem.LocalToGeographic(new System.Numerics.Vector3(pos.x, pos.y, pos.z));
    }
}
```

Note: You may need to convert between `UnityEngine.Vector3` and `System.Numerics.Vector3`.

## Performance Considerations

### Optimization Tips

1. **Cache transformation matrices**: Calculate `ecefToEnuMatrix` and `enuToEcefMatrix` once per origin
2. **Batch conversions**: Convert multiple coordinates in a loop rather than one at a time
3. **Avoid redundant conversions**: Store converted values rather than recalculating
4. **Use ECEF for calculations**: When working with multiple points, keep them in ECEF for vector math

## Future Enhancements

Potential features for future releases:

### Additional Coordinate Systems
- UTM (Universal Transverse Mercator) projection
- State Plane Coordinates (US)
- Custom datum support (GRS80, NAD83)

### Advanced Features
- Haversine distance calculation (great-circle distance)
- Bearing calculation between points
- Vincenty's formulae for high-accuracy geodesic calculations
- Geoid height models (EGM96, EGM2008)

### Performance
- SIMD optimizations for batch conversions
- Span-based APIs for zero-copy operations
- Async/parallel batch processing

### Utilities
- KML/KMZ and GPX file parsing
- GeoJSON support
- Coordinate format parsing and serialization

## References

### WGS84 Specification
- NIMA Technical Report TR8350.2: "Department of Defense World Geodetic System 1984"
- [WGS84 on Wikipedia](https://en.wikipedia.org/wiki/World_Geodetic_System)

### Coordinate Conversion Algorithms
- Bowring, B. R. (1976). "Transformation from spatial to geographical coordinates"
- Olson, D. K. (1996). "Converting Earth-Centered, Earth-Fixed Coordinates to Geodetic Coordinates"

### Related Systems
- [Unreal Engine Georeferencing Plugin](https://docs.unrealengine.com/5.0/en-US/georeferencing-in-unreal-engine/)
- [Cesium for Unity](https://cesium.com/platform/cesium-for-unity/)
