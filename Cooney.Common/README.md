# Cooney.Common

> [!WARNING]
> This library is *entirely* vibe coded. No assumptions should be made about its correctness for any purpose whatsoever.

A .NET Standard 2.0 library providing fundamental mathematical types and utilities. This library is designed to be framework-agnostic and can be used in Unity, non-Unity applications, and any .NET Standard 2.0 compatible environment.

## Quick Start

### Basic Vector Operations

```csharp
using Cooney.Common.Maths;

// Create a 3D vector
var vector = new Double3(1.5, 2.5, 3.5);

// Access components
double x = vector.x;
double y = vector.y;
double z = vector.z;

Console.WriteLine($"Vector: ({vector.x}, {vector.y}, {vector.z})");
```

### Matrix-Vector Multiplication

```csharp
using Cooney.Common.Maths;

// Create a rotation matrix (90° rotation around Z-axis)
var matrix = new Double3x3(
    new Double3(0, 1, 0),   // Column 0
    new Double3(-1, 0, 0),  // Column 1
    new Double3(0, 0, 1)    // Column 2
);

var vector = new Double3(1, 0, 0);

// Multiply matrix by vector
Double3 result = Maths.Mul(matrix, vector);
Console.WriteLine($"Result: ({result.x}, {result.y}, {result.z})"); // (0, -1, 0)
```

### Matrix Transpose

```csharp
using Cooney.Common.Maths;

var matrix = new Double3x3(
    new Double3(1, 2, 3),
    new Double3(4, 5, 6),
    new Double3(7, 8, 9)
);

Double3x3 transposed = Maths.Transpose(matrix);
// Rows become columns, columns become rows
```

### Angle Conversions

```csharp
using Cooney.Common.Maths;
using System;

double degrees = 90.0;
double radians = degrees * Maths.DegreesToRadians;

Console.WriteLine($"{degrees}° = {radians} radians"); // 90° = 1.5707963267948966 radians

double backToDegrees = radians * Maths.RadiansToDegrees;
Console.WriteLine($"{radians} radians = {backToDegrees}°"); // 1.5707... radians = 90°
```

## API Reference

### Double3

Represents a 3-dimensional vector with double-precision components.

```csharp
public struct Double3
{
    public double x;
    public double y;
    public double z;

    public Double3(double x, double y, double z);
}
```

**Properties:**
- `x`: X component (double precision)
- `y`: Y component (double precision)
- `z`: Z component (double precision)

**Usage Notes:**
- This is a value type (struct) - passed by value unless using `ref` or `in` keywords
- All fields are public for direct access and maximum performance
- No operator overloads are provided - use the `Maths` static class for operations
- Commonly used for: ECEF coordinates, ENU coordinates, high-precision positions

### Double3x3

Represents a 3x3 matrix with double-precision components in column-major layout.

```csharp
public struct Double3x3
{
    public Double3 c0;  // Column 0
    public Double3 c1;  // Column 1
    public Double3 c2;  // Column 2

    public Double3x3(Double3 c0, Double3 c1, Double3 c2);
}
```

**Properties:**
- `c0`: First column vector
- `c1`: Second column vector
- `c2`: Third column vector

**Layout:**
```
Matrix:          Code representation:
[ c0.x  c1.x  c2.x ]    c0 = (c0.x, c0.y, c0.z)
[ c0.y  c1.y  c2.y ]    c1 = (c1.x, c1.y, c1.z)
[ c0.z  c1.z  c2.z ]    c2 = (c2.x, c2.y, c2.z)
```

**Usage Notes:**
- **Column-major layout**: Each `Double3` represents a column, not a row
- This matches the memory layout used by most graphics APIs and linear algebra libraries
- Commonly used for: rotation matrices, coordinate transformation matrices

### Maths

Static class providing mathematical constants and operations.

```csharp
public static class Maths
{
    // Constants
    public const double DegreesToRadians = Math.PI / 180.0;  // ≈ 0.017453292519943295
    public const double RadiansToDegrees = 180.0 / Math.PI;  // ≈ 57.29577951308232

    // Operations
    public static Double3 Mul(Double3x3 m, Double3 v);
    public static Double3x3 Transpose(Double3x3 m);
}
```

**Constants:**
- `DegreesToRadians`: Conversion factor from degrees to radians (π/180)
- `RadiansToDegrees`: Conversion factor from radians to degrees (180/π)

**Methods:**

#### `Mul(Double3x3 m, Double3 v)`
Multiplies a 3x3 matrix by a 3D vector.

**Parameters:**
- `m`: The matrix (left operand)
- `v`: The vector (right operand)

**Returns:** The resulting vector after transformation

**Formula:**
```
result.x = m.c0.x * v.x + m.c1.x * v.y + m.c2.x * v.z
result.y = m.c0.y * v.x + m.c1.y * v.y + m.c2.y * v.z
result.z = m.c0.z * v.x + m.c1.z * v.y + m.c2.z * v.z
```

#### `Transpose(Double3x3 m)`
Computes the transpose of a 3x3 matrix (swaps rows and columns).

**Parameters:**
- `m`: The matrix to transpose

**Returns:** The transposed matrix

**Formula:**
```
If input matrix is:        Output matrix is:
[ m.c0.x  m.c1.x  m.c2.x ]    [ m.c0.x  m.c0.y  m.c0.z ]
[ m.c0.y  m.c1.y  m.c2.y ] -> [ m.c1.x  m.c1.y  m.c1.z ]
[ m.c0.z  m.c1.z  m.c2.z ]    [ m.c2.x  m.c2.y  m.c2.z ]
```

