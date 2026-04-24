using CSharpCAD;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;

namespace CSharpCad.UnitTests;

[TestFixture]
public class CompilerServiceTests
{
    [Test]
    public void TestCompileValidScript()
    {
        string script = "Draw(new Box(8, 20, 10));";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;

        var result = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Empty, "Should have no compilation errors");
        Assert.That(result, Is.Not.Null, "Should return a compiled object");
    }

    [Test]
    public void TestCompileInvalidScript()
    {
        string script = "InvalidMethod();";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;

        var result = compiler.Compile(script, out errors);

        Assert.That(errors, Is.Not.Empty, "Should have compilation errors");
        Assert.That(result, Is.Null, "Should return null for invalid script");
    }
}
