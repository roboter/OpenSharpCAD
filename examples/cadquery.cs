// ==============================================================================
// CadQuery to OpenSharpCAD Example: Parametric Twisted Cycloidal Gear
// ==============================================================================
//
// Original CadQuery (Python) script:
// ------------------------------------------------------------------------------
// import cadquery as cq
// from math import sin, cos, pi, floor
//
// def hypocycloid(t, r1, r2):
//     return (
//         (r1 - r2) * cos(t) + r2 * cos(r1 / r2 * t - t),
//         (r1 - r2) * sin(t) + r2 * sin(-(r1 / r2 * t - t)),
//     )
//
// def epicycloid(t, r1, r2):
//     return (
//         (r1 + r2) * cos(t) - r2 * cos(r1 / r2 * t + t),
//         (r1 + r2) * sin(t) - r2 * sin(r1 / r2 * t + t),
//     )
//
// def gear(t, r1=4, r2=1):
//     if (-1) ** (1 + floor(t / 2 / pi * (r1 / r2))) < 0:
//         return epicycloid(t, r1, r2)
//     else:
//         return hypocycloid(t, r1, r2)
//
// result = (
//     cq.Workplane("XY")
//     .parametricCurve(lambda t: gear(t * 2 * pi, 6, 1))
//     .twistExtrude(15, 90)
//     .faces(">Z")
//     .workplane()
//     .circle(2)
//     .cutThruAll()
// )
//
// show_object(result)
// ==============================================================================

// 1. Define the generating mathematical functions
Vector2 Hypocycloid(double t, double r1, double r2)
{
    return new Vector2(
        (r1 - r2) * Math.Cos(t) + r2 * Math.Cos((r1 / r2) * t - t),
        (r1 - r2) * Math.Sin(t) + r2 * Math.Sin(-((r1 / r2) * t - t))
    );
}

Vector2 Epicycloid(double t, double r1, double r2)
{
    return new Vector2(
        (r1 + r2) * Math.Cos(t) - r2 * Math.Cos((r1 / r2) * t + t),
        (r1 + r2) * Math.Sin(t) - r2 * Math.Sin((r1 / r2) * t + t)
    );
}

Vector2 Gear(double t, double r1 = 4, double r2 = 1)
{
    // Parity condition: alternate between epicycloid (outer lobe) and hypocycloid (inner root)
    if (Math.Pow(-1, 1 + Math.Floor(t / (2 * Math.PI) * (r1 / r2))) < 0)
        return Epicycloid(t, r1, r2);
    else
        return Hypocycloid(t, r1, r2);
}

// 2. Generate 2D parametric gear profile (sampling t in [0, 2*PI])
int steps = 180;
var gearProfile = new Path();
for (int i = 0; i < steps; i++)
{
    double t = (double)i / steps * 2.0 * Math.PI;
    var pt = Gear(t, r1: 6, r2: 1);
    gearProfile.AddPoint(pt.X, pt.Y);
}
gearProfile.Close(); // Essential: close path to form a 2D boundary

// 3. TwistExtrude: height 15 mm, 90 degree twist along Z axis
var twistedGear = TwistExtrude(gearProfile, height: 15, twistDegrees: 90, slices: 30);

// 4. Center bore hole: circle of radius 2 cut all the way through (CSG subtraction)
var centerHole = new Cylinder(radius: 2, height: 25).Translate(0, 0, 7.5);
var result = twistedGear - centerHole;

// 5. Render final model in viewport
Draw(result, RGB(255, 121, 198));
