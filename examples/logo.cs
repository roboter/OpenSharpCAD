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

void Logo(double size = 50.0, int fn = 100)
{
    double holeRadius = (size / 2.0) / 2.0;
    double cylinderHeight = size * 1.25;

    // 1. Central sphere of diameter 'size' (radius = size / 2)
    var sphere = new Sphere(radius: size / 2.0);

    // 2. Three orthogonal cutting cylinders along Z, X, and Y axes
    var cylZ = new Cylinder(radius: holeRadius, height: cylinderHeight, sides: fn).Translate(0, 0, -cylinderHeight / 2.0);
    var cylX = new Rotate(cylZ, y: MathHelper.DegreesToRadians(90));
    var cylY = new Rotate(cylZ, x: MathHelper.DegreesToRadians(90));

    // 3. Perform boolean subtraction using natural C# operator overloading (-)
    var logo = sphere - cylZ - cylX - cylY;

    // 4. Render with custom RGB color (classic OpenSCAD warm orange)
    Draw(logo, RGB(255, 170, 50));
}

// Generate the 50 mm logo
Logo(50);
