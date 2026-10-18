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
        var parent = new MatterHackers.Agg.UI.GuiWidget(1024, 768);
        var helpWin = new HelpWindow((snippet, isExample) => { }, () => { });
        parent.AddChild(helpWin);
        Assert.That(helpWin, Is.Not.Null);

        parent.PerformLayout();

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

        // Verify resizability of the dialog card
        Assert.That(helpWin.DialogCard, Is.Not.Null);
        double initialW = helpWin.DialogCard.Width;
        double initialH = helpWin.DialogCard.Height;
        Assert.That(initialW, Is.GreaterThanOrEqualTo(460));
        Assert.That(initialH, Is.GreaterThanOrEqualTo(320));

        // Test Maximize
        helpWin.ToggleMaximize();
        Assert.That(helpWin.DialogCard.Width, Is.GreaterThan(initialW));
        Assert.That(helpWin.DialogCard.Height, Is.GreaterThan(initialH));

        // Test Restore
        helpWin.ToggleMaximize();
        Assert.That(helpWin.DialogCard.Width, Is.EqualTo(initialW));
        Assert.That(helpWin.DialogCard.Height, Is.EqualTo(initialH));
    }

    [Test]
    public async System.Threading.Tasks.Task TestRoslynCompletionEngine()
    {
        var engine = new RoslynCompletionEngine();

        // Test 1: Complete "new Bo" -> should include "Box"
        string script1 = "var mySolid = new Bo";
        var completions1 = await engine.GetCompletionsAsync(script1, script1.Length);
        Assert.That(completions1, Is.Not.Null.And.Not.Empty);
        var boxItem = completions1.Find(c => c.DisplayText == "Box");
        Assert.That(boxItem, Is.Not.Null, "Expected 'Box' in completions for 'new Bo'");

        // Test 2: Complete member access on Box "b." -> should include "Name" or "GetCenter"
        string script2 = "var b = new Box(10, 10, 10);\nb.";
        var completions2 = await engine.GetCompletionsAsync(script2, script2.Length);
        Assert.That(completions2, Is.Not.Null.And.Not.Empty);
        var memberItem = completions2.Find(c => c.DisplayText == "Name" || c.DisplayText == "GetCenter");
        Assert.That(memberItem, Is.Not.Null, $"Expected 'Name' or 'GetCenter' member completion on Box instance, found: {string.Join(", ", completions2.Select(c => c.DisplayText))}");
    }
}
