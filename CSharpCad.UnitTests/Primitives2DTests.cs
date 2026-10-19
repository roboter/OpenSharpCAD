using CSharpCAD;
using MatterHackers.Csg.Solids;
using MatterHackers.VectorMath;
using Microsoft.CodeAnalysis;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Circle = MatterHackers.Csg.Solids.Circle;
using Path = MatterHackers.Csg.Solids.Path;

namespace CSharpCad.UnitTests;

[TestFixture]
public class Primitives2DTests
{
    [Test]
    public void TestCircleExtrudeAndRevolve()
    {
        var circle = new Circle(radius: 15);
        Assert.That(circle.IsClosed, Is.True, "Circle should be closed");

        // Extrude
        var extruded = circle.Extrude(height: 20);
        Assert.That(extruded, Is.Not.Null, "Extruded circle should return a valid 3D CsgObject");
        var aabb = extruded.GetAxisAlignedBoundingBox();
        Assert.That(aabb.ZSize, Is.GreaterThan(0));

        // Revolve
        var revolved = circle.Revolve(angleDegrees: 360, axisOffset: 25);
        Assert.That(revolved, Is.Not.Null, "Revolved circle should return a valid 3D CsgObject (Torus)");
    }

    [Test]
    public void TestCircleValidation()
    {
        var invalidCircle = new Circle(radius: -5);
        var ex = Assert.Throws<InvalidOperationException>(() => invalidCircle.Extrude(10));
        Assert.That(ex.Message, Does.Contain("radius must be positive"));
    }

    [Test]
    public void TestRectangleAndSquareExtrudeAndRevolve()
    {
        var rect = new Rectangle(width: 30, height: 20);
        Assert.That(rect.IsClosed, Is.True);
        var rectExtrude = rect.Extrude(height: 10);
        Assert.That(rectExtrude, Is.Not.Null);

        var roundedRect = new Rectangle(30, 20, cornerRadius: 3);
        var roundedExtrude = roundedRect.Extrude(15);
        Assert.That(roundedExtrude, Is.Not.Null);

        var square = new Square(size: 25);
        Assert.That(square.IsClosed, Is.True);
        var squareExtrude = square.Extrude(25);
        Assert.That(squareExtrude, Is.Not.Null);

        // Test alternate spelling
        var sqare = new Sqare(size: 15);
        Assert.That(sqare.IsClosed, Is.True);
        var sqareExtrude = sqare.Extrude(10);
        Assert.That(sqareExtrude, Is.Not.Null);
    }

    [Test]
    public void TestRectangleValidation()
    {
        var invalidRect = new Rectangle(-10, 20);
        var ex = Assert.Throws<InvalidOperationException>(() => invalidRect.Extrude(10));
        Assert.That(ex.Message, Does.Contain("width and height must be positive"));
    }

    [Test]
    public void TestClosedPathExtrudeAndRevolve()
    {
        var path = new Path();
        path.MoveTo(0, 0)
            .LineTo(30, 0)
            .LineTo(30, 20)
            .LineTo(0, 20)
            .Close();

        Assert.That(path.IsClosed, Is.True, "Path should be closed after Close()");
        var extruded = path.Extrude(height: 12);
        Assert.That(extruded, Is.Not.Null);

        var revolved = path.Revolve(angleDegrees: 180, axisOffset: 10);
        Assert.That(revolved, Is.Not.Null);

        // Path initialized from coordinate array with close: true
        var coordPath = new Path(new double[] { 0, 0, 10, 0, 10, 10, 0, 10 }, close: true);
        Assert.That(coordPath.IsClosed, Is.True);
        Assert.That(coordPath.Extrude(5), Is.Not.Null);
    }

    [Test]
    public void TestUnclosedPathValidation()
    {
        // 1. Open path without Close()
        var openPath = new Path();
        openPath.MoveTo(0, 0)
                .LineTo(10, 0)
                .LineTo(10, 10);

        Assert.That(openPath.IsClosed, Is.False, "Path should be open");
        var ex1 = Assert.Throws<InvalidOperationException>(() => openPath.Extrude(10));
        Assert.That(ex1.Message, Does.Contain("open path").And.Contain("Close()"));

        // 2. Path with fewer than 3 points
        var shortPath = new Path();
        shortPath.MoveTo(0, 0).LineTo(10, 0);
        shortPath.Close();
        var ex2 = Assert.Throws<InvalidOperationException>(() => shortPath.Extrude(10));
        Assert.That(ex2.Message, Does.Contain("At least 3 points"));

        // 3. Empty path
        var emptyPath = new Path();
        var ex3 = Assert.Throws<InvalidOperationException>(() => emptyPath.Extrude(10));
        Assert.That(ex3.Message, Does.Contain("empty path"));
    }

