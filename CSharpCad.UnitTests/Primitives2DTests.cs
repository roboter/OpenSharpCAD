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
}
