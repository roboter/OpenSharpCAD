# MatterCAD Documentation

Welcome to the MatterCAD documentation. This guide provides an overview of all available shapes, transformations, and operations in the MatterCAD library.

## Shapes

### Box
Creates a rectangular box.
- **Constructors**:
  - `Box(double sizeX, double sizeY, double sizeZ, string name = "", bool createCentered = true)`
  - `Box(Vector3 size, string name = "", bool createCentered = true)`

![Box](examples/box.svg)

### Cylinder
Creates a cylinder or cone.
- **Constructors**:
  - `Cylinder(double radius, double height, int sides, Alignment alignment = Alignment.z, string name = "")`
  - `Cylinder(double radius1, double radius2, double height, int sides, Alignment alignment = Alignment.z, string name = "")`

![Cylinder](examples/cylinder.svg)

### Sphere
Creates a sphere.
- **Constructors**:
  - `Sphere(double radius, string name = "")`

![Sphere](examples/sphere.svg)

### Torus
Creates a torus (donut shape).
- **Constructors**:
  - `Torus(double majorRadius, double minorRadius, int sides = 20, int segments = 20, string name = "")`

![Torus](examples/torus.svg)

### LinearExtrude
Extrudes a 2D shape along a linear path.
- **Constructors**:
  - `LinearExtrude(IEnumerable<Vector2> points, double height, string name = "")`

### RotateExtrude
Extrudes a 2D shape by rotating it around an axis.
- **Constructors**:
  - `RotateExtrude(IEnumerable<Vector2> points, int sides = 20, string name = "")`

---

## Transformations

### Translate
Moves an object in 3D space.
- **Example**: `new Translate(myObject, x: 10, y: 0, z: 5);`

### Rotate
Rotates an object around the X, Y, and Z axes.
- **Example**: `new Rotate(myObject, x: MathHelper.DegreesToRadians(45));`

### Scale
Changes the size of an object.
- **Example**: `new Scale(myObject, x: 2, y: 1, z: 1);`

### Align
Aligns one object to another based on their faces.

---

## Operations (CSG)

### Union
Combines multiple objects into one.
- **C# operator**: `obj1 + obj2`

![Union](examples/union.svg)

### Difference
Subtracts one object from another.
- **C# operator**: `obj1 - obj2`

![Difference](examples/difference.svg)

### Intersection
Keeps only the overlapping part of two objects.
- **C# operator**: `obj1 & obj2` (or similar depending on implementation)

![Intersection](examples/intersection.svg)