    [Test]
    public void TestLineValidation()
    {
        var line = new Line(0, 0, 20, 20);
        Assert.That(line.IsClosed, Is.False);

        var exExtrude = Assert.Throws<InvalidOperationException>(() => line.Extrude(10));
        Assert.That(exExtrude.Message, Does.Contain("open Line").And.Contain("Path"));

        var exRevolve = Assert.Throws<InvalidOperationException>(() => line.Revolve());
        Assert.That(exRevolve.Message, Does.Contain("open Line"));

        // Converting lines into a closed Path works
        var path = new Path();
        path.Add(new Line(0, 0, 20, 0));
        path.Add(new Line(20, 0, 10, 20));
        path.Close();
        Assert.That(path.IsClosed, Is.True);
        Assert.That(path.Extrude(5), Is.Not.Null);
    }

    [Test]
    public void TestDotValidation()
    {
        var dot = new Dot(5, 10);
        Assert.That(dot.IsClosed, Is.False);

        var exExtrude = Assert.Throws<InvalidOperationException>(() => dot.Extrude(10));
        Assert.That(exExtrude.Message, Does.Contain("Dot").And.Contain("0-dimensional"));

        var exRevolve = Assert.Throws<InvalidOperationException>(() => dot.Revolve());
        Assert.That(exRevolve.Message, Does.Contain("Dot"));

        // Can convert to Circle
        var circle = dot.ToCircle(radius: 8);
        Assert.That(circle.IsClosed, Is.True);
        Assert.That(circle.Extrude(10), Is.Not.Null);
    }

    [Test]
    public void TestScriptCompilationWith2DPrimitivesAndExtrusion()
    {
        string script = @"
var basePlate = new Rectangle(60, 40).Extrude(8);
var hole1 = new Circle(radius: 5).Extrude(12);
var hole2 = new Square(size: 8).Extrude(12);

var path = new Path();
path.MoveTo(-10, -5).LineTo(10, -5).LineTo(0, 10).Close();
var rib = path.Extrude(15);

Draw(basePlate - hole1 + rib);
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Empty, "Script with 2D primitives should compile without errors");
        Assert.That(compiled, Is.Not.Null, "Script should return compiled object");

