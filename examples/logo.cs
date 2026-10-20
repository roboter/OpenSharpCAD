// ==============================================================================
// OpenSCAD to OpenSharpCAD Example: The Iconic OpenSCAD Logo
// ==============================================================================
//
// Original OpenSCAD script (logo.scad by Clifford Wolf & Marius Kintel, CC0):
// ------------------------------------------------------------------------------
// Logo(50);
//
// module Logo(size=50, $fn=100) {
//     hole = size/2;
//     cylinderHeight = size * 1.25;
//
//     difference() {
//         sphere(d=size);
//         cylinder(d=hole, h=cylinderHeight, center=true);
//         #rotate([90, 0, 0]) cylinder(d=hole, h=cylinderHeight, center=true);
//         rotate([0, 90, 0]) cylinder(d=hole, h=cylinderHeight, center=true);
//     }
// }
// ==============================================================================

CsgObject Logo(double size = 50.0, int fn = 100)
{
    double holeRadius = (size / 2.0) / 2.0;
    double cylinderHeight = size * 1.25;

    // 1. Central sphere of diameter 'size' (radius = size / 2)
    var sphere = new Sphere(radius: size / 2.0);

    // 2. Three orthogonal cutting cylinders along Z, X, and Y axes
    var cylZ = new Cylinder(radius: holeRadius, height: cylinderHeight, sides: fn);
    var cylX = new Rotate(cylZ, y: MathHelper.DegreesToRadians(90));
    var cylY = new Rotate(cylZ, x: MathHelper.DegreesToRadians(90));

    // 3. Perform boolean subtraction using natural C# operator overloading (-)
    return sphere - cylZ - cylX - cylY;
}

// Generate the 50 mm logo
Draw(Logo(50), RGB(255, 170, 50));
