// ==============================================================================
// OpenSCAD to OpenSharpCAD Example: SCARA Robotic Arm Trajectory Animation
// ==============================================================================
//
// In OpenSCAD (animation.scad), positioning the second arm link required writing
// complex trigonometric functions (angB, ang2, ang1), manual angle offsets, and
// nested translate() / rotate() blocks:
//
//   rotate([0, 0, a1]) segment("red", l1);
//   translate(l1 * [cos(a1), sin(a1), 0]) {
//       rotate([0, 0, a2]) segment("green", l2);
//   }
//
// In OpenSharpCAD, we eliminate that complexity using native OpenSharpCAD features:
// 1. Construct Link 1 with new Line(arm1_length, elbowAngle).
// 2. Directly extract link1.EndPoint as the 3D elbow joint coordinate.
// 3. Construct Link 2 directly from link1.EndPoint to targetPos.
// 4. Render solids using CylinderBetween(link1, r) and CylinderBetween(link2, r).
// No ang2, angB, or nested transform calculations needed!
// ==============================================================================

// 1. Arm segment lengths and joint radius
double arm1_length = 70.0; // Red arm segment length
double arm2_length = 50.0; // Green arm segment length
double r = 2.0;            // Arm thickness / joint radius

// 2. Parametric path traced by the stylus over time t in [0..1]
Point Position(double timeVal)
{
    if (timeVal < 0.5)
    {
        double angle = 5.0 * 2.0 * Math.PI * timeVal;
        return new Point(
            200.0 * timeVal - 50.0,
            30.0 * Math.Sin(angle) + 60.0
        );
    }
    else
    {
        double angle = 2.0 * Math.PI * (timeVal - 0.5);
        return new Point(
            50.0 * Math.Cos(angle),
            -100.0 * Math.Sin(angle) + 60.0
        );
    }
}

// 3. Draw base plate & etched trajectory groove
var plate = new Box(150, 150, 1).Translate(0, 20, -3.0 * r - 1.0);
Draw(plate, RGB(38, 42, 54));

int curveSamples = 60;
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

// 4. Target position from animation variable $t (or t / Time)
Point targetPos = Position($t);
Point basePos = new Point(0, 0, 0);

// Calculate base angle to elbow using Law of Cosines at the base origin:
// d^2 = L1^2 + targetDist^2 - 2*L1*targetDist*cos(theta)
double targetDist = basePos.DistanceTo(targetPos);
double targetAngle = Math.Atan2(targetPos.Y, targetPos.X);
double cosTheta = Math.Clamp(
    (arm1_length * arm1_length + targetDist * targetDist - arm2_length * arm2_length) / (2.0 * arm1_length * targetDist),
    -1.0, 1.0
);
double elbowAngle = targetAngle - Math.Acos(cosTheta);

// 5. OpenSharpCAD Feature: Use Line to construct link 1, then retrieve link1.EndPoint
// directly as the joint pivot without manual coordinate or multi-angle calculations!
var link1 = new Line(arm1_length, elbowAngle);
Point elbowPos = link1.EndPoint;

// Link 2 bridges directly from link1.EndPoint to targetPos
var link2 = new Line(elbowPos, targetPos);

// 6. Draw SCARA robotic arm links & joints using OpenSharpCAD's CylinderBetween
// Base turret / shoulder pivot
Draw(new Sphere(2.0 * r), RGB(180, 185, 205));
Draw(new Cylinder(radius: 2.0, height: 6.0 * r).Translate(0, 0, -3.0 * r), RGB(180, 185, 205));

// Red arm (first link: base -> elbow)
Draw(CylinderBetween(link1, r), RGB(255, 85, 85));
Draw(new Sphere(r), RGB(255, 85, 85));

// Elbow joint pivot
Draw(new Sphere(2.0 * r).Translate(elbowPos), RGB(180, 185, 205));
Draw(new Cylinder(radius: 2.0, height: 4.0 * r).Translate(0, 0, -2.0 * r).Translate(elbowPos), RGB(180, 185, 205));

// Green arm (second link: elbow -> target)
Draw(CylinderBetween(link2, r), RGB(80, 250, 123));
Draw(new Sphere(r).Translate(elbowPos), RGB(80, 250, 123));
Draw(new Sphere(r).Translate(targetPos), RGB(80, 250, 123));

// End effector stylus pointing down at current (x, y)
var stylus = new Cylinder(radius1: 0.2, radius2: r, height: 4.0 * r)
    .Translate(0, 0, -2.0 * r)
    .Translate(targetPos.X, targetPos.Y, -r / 2.0);
Draw(stylus, RGB(255, 184, 108));