        dynamic dyn = compiled;
        dyn.Build();
        Assert.That((string)dyn.LastError, Is.Null.Or.Empty, "Build should succeed with no runtime error");
    }

    [Test]
    public void TestScriptRuntimeValidationOnOpenPath()
    {
        string script = @"
var openPath = new Path();
openPath.MoveTo(0, 0).LineTo(10, 0).LineTo(10, 10);
// Forgot to call Close()!
var solid = openPath.Extrude(20);
Draw(solid);
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Empty, "Script should compile (syntax is valid)");
        Assert.That(compiled, Is.Not.Null);

        dynamic dyn = compiled;
        dyn.Build();
        string lastError = (string)dyn.LastError;
        Assert.That(lastError, Is.Not.Null.And.Not.Empty, "Build should report runtime error for unclosed path");
        Assert.That(lastError, Does.Contain("open path").And.Contain("Close()"));
    }

    [Test]
    public void TestRectangleToCircleLoft()
    {
        var rect = new Rectangle(width: 40, height: 40);
        var circle = new Circle(radius: 10);

        // 1. Fluent shape1.Loft(shape2, height)
        var duct = rect.Loft(circle, height: 30);
        Assert.That(duct, Is.Not.Null);

        var aabb = duct.GetAxisAlignedBoundingBox();
        Assert.That(aabb.ZSize, Is.EqualTo(30).Within(0.01));
        Assert.That(aabb.MinXYZ.Z, Is.EqualTo(0).Within(0.01));
        Assert.That(aabb.MaxXYZ.Z, Is.EqualTo(30).Within(0.01));

        // 2. Direct new Loft(bottom, top, height)
        var loftDirect = new Loft(rect, circle, height: 25);
        Assert.That(loftDirect, Is.Not.Null);
        var directBox = loftDirect.GetAxisAlignedBoundingBox();
        Assert.That(directBox.ZSize, Is.EqualTo(25).Within(0.01));
    }

    [Test]
    public void TestMultiSectionLoft()
    {
        var bottom = new Circle(radius: 20);
        var middle = new Rectangle(width: 30, height: 30);
        var top = new Circle(radius: 12);

        var loft = new Loft((bottom, 0), (middle, 15), (top, 40));
        Assert.That(loft, Is.Not.Null);

        var aabb = loft.GetAxisAlignedBoundingBox();
        Assert.That(aabb.ZSize, Is.EqualTo(40).Within(0.01));
        Assert.That(aabb.MinXYZ.Z, Is.EqualTo(0).Within(0.01));
        Assert.That(aabb.MaxXYZ.Z, Is.EqualTo(40).Within(0.01));
    }

    [Test]
    public void TestLoftTwistAndCentering()
    {
        var square = new Square(size: 20);
        var twisted = square.Loft(square, height: 40, twistDegrees: 45, centered: true);
        Assert.That(twisted, Is.Not.Null);

        var aabb = twisted.GetAxisAlignedBoundingBox();
        Assert.That(aabb.ZSize, Is.EqualTo(40).Within(0.01));
        Assert.That(aabb.MinXYZ.Z, Is.EqualTo(-20).Within(0.01));
        Assert.That(aabb.MaxXYZ.Z, Is.EqualTo(20).Within(0.01));
    }

    [Test]
    public void TestLoftValidation()
    {
        var validCircle = new Circle(10);
        var openPath = new Path();
        openPath.MoveTo(0, 0).LineTo(10, 0);

        // Open path throws
        var exOpen = Assert.Throws<InvalidOperationException>(() => openPath.Loft(validCircle, 20));
        Assert.That(exOpen.Message, Does.Contain("Cannot loft"));

        // Dot throws
        var dot = new Dot(0, 0);
        var exDot = Assert.Throws<InvalidOperationException>(() => dot.Loft(validCircle, 20));
        Assert.That(exDot.Message, Does.Contain("Cannot loft a Dot"));

        // Line throws
        var line = new Line(0, 0, 10, 10);
        var exLine = Assert.Throws<InvalidOperationException>(() => line.Loft(validCircle, 20));
        Assert.That(exLine.Message, Does.Contain("Cannot loft").And.Contain("Line"));

        // Negative or zero height throws
        Assert.Throws<ArgumentOutOfRangeException>(() => validCircle.Loft(validCircle, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => validCircle.Loft(validCircle, -10));

        // Less than 2 sections throws
        Assert.Throws<ArgumentException>(() => new Loft(new LoftSection[] { new LoftSection(validCircle, 0) }));

        // Consecutive sections with same Z height throws
        Assert.Throws<ArgumentException>(() => new Loft((validCircle, 10), (validCircle, 10)));
    }

    [Test]
    public void TestLoftInScriptCompilationAndCsg()
    {
        string script = @"
var bottom = new Rectangle(30, 30);
var top = new Circle(12);

// Loft transition
var duct = bottom.Loft(top, height: 25);

// Multi-section loft
var multiLoft = Loft((new Circle(15), 0), (new Rectangle(20, 20), 10), (new Circle(8), 20));

// CSG subtraction
var hole = new Cylinder(radius: 6, height: 30, sides: 30);
var hollowDuct = duct - hole;

Draw(hollowDuct);
Draw(multiLoft);
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Empty, "Script with Loft should compile without errors");
        Assert.That(compiled, Is.Not.Null);

        dynamic dyn = compiled;
        dyn.Build();
        Assert.That((string)dyn.LastError, Is.Null.Or.Empty, "Build should succeed with no runtime error");
    }

    [Test]
    public void TestTwistExtrudeBasicAndCentering()
    {
        var square = new Square(size: 20);
        var column = square.TwistExtrude(height: 50, twistDegrees: 90, slices: 25, centered: true);
        Assert.That(column, Is.Not.Null);

        var aabb = column.GetAxisAlignedBoundingBox();
        Assert.That(aabb.ZSize, Is.EqualTo(50).Within(0.01));
        Assert.That(aabb.MinXYZ.Z, Is.EqualTo(-25).Within(0.01));
        Assert.That(aabb.MaxXYZ.Z, Is.EqualTo(25).Within(0.01));

        // Direct class constructor
        var direct = new TwistExtrude(square, height: 30, twistDegrees: 180);
        Assert.That(direct, Is.Not.Null);
        var directAabb = direct.GetAxisAlignedBoundingBox();
        Assert.That(directAabb.ZSize, Is.EqualTo(30).Within(0.01));
    }

    [Test]
    public void TestTwistExtrudeWithScale()
    {
        var rect = new Rectangle(30, 20);
        var tapered = rect.TwistExtrude(height: 40, twistDegrees: 180, slices: 20, scale: 0.5);
        Assert.That(tapered, Is.Not.Null);

        var aabb = tapered.GetAxisAlignedBoundingBox();
        Assert.That(aabb.ZSize, Is.EqualTo(40).Within(0.01));

        // Pyramid apex tapering (scale = 0)
        var pyramid = rect.TwistExtrude(height: 35, twistDegrees: 45, slices: 15, scale: 0.0);
        Assert.That(pyramid, Is.Not.Null);
        var pyramidAabb = pyramid.GetAxisAlignedBoundingBox();
        Assert.That(pyramidAabb.ZSize, Is.EqualTo(35).Within(0.01));
    }

    [Test]
    public void TestExtrudeWithTwistOverload()
    {
        var path = new Path();
        path.MoveTo(-10, -10).LineTo(10, -10).LineTo(10, 10).LineTo(-10, 10).Close();

        // Extrude method with twistDegrees parameter
        var extrudedTwist = path.Extrude(height: 25, twistDegrees: 90, slices: 20);
        Assert.That(extrudedTwist, Is.Not.Null);
        var aabb = extrudedTwist.GetAxisAlignedBoundingBox();
        Assert.That(aabb.ZSize, Is.EqualTo(25).Within(0.01));
    }

    [Test]
    public void TestTwistExtrudeValidation()
    {
        var openPath = new Path();
        openPath.MoveTo(0, 0).LineTo(10, 0);
        Assert.Throws<InvalidOperationException>(() => openPath.TwistExtrude(20));

        var dot = new Dot(5, 5);
        Assert.Throws<InvalidOperationException>(() => dot.TwistExtrude(20));

        var line = new Line(0, 0, 10, 10);
        Assert.Throws<InvalidOperationException>(() => line.TwistExtrude(20));

        var valid = new Circle(10);
        Assert.Throws<ArgumentOutOfRangeException>(() => valid.TwistExtrude(-10));
        Assert.Throws<ArgumentOutOfRangeException>(() => valid.TwistExtrude(20, scale: -0.5));
    }

    [Test]
    public void TestTwistExtrudeInScriptAndCsg()
    {
        string script = @"
var square = new Square(size: 20);
var twistedColumn = square.TwistExtrude(height: 50, twistDegrees: 120, slices: 25);

// LinearExtrude with twist
double[] pts = new double[] { -8, -8, 8, -8, 8, 8, -8, 8 };
var linearTwisted = new LinearExtrude(pts, height: 30, twistRadians: MathHelper.DegreesToRadians(60));

// Standalone TwistExtrude call
var vase = TwistExtrude(new Circle(15), height: 40, twistDegrees: 45, scale: 1.5);

// CSG boolean
var hole = new Cylinder(radius: 5, height: 60, sides: 30);
var hollow = twistedColumn - hole;

Draw(hollow);
Draw(linearTwisted);
Draw(vase);
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Empty, "Script with TwistExtrude should compile without errors");
        Assert.That(compiled, Is.Not.Null);

        dynamic dyn = compiled;
        dyn.Build();
        Assert.That((string)dyn.LastError, Is.Null.Or.Empty, "Build should succeed with no runtime error");
    }

    [Test]
    public void TestPointCompatibilityWithVectorAndTuples()
    {
        // 2D and 3D tuple implicit conversion to Point
        Point p2d = (15.0, 25.0);
        Assert.That(p2d.X, Is.EqualTo(15.0));
        Assert.That(p2d.Y, Is.EqualTo(25.0));
        Assert.That(p2d.Z, Is.EqualTo(0.0));

        Point p3d = (10.0, 20.0, 30.0);
        Assert.That(p3d.X, Is.EqualTo(10.0));
        Assert.That(p3d.Y, Is.EqualTo(20.0));
        Assert.That(p3d.Z, Is.EqualTo(30.0));

        // Implicit conversions to Vector2 and Vector3
        Vector2 v2 = p2d;
        Assert.That(v2.X, Is.EqualTo(15.0));
        Assert.That(v2.Y, Is.EqualTo(25.0));

        Vector3 v3 = p3d;
        Assert.That(v3.X, Is.EqualTo(10.0));
        Assert.That(v3.Y, Is.EqualTo(20.0));
        Assert.That(v3.Z, Is.EqualTo(30.0));

        // Implicit conversions from Vector2 and Vector3
        Point fromV2 = new Vector2(3, 4);
        Assert.That(fromV2.X, Is.EqualTo(3.0));
        Assert.That(fromV2.Y, Is.EqualTo(4.0));

        Point fromV3 = new Vector3(1, 2, 3);
        Assert.That(fromV3.Z, Is.EqualTo(3.0));

        // Arithmetic operators and distance
        var sum = p3d + new Point(1, 1, 1);
        Assert.That(sum.X, Is.EqualTo(11.0));

        var diff = p3d - new Point(10, 20, 0);
        Assert.That(diff.Z, Is.EqualTo(30.0));

        var scaled = new Point(2, 3, 4) * 2;
        Assert.That(scaled.X, Is.EqualTo(4.0));
        Assert.That(scaled.Z, Is.EqualTo(8.0));

        var dist = new Point(0, 0, 0).DistanceTo(new Point(0, 30, 40));
        Assert.That(dist, Is.EqualTo(50.0).Within(1e-6));
    }

    [Test]
    public void TestTwoPointsCreateLineAndExposeHeight()
    {
        // 2 points create a Line
        var p1 = new Point(0, 0, 0);
        var p2 = new Point(0, 0, 40);
        var line = new Line(p1, p2);

        // Height and Length properties
        Assert.That(line.Height, Is.EqualTo(40.0).Within(1e-6));
        Assert.That(line.Length, Is.EqualTo(40.0).Within(1e-6));
        Assert.That(line.Dz, Is.EqualTo(40.0).Within(1e-6));
        Assert.That(line.Dx, Is.EqualTo(0.0).Within(1e-6));

        // p1.LineTo(p2)
        var lineFromMethod = p1.LineTo(p2);
        Assert.That(lineFromMethod.Height, Is.EqualTo(40.0).Within(1e-6));

        // Tuple implicit conversion to Line
        Line lineTuple = (p1, p2);
        Assert.That(lineTuple.Height, Is.EqualTo(40.0).Within(1e-6));

        // Diagonal 3D line
        var pa = new Point(10, 20, 30);
        var pb = new Point(10, 20, 75);
        var diagLine = new Line(pa, pb);
        Assert.That(diagLine.Height, Is.EqualTo(45.0).Within(1e-6));
    }

    [Test]
    public void TestLineHeightUsedInCylinder()
    {
        var p1 = new Point(0, 0, 0);
        var p2 = new Point(0, 0, 40);
        var line = new Line(p1, p2);

        // Directly use line.Height in cylinder height parameter
        var cyl = new Cylinder(radius: 6, height: line.Height);
        Assert.That(cyl.Height, Is.EqualTo(40.0).Within(1e-6));
        Assert.That(cyl.Radius, Is.EqualTo(6.0).Within(1e-6));

        // Direct constructors accepting Line
        var cylDirect = new Cylinder(line, 7);
        Assert.That(cylDirect.Height, Is.EqualTo(40.0).Within(1e-6));
        Assert.That(cylDirect.Radius, Is.EqualTo(7.0).Within(1e-6));

        var cylDirect2 = new Cylinder(line, radius: 8);
        Assert.That(cylDirect2.Height, Is.EqualTo(40.0).Within(1e-6));

        // Line to cylinder strut bridging 3D points
        var strut = line.ToCylinder(radius: 4);
        Assert.That(strut, Is.Not.Null);

        // Cylinder.CreateBetween overloads
        var betweenPoints = Cylinder.CreateBetween(p1, p2, radius: 5);
        Assert.That(betweenPoints, Is.Not.Null);

        var betweenLine = Cylinder.CreateBetween(line, radius: 5);
        Assert.That(betweenLine, Is.Not.Null);
    }

    [Test]
    public void TestPointLineCylinderInCompilerService()
    {
        string script = @"
var p1 = new Point(0, 0, 0);
var p2 = new Point(0, 0, 40);
var line = new Line(p1, p2);
var cyl = new Cylinder(radius: 8, height: line.Height);

Draw(cyl, Color.Orange);
Draw(line, Color.Yellow);
Draw(p1, Color.Red);
Draw(p2, Color.Green);
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Empty, "Point->Line->Cylinder script should compile with 0 diagnostics");
        Assert.That(compiled, Is.Not.Null);

        dynamic dyn = compiled;
        dyn.Build();
        Assert.That((string)dyn.LastError, Is.Null.Or.Empty, "Build should succeed with no runtime error");

        var bounds = dyn.GetBounds();
        Assert.That(bounds, Is.Not.Null, "Bounds should be populated");
        Assert.That(bounds.ZSize, Is.GreaterThanOrEqualTo(39.0), "Bounding box Z size should reflect 40-unit height");
    }

    [Test]
    public void TestLinqCubeEdgesRightSidePointsAndSpheres()
    {
        var cube = new Box(30, 30, 30);

        // 1. LINQ on GetBoundingEdges(): SelectMany -> Where X > 0 -> Distinct
        var rightPointsFromEdges = cube.GetBoundingEdges()
            .SelectMany(e => new[] { e.Start, e.End })
            .Where(pt => pt.X > 0)
            .Distinct()
            .ToList();

        Assert.That(rightPointsFromEdges.Count, Is.EqualTo(4), "Cube has exactly 4 right-side vertices (X > 0)");
        foreach (var pt in rightPointsFromEdges)
        {
            Assert.That(pt.X, Is.GreaterThan(0));
            var sphere = new Sphere(radius: 4).Translate(pt);
            Assert.That(sphere, Is.Not.Null);
        }

        // 2. LINQ on cube.Points: Where X > 0
        var rightPoints = cube.Points
            .Where(p => p.X > 0)
            .Distinct()
            .ToList();
        Assert.That(rightPoints.Count, Is.EqualTo(4));

        // 3. LINQ on cube.Lines: Where X1 > 0 && X2 > 0
        var rightEdges = cube.Lines
            .Where(l => l.X1 > 0 && l.X2 > 0)
            .ToList();
        Assert.That(rightEdges.Count, Is.EqualTo(4), "Cube has exactly 4 bounding edges belonging entirely to the right face");
    }

    [Test]
    public void TestLinqCubeEdgesScriptExecution()
    {
        string script = @"
var cube = new Box(30, 30, 30);

// LINQ: filter only right-side points (X > 0) from edges
var rightPoints = cube.GetBoundingEdges()
    .SelectMany(e => new[] { e.Start, e.End })
    .Where(p => p.X > 0)
    .Distinct();

// Add a sphere to each filtered right point
foreach (var pt in rightPoints)
{
    Draw(new Sphere(radius: 4).Translate(pt), Color.Red);
}

// LINQ: filter only right-side edges and bridge with struts
var rightEdges = cube.GetBoundingEdges()
    .Where(e => e.Start.X > 0 && e.End.X > 0);

foreach (var (start, end) in rightEdges)
{
    Draw(CylinderBetween(start, end, radius: 1.5), Color.Yellow);
}

// LINQ: Draw projected collection directly
var topSpheres = cube.GetPoints()
    .Where(p => p.Z > 0)
    .Select(p => new Sphere(2.5).Translate(p));
Draw(topSpheres, Color.Cyan);

Draw(cube, Color.Gray);
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Empty, "LINQ script should compile with 0 errors");
        Assert.That(compiled, Is.Not.Null);

        dynamic dyn = compiled;
        dyn.Build();
        Assert.That((string)dyn.LastError, Is.Null.Or.Empty, "Script with LINQ should execute with no runtime errors");

        var bounds = dyn.GetBounds();
        Assert.That(bounds, Is.Not.Null);
        Assert.That(bounds.XSize, Is.GreaterThan(0));
    }
}