**Note:** For rotation matrices, the transpose is equal to the inverse.

## Usage Examples

### Example 1: Creating a Rotation Matrix

```csharp
using Cooney.Common.Maths;
using System;

// Create a rotation matrix for 45° around Z-axis
double angle = 45.0 * Maths.DegreesToRadians;
double cos = Math.Cos(angle);
double sin = Math.Sin(angle);

var rotationMatrix = new Double3x3(
    new Double3(cos, sin, 0),   // Column 0
    new Double3(-sin, cos, 0),  // Column 1
    new Double3(0, 0, 1)        // Column 2
);

// Rotate a vector
var vector = new Double3(1, 0, 0);
Double3 rotated = Maths.Mul(rotationMatrix, vector);

Console.WriteLine($"Original: ({vector.x:F3}, {vector.y:F3}, {vector.z:F3})");
Console.WriteLine($"Rotated:  ({rotated.x:F3}, {rotated.y:F3}, {rotated.z:F3})");
```

### Example 2: Chaining Transformations

```csharp
using Cooney.Common.Maths;

// Create a transformation matrix
var transform = new Double3x3(
    new Double3(2, 0, 0),  // Scale X by 2
    new Double3(0, 2, 0),  // Scale Y by 2
    new Double3(0, 0, 2)   // Scale Z by 2
);

// Apply transformation to multiple points
var points = new Double3[]
{
    new Double3(1, 0, 0),
    new Double3(0, 1, 0),
    new Double3(0, 0, 1)
};

foreach (var point in points)
{
    Double3 transformed = Maths.Mul(transform, point);
    Console.WriteLine($"({point.x}, {point.y}, {point.z}) -> ({transformed.x}, {transformed.y}, {transformed.z})");
}
```

### Example 3: Inverting a Rotation Matrix

```csharp
using Cooney.Common.Maths;
using System;

// Create a rotation matrix
double angle = 30.0 * Maths.DegreesToRadians;
var rotationMatrix = new Double3x3(
    new Double3(Math.Cos(angle), Math.Sin(angle), 0),
    new Double3(-Math.Sin(angle), Math.Cos(angle), 0),
    new Double3(0, 0, 1)
);

// For rotation matrices, transpose is the inverse
Double3x3 inverseRotation = Maths.Transpose(rotationMatrix);

// Rotate and then un-rotate
var vector = new Double3(5, 3, 2);
Double3 rotated = Maths.Mul(rotationMatrix, vector);
Double3 restored = Maths.Mul(inverseRotation, rotated);

Console.WriteLine($"Original: ({vector.x:F6}, {vector.y:F6}, {vector.z:F6})");
Console.WriteLine($"Rotated:  ({rotated.x:F6}, {rotated.y:F6}, {rotated.z:F6})");
Console.WriteLine($"Restored: ({restored.x:F6}, {restored.y:F6}, {restored.z:F6})");
```

## Integration with Other Libraries

### Cooney.Geospatial

This library is used extensively by [Cooney.Geospatial](../Cooney.Geospatial) for coordinate transformations:

```csharp
using Cooney.Common.Maths;
using Cooney.Geospatial;

// Common types are used for ENU coordinates and transformation matrices
var origin = new GeographicCoordinates(-33.8688, 151.2093, 0);
Double3x3 ecefToEnuMatrix = CoordinateTransformations.CalculateEcefToEnuMatrix(
    origin.Latitude,
    origin.Longitude
);

// Matrix is used to transform ECEF coordinates to ENU
```

### Unity Integration

Converting between `Double3` and Unity's `Vector3`:

```csharp
using Cooney.Common.Maths;
using UnityEngine;

public static class UnityExtensions
{
    public static Vector3 ToUnityVector3(this Double3 d3)
    {
        return new Vector3((float)d3.x, (float)d3.y, (float)d3.z);
    }

    public static Double3 ToDouble3(this Vector3 v3)
    {
        return new Double3(v3.x, v3.y, v3.z);
    }
}

// Usage
var highPrecision = new Double3(1.23456789012345, 2.34567890123456, 3.45678901234567);
Vector3 unityVector = highPrecision.ToUnityVector3();
```

## Mathematical Foundation

### Matrix-Vector Multiplication

Matrix-vector multiplication transforms a vector by a matrix. For a 3x3 matrix **M** and vector **v**:

```
[ m₀₀  m₀₁  m₀₂ ]   [ vₓ ]   [ m₀₀vₓ + m₀₁vᵧ + m₀₂vᵤ ]
[ m₁₀  m₁₁  m₁₂ ] × [ vᵧ ] = [ m₁₀vₓ + m₁₁vᵧ + m₁₂vᵤ ]
[ m₂₀  m₂₁  m₂₂ ]   [ vᵤ ]   [ m₂₀vₓ + m₂₁vᵧ + m₂₂vᵤ ]
```

In column-major notation (as used by `Double3x3`):

```
result = m.c0 * v.x + m.c1 * v.y + m.c2 * v.z
```

### Matrix Transpose

The transpose of a matrix **M** is denoted **Mᵀ** and is obtained by swapping rows and columns:

```
If M = [ a  b  c ]     Then Mᵀ = [ a  d  g ]
       [ d  e  f ]              [ b  e  h ]
       [ g  h  i ]              [ c  f  i ]
```

**Properties:**
- **(Mᵀ)ᵀ = M** (transposing twice gives the original matrix)
- For rotation matrices: **Mᵀ = M⁻¹** (transpose equals inverse)
- **(AB)ᵀ = BᵀAᵀ** (transpose of product is product of transposes in reverse order)
