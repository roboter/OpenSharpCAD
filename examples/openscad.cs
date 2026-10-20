// ==============================================================================
// OpenSCAD to OpenSharpCAD Example: SCARA Robotic Arm Trajectory Animation
// ==============================================================================
//
// Original OpenSCAD script (animation.scad by Torsten Paul <Torsten.Paul@gmx.de>, CC0):
// ------------------------------------------------------------------------------
// arm1_length = 70;
// arm2_length = 50;
// r = 2;
// $fn = 30;
//
// plate();
// pos = position($t);
// arm(pos[0], pos[1], arm1_length, arm2_length);
//
// function position(t) = t < 0.5
//     ? [ 200 * t - 50, 30 * sin(5 * 360 * t) + 60 ]
//     : [ 50 * cos(360 * (t - 0.5)), 100 * -sin(360 * (t- 0.5)) + 60 ];
//
// function sq(x, y) = x * x + y * y;
// function angB(x, y, l1, l2) = 180 - acos((l2 * l2 + l1 * l1 - sq(x, y)) / (2 * l1 * l2));
// function ang2(x, y, l1, l2) = 90 - acos((l2 * l2 - l1 * l1 + sq(x, y)) / (2 * l2 * sqrt(sq(x, y)))) - atan2(x, y);
// function ang1(x, y, l1, l2) = ang2(x, y, l1, l2) + angB(x, y, l1, l2);
// ==============================================================================

// 1. Arm segment lengths and joint radius
double arm1_length = 70.0; // Red arm segment length
double arm2_length = 50.0; // Green arm segment length
double r = 2.0;            // Arm thickness / joint radius

// 2. Degree-based trigonometry helpers matching OpenSCAD behavior
double DegToRad(double deg) => deg * Math.PI / 180.0;
double RadToDeg(double rad) => rad * 180.0 / Math.PI;

double SinDeg(double deg) => Math.Sin(DegToRad(deg));
double CosDeg(double deg) => Math.Cos(DegToRad(deg));
double AcosDeg(double val) => RadToDeg(Math.Acos(Math.Clamp(val, -1.0, 1.0)));
double Atan2Deg(double y, double x) => RadToDeg(Math.Atan2(y, x));

// 3. Inverse kinematics functions for SCARA 2-link planar arm
double Sq(double x, double y) => x * x + y * y;

double AngB(double x, double y, double l1, double l2)
{
    double cosB = (l2 * l2 + l1 * l1 - Sq(x, y)) / (2.0 * l1 * l2);
    return 180.0 - AcosDeg(cosB);
}

double Ang2(double x, double y, double l1, double l2)
{
    double distSq = Sq(x, y);
    double dist = Math.Sqrt(distSq);
    double cosVal = (l2 * l2 - l1 * l1 + distSq) / (2.0 * l2 * dist);
    return 90.0 - AcosDeg(cosVal) - Atan2Deg(y, x);
}

double Ang1(double x, double y, double l1, double l2)
{
    return Ang2(x, y, l1, l2) + AngB(x, y, l1, l2);
}

// 4. Parametric path traced by the stylus over time t in [0..1]
Vector2 Position(double timeVal)
{
    if (timeVal < 0.5)
    {
        return new Vector2(
            200.0 * timeVal - 50.0,
            30.0 * SinDeg(5.0 * 360.0 * timeVal) + 60.0
        );
    }
    else
    {
        return new Vector2(
            50.0 * CosDeg(360.0 * (timeVal - 0.5)),
            100.0 * -SinDeg(360.0 * (timeVal - 0.5)) + 60.0
        );
    }
}

// 5. Capsule arm segment (hull of two endpoint spheres connected by a cylinder)
CsgObject Segment(double length, double radius)
{
    var s1 = new Sphere(radius);
    var s2 = new Sphere(radius).Translate(length, 0, 0);
    var body = CylinderBetween(new Vector3(0, 0, 0), new Vector3(length, 0, 0), radius, sides: 16);
    return s1 + s2 + body;
}

// 6. Draw base plate & etched trajectory curve
var plate = new Box(150, 150, 1).Translate(-75, 25 - 75, -3.0 * r - 1.0);
Draw(plate, RGB(38, 42, 54));

int curveSamples = 150;
for (int i = 0; i < curveSamples; i++)
{
    double t0 = (double)i / curveSamples;
    double t1 = (double)(i + 1) / curveSamples;
    var p0 = Position(t0);
    var p1 = Position(t1);
    var groove = CylinderBetween(
        new Vector3(p0.X, p0.Y, -3.0 * r - 0.1),
        new Vector3(p1.X, p1.Y, -3.0 * r - 0.1),
        radius: 0.5,
        sides: 8
    );
    Draw(groove, RGB(139, 233, 253));
}

// 7. Calculate current position & joint angles from animation variable $t (or t / Time)
var targetPos = Position($t);
double a1 = Ang1(targetPos.X, targetPos.Y, arm1_length, arm2_length);
double a2 = Ang2(targetPos.X, targetPos.Y, arm1_length, arm2_length);

// Base turret / shoulder pivot
var basePivot = new Sphere(2.0 * r) + new Cylinder(radius: 2.0, height: 6.0 * r).Translate(0, 0, -3.0 * r);
Draw(basePivot, RGB(180, 185, 205));

// Red arm (first link)
var arm1 = new Rotate(Segment(arm1_length, r), z: DegToRad(a1));
Draw(arm1, RGB(255, 85, 85));

// Elbow joint
double elbowX = arm1_length * CosDeg(a1);
double elbowY = arm1_length * SinDeg(a1);
var elbowPivot = new Sphere(2.0 * r).Translate(elbowX, elbowY, 0);
Draw(elbowPivot, RGB(180, 185, 205));

// Green arm (second link)
var arm2 = new Rotate(Segment(arm2_length, r), z: DegToRad(a2)).Translate(elbowX, elbowY, 0);
Draw(arm2, RGB(80, 250, 123));

// End effector stylus pointing down at current (x, y)
var stylus = new Cylinder(radius1: 0.2, radius2: r, height: 4.0 * r).Translate(0, 0, -2.0 * r).Translate(targetPos.X, targetPos.Y, -r / 2.0);
Draw(stylus, RGB(255, 184, 108));
