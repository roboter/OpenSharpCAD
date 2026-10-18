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

    [Test]
    public void TestHelpWindowConstructAndTabs()
    {
        // Verify that HelpWindow builds all tabs and layouts without throwing any layout exceptions
        var helpWin = new HelpWindow((snippet, isExample) => { }, () => { });
        Assert.That(helpWin, Is.Not.Null);

        helpWin.LocalBounds = new MatterHackers.Agg.RectangleDouble(0, 0, 1024, 768);
        helpWin.PerformLayout();

        // Count TextWidgets and verify they have valid non-empty text and non-black text color on dark theme
        int textWidgetCount = 0;
        void VerifyWidgets(MatterHackers.Agg.UI.GuiWidget w)
        {
            if (w is MatterHackers.Agg.UI.TextWidget tw)
            {
                textWidgetCount++;
                Assert.That(tw.Text, Is.Not.Null.And.Not.Empty, "TextWidget should have non-empty text");
                Assert.That(tw.Width, Is.GreaterThan(0), $"TextWidget '{tw.Text}' should have positive width");
            }

            foreach (var child in w.Children)
            {
                VerifyWidgets(child);
            }
        }

        VerifyWidgets(helpWin);
        Assert.That(textWidgetCount, Is.GreaterThan(10), "HelpWindow should contain multiple visible TextWidgets");
    }
}
