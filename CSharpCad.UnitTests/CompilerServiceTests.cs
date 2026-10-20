using CSharpCAD;
using NUnit.Framework;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using MatterHackers.Agg.UI;

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
        Assert.That(helpWin.DialogCard, Is.Not.Null);
        Assert.That(helpWin.Title, Does.Contain("Cheat Sheet & Reference"));

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

        // Test Maximize toggle
        bool initialMax = helpWin.Maximized;
        helpWin.ToggleMaximize();
        Assert.That(helpWin.Maximized, Is.EqualTo(!initialMax));
        helpWin.ToggleMaximize();
        Assert.That(helpWin.Maximized, Is.EqualTo(initialMax));
    }

    [Test]
    public void TestHelpWindow3DViewportPresenceAndBounds()
    {
        var helpWin = new HelpWindow((snippet, isExample) => { }, () => { });
        helpWin.PerformLayout();

        TrackballTumbleView trackView = null;
        void FindTrackView(MatterHackers.Agg.UI.GuiWidget w)
        {
            if (w is TrackballTumbleView tv) trackView = tv;
            foreach (var c in w.Children) FindTrackView(c);
        }
        FindTrackView(helpWin);
        Assert.That(trackView, Is.Not.Null, "HelpWindow should contain a TrackballTumbleView (Interactive3DViewport)");
        Assert.That(trackView.Visible, Is.True, "3D Viewport should be visible");
        Assert.That(trackView.Width, Is.GreaterThan(0), "3D Viewport width should be positive");
        Assert.That(trackView.Height, Is.GreaterThanOrEqualTo(300), "3D Viewport height should be >= 300");

        // Verify Home button exists in HelpWindow
        bool foundHomeBtn = false;
        void FindHomeBtn(MatterHackers.Agg.UI.GuiWidget w)
        {
            if (w is HelpWindow.StyledButton btn && btn.Children.Count > 0 && btn.Children[0] is MatterHackers.Agg.UI.TextWidget tw && tw.Text.Contains("Home"))
            {
                foundHomeBtn = true;
            }
            foreach (var c in w.Children) FindHomeBtn(c);
        }
        FindHomeBtn(helpWin);
        Assert.That(foundHomeBtn, Is.True, "HelpWindow should have a 'Home' button");
    }

    [Test]
    public void TestMainWindowHomeButtonAndCentering()
    {
        var mainWin = new MainWindow(false);
        mainWin.PerformLayout();

        // Verify Home button exists in MainWindow viewport HUD
        bool foundHomeBtn = false;
        void FindHomeBtn(MatterHackers.Agg.UI.GuiWidget w)
        {
            if (w is HelpWindow.StyledButton btn && btn.Children.Count > 0 && btn.Children[0] is MatterHackers.Agg.UI.TextWidget tw && tw.Text.Contains("Home"))
            {
                foundHomeBtn = true;
            }
            foreach (var c in w.Children) FindHomeBtn(c);
        }
        FindHomeBtn(mainWin);
        Assert.That(foundHomeBtn, Is.True, "MainWindow should have a 'Home' button in viewport HUD");

        // Test CenterView execution
        mainWin.CenterView();
        Assert.That(mainWin.trackBallWidget.World.Scale, Is.GreaterThan(0));
    }

    [Test]
    public void TestMainWindowKeepsPrevious3DObjectOnError()
    {
        var mainWin = new MainWindow(false);
        mainWin.PerformLayout();

        // 1. Initially compiles valid default script
        var initialObj = mainWin.ClassRef;
        Assert.That(initialObj, Is.Not.Null, "Initial script should compile to a valid 3D object");

        // 2. Introduce a syntax/compiler error
        mainWin.SetScriptAndCompile("this is completely invalid syntax !!! ;;;;");
        var objAfterSyntaxError = mainWin.ClassRef;
        Assert.That(objAfterSyntaxError, Is.Not.Null, "3D object should not become null on compile error");
        Assert.That(ReferenceEquals(initialObj, objAfterSyntaxError), Is.True, "Previous 3D object must be retained on compile error");

        // 3. Compile another valid script
        mainWin.SetScriptAndCompile("var s = new Sphere(15); Draw(s);");
        var sphereObj = mainWin.ClassRef;
        Assert.That(sphereObj, Is.Not.Null);
        Assert.That(ReferenceEquals(initialObj, sphereObj), Is.False, "New valid script should update 3D object");

        // 4. Introduce a runtime/build error (e.g. division by zero or exception during execution)
        mainWin.SetScriptAndCompile("int zero = 0; int x = 10 / zero; Draw(new Box(x, x, x));");
        var objAfterRuntimeError = mainWin.ClassRef;
        Assert.That(objAfterRuntimeError, Is.Not.Null, "3D object should not become null on runtime error");
        Assert.That(ReferenceEquals(sphereObj, objAfterRuntimeError), Is.True, "Previous 3D object must be retained on runtime error");
    }

    [Test]
    public void TestHelpWindowKeyHandlingAndClipboard()
    {
        bool closed = false;
        var helpWin = new HelpWindow((s, r) => { }, () => { closed = true; });

        // Escape key should NOT close window
        closed = false;
        var escKey = new MatterHackers.Agg.UI.KeyEventArgs(MatterHackers.Agg.UI.Keys.Escape);
        helpWin.OnKeyDown(escKey);
        Assert.That(closed, Is.False, "Escape key should not close window");

        // F1 key should close window
        closed = false;
        var f1Key = new MatterHackers.Agg.UI.KeyEventArgs(MatterHackers.Agg.UI.Keys.F1);
        helpWin.OnKeyDown(f1Key);
        Assert.That(closed, Is.True, "F1 key should close window");
        Assert.That(f1Key.Handled, Is.True);

        // CopyToClipboard should execute without throwing
        Assert.DoesNotThrow(() => HelpWindow.CopyToClipboard("var box = new Box(10, 10, 10);"));
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

    [Test]
    public void TestTrackballTumbleViewMouseMapping()
    {
        var world = new MatterHackers.VectorMath.WorldView(800, 600);
        var parentWidget = new MatterHackers.Agg.UI.GuiWidget();
        parentWidget.LocalBounds = new MatterHackers.Agg.RectangleDouble(0, 0, 800, 600);
        var trackView = new TrackballTumbleView(world, parentWidget);
        parentWidget.AddChild(trackView);

        // Left mouse click should activate Translation (Pan)
        var leftClick = new MatterHackers.Agg.UI.MouseEventArgs(MatterHackers.Agg.UI.MouseButtons.Left, 1, 100, 100, 0);
        trackView.OnMouseDown(leftClick);
        Assert.That(trackView.TrackBallController.CurrentTrackingType, Is.EqualTo(MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Translation), "Left click must trigger Translation (Pan)");
        trackView.OnMouseUp(leftClick);

        // Right mouse click should activate Rotation
        var rightClick = new MatterHackers.Agg.UI.MouseEventArgs(MatterHackers.Agg.UI.MouseButtons.Right, 1, 100, 100, 0);
        trackView.OnMouseDown(rightClick);
        Assert.That(trackView.TrackBallController.CurrentTrackingType, Is.EqualTo(MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Rotation), "Right click must trigger Rotation");
        trackView.OnMouseUp(rightClick);
    }

    [Test]
    public void TestCsgObjectInspectionProperties()
    {
        var box = new MatterHackers.Csg.Solids.Box(20, 30, 10);
        Assert.That(box.Width, Is.EqualTo(20));
        Assert.That(box.Depth, Is.EqualTo(30));
        Assert.That(box.Height, Is.EqualTo(10));

        Assert.That(box.Center.X, Is.EqualTo(0).Within(1e-4));
        Assert.That(box.Center.Y, Is.EqualTo(0).Within(1e-4));
        Assert.That(box.Center.Z, Is.EqualTo(0).Within(1e-4));

        Assert.That(box.TopCenter.Z, Is.EqualTo(5).Within(1e-4));
        Assert.That(box.BottomCenter.Z, Is.EqualTo(-5).Within(1e-4));
        Assert.That(box.LeftCenter.X, Is.EqualTo(-10).Within(1e-4));
        Assert.That(box.RightCenter.X, Is.EqualTo(10).Within(1e-4));
        Assert.That(box.FrontCenter.Y, Is.EqualTo(-15).Within(1e-4));
        Assert.That(box.BackCenter.Y, Is.EqualTo(15).Within(1e-4));

        var corners = box.GetCorners();
        Assert.That(corners.Length, Is.EqualTo(8));

        var boundingEdges = box.GetBoundingEdges();
        Assert.That(boundingEdges.Length, Is.EqualTo(12));

        var uniqueVerts = box.GetUniqueVertices();
        Assert.That(uniqueVerts.Count, Is.EqualTo(8));

        var edges = box.GetEdges();
        Assert.That(edges.Count, Is.GreaterThanOrEqualTo(12));
    }

    [Test]
    public void TestCylinderCreateBetween()
    {
        var p1 = new MatterHackers.VectorMath.Vector3(0, 0, 0);
        var p2 = new MatterHackers.VectorMath.Vector3(0, 0, 20);
        var cyl = MatterHackers.Csg.Solids.Cylinder.CreateBetween(p1, p2, 5, 24);
        Assert.That(cyl, Is.Not.Null);

        Assert.That(cyl.Min.Z, Is.EqualTo(0).Within(1e-3));
        Assert.That(cyl.Max.Z, Is.EqualTo(20).Within(1e-3));
        Assert.That(cyl.Center.Z, Is.EqualTo(10).Within(1e-3));
    }

    [Test]
    public void TestScriptWithCubePointsAndEdgesCompilesAndExecutes()
    {
        string script = @"
var cube = new Box(20, 20, 20);
CsgObject joints = null;
foreach (var pt in cube.GetUniqueVertices())
{
    var s = new Sphere(2).Translate(pt);
    joints = (joints == null) ? s : joints + s;
}

CsgObject struts = null;
foreach (var (start, end) in cube.GetBoundingEdges())
{
    var strut = CylinderBetween(start, end, 1);
    struts = (struts == null) ? strut : struts + strut;
}

var topCap = new Cylinder(radius: 4, height: 10).Translate(cube.TopCenter);
Draw(cube, RGB(50, 100, 200));
Draw(joints, Color.Orange);
Draw(struts, Color.Yellow);
Draw(topCap, Color.Green);
";
        var compiler = new CompilerService();
        var result = compiler.Compile(script, out var errors);
        Assert.That(errors, Is.Empty);
        Assert.That(result, Is.Not.Null);

        var buildMethod = result.GetType().GetMethod("Build");
        Assert.That(buildMethod, Is.Not.Null);
        buildMethod.Invoke(result, null);

        var lastErrorProp = result.GetType().GetProperty("LastError");
        var lastError = lastErrorProp?.GetValue(result) as string;
        Assert.That(lastError, Is.Null, $"Script execution had error: {lastError}");
    }

    [Test]
    public void TestChainExampleScriptCompilesAndExecutes()
    {
        string script = @"
var ring1 = new Torus(innerRadius: 10, outerRadius: 20);
var ring2 = new Rotate(
    new Translate(new Torus(innerRadius: 10, outerRadius: 20), 20, 20, 0),
    x: MathHelper.DegreesToRadians(90), z: MathHelper.DegreesToRadians(90));

Draw(ring1, Color.Black);
Draw(ring2, Color.Indigo);
";
        var compiler = new CompilerService();
        var result = compiler.Compile(script, out var errors);
        Assert.That(errors, Is.Empty, "Chain script should compile with no errors");
        Assert.That(result, Is.Not.Null);

        var buildMethod = result.GetType().GetMethod("Build");
        Assert.That(buildMethod, Is.Not.Null);
        buildMethod.Invoke(result, null);

        var lastErrorProp = result.GetType().GetProperty("LastError");
        var lastError = lastErrorProp?.GetValue(result) as string;
        Assert.That(lastError, Is.Null, $"Chain script execution had error: {lastError}");
    }

    private class TestClipboard : ISystemClipboard
    {
        private string _text = "";
        public bool ContainsFileDropList => false;
        public bool ContainsImage => false;
        public bool ContainsText => !string.IsNullOrEmpty(_text);
        public System.Collections.Specialized.StringCollection GetFileDropList() => new();
        public MatterHackers.Agg.Image.ImageBuffer GetImage() => null!;
        public string GetText() => _text;
        public void SetText(string text) => _text = text;
        public void SetImage(MatterHackers.Agg.Image.ImageBuffer imageBuffer) { }
    }

    [Test]
    public void TestReadOnlyCodeBoxWidgetIsReadOnlyAndCopiesText()
    {
        Clipboard.SetSystemClipboard(new TestClipboard());

        string sampleCode = "var b = new Box(10, 20, 30);\nDraw(b, Color.Red);";
        var codeBox = new ReadOnlyCodeBoxWidget(sampleCode, fontSize: 10, maxDisplayHeight: 200);

        Assert.That(codeBox.Editor, Is.Not.Null);
        Assert.That(codeBox.Editor.ReadOnly, Is.True);
        Assert.That(codeBox.Text, Is.EqualTo(sampleCode));

        // Test Ctrl+C with no selection copies full text
        var keyEvent = new KeyEventArgs(Keys.C | Keys.Control);
        codeBox.Editor.InternalTextEditWidget.OnKeyDown(keyEvent);
        Assert.That(keyEvent.Handled, Is.True);
        Assert.That(Clipboard.Instance.GetText(), Is.EqualTo(sampleCode));

        // Test Ctrl+C with selection copies only the selection
        codeBox.Editor.InternalTextEditWidget.SetSelection(0, 2);
        var keyEvent2 = new KeyEventArgs(Keys.C | Keys.Control);
        codeBox.Editor.InternalTextEditWidget.OnKeyDown(keyEvent2);
        Assert.That(keyEvent2.Handled, Is.True);
        Assert.That(Clipboard.Instance.GetText(), Is.EqualTo("var"));
    }

    [Test]
    public void TestEditorViewsHaveScrollbarEnabled()
    {
        // 1. SyntaxTextEditWidget has AutoScroll and VerticalScrollBar configured
        var editor = new SyntaxTextEditWidget("line1\nline2\nline3", 16);
        Assert.That(editor.AutoScroll, Is.True);
        Assert.That(editor.Multiline, Is.True);
        Assert.That(editor.VerticalScrollBar, Is.Not.Null);
        Assert.That(editor.VerticalScrollBar.Show, Is.EqualTo(ScrollBar.ShowState.WhenRequired));

        // 2. ReadOnlyCodeBoxWidget editor has scrollbar enabled
        var readOnlyBox = new ReadOnlyCodeBoxWidget("line1\nline2\nline3", fontSize: 10, maxDisplayHeight: 200);
        Assert.That(readOnlyBox.Editor.AutoScroll, Is.True);
        Assert.That(readOnlyBox.Editor.VerticalScrollBar, Is.Not.Null);
        Assert.That(readOnlyBox.Editor.VerticalScrollBar.Show, Is.EqualTo(ScrollBar.ShowState.WhenRequired));

        // 3. CodeEditorWidget wraps editor with scrollbar
        var codeEditor = new CodeEditorWidget("line1\nline2", 14);
        Assert.That(codeEditor.Editor.AutoScroll, Is.True);
        Assert.That(codeEditor.Editor.VerticalScrollBar.Show, Is.EqualTo(ScrollBar.ShowState.WhenRequired));
    }

    [Test]
    public void TestAutocompleteClickAppliesCompletion()
    {
        var editorWidget = new CodeEditorWidget("var x = new Cyl", 14);
        editorWidget.Editor.InternalTextEditWidget.CharIndexToInsertBefore = editorWidget.Editor.Text.Length;

        var autoWidget = FindChild<AutocompleteWidget>(editorWidget);
        Assert.That(autoWidget, Is.Not.Null);

        var item = new RoslynCompletionItem
        {
            DisplayText = "Cylinder",
            InsertionText = "Cylinder",
            Kind = "class",
            InlineDescription = "Class"
        };

        autoWidget.Populate(new List<RoslynCompletionItem> { item }, "Cyl");
        Assert.That(autoWidget.Visible, Is.True);

        var listItem = FindChild<AutocompleteListItem>(autoWidget);
        Assert.That(listItem, Is.Not.Null);

        // Click on the autocomplete item via MouseUp
        listItem.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        // Completion should be applied: "var x = new Cylinder", and popup hidden
        Assert.That(editorWidget.Editor.Text, Is.EqualTo("var x = new Cylinder"));
        Assert.That(autoWidget.Visible, Is.False);
    }

    [Test]
    public async Task TestCylinderSignaturesOnTypingNewCylinder()
    {
        var engine = new RoslynCompletionEngine();
        var sigs = await engine.GetSignaturesAsync("Cylinder", isConstructor: true, argIndex: 0, "var c = new Cylinder(", 20);

        Assert.That(sigs, Is.Not.Null);
        Assert.That(sigs.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(sigs.All(s => s.Kind == "constructor"), Is.True);
        Assert.That(sigs.Any(s => s.DisplayText.Contains("double radius, double height")), Is.True);
        Assert.That(sigs.Any(s => s.DisplayText.Contains("Line line, double radius")), Is.True);
        Assert.That(sigs.Any(s => s.DisplayText.Contains("double radius1, double radius2, double height")), Is.True);
    }

    [Test]
    public async Task TestCylinderSignaturesFuzzyMatchOnTypingNewCylynder()
    {
        var engine = new RoslynCompletionEngine();
        // User typed "Cylynder" with a typo ('y' instead of 'i')
        var sigs = await engine.GetSignaturesAsync("Cylynder", isConstructor: true, argIndex: 0, "var c = new Cylynder(", 21);

        Assert.That(sigs, Is.Not.Null);
        Assert.That(sigs.Count, Is.GreaterThanOrEqualTo(3));
        Assert.That(sigs[0].DisplayText, Does.StartWith("Cylinder("));
    }

    [Test]
    public async Task TestApplySignatureCorrectsTypoAndInsertsParameters()
    {
        var engine = new RoslynCompletionEngine();
        var sigs = await engine.GetSignaturesAsync("Cylynder", isConstructor: true, argIndex: 0, "var c = new Cylynder(", 21);

        var editorWidget = new CodeEditorWidget("var c = new Cylynder(", 14);
        editorWidget.Editor.InternalTextEditWidget.CharIndexToInsertBefore = editorWidget.Editor.Text.Length;

        var autoWidget = FindChild<AutocompleteWidget>(editorWidget);
        Assert.That(autoWidget, Is.Not.Null);

        autoWidget.Populate(sigs, "");
        Assert.That(autoWidget.Visible, Is.True);

        var listItem = FindChild<AutocompleteListItem>(autoWidget);
        Assert.That(listItem, Is.Not.Null);

        // Click on the first signature item
        listItem.OnMouseUp(new MouseEventArgs(MouseButtons.Left, 1, 5, 5, 0));

        // Should replace Cylynder with Cylinder and insert radius, height)
        Assert.That(editorWidget.Editor.Text, Is.EqualTo("var c = new Cylinder(radius, height)"));
        Assert.That(autoWidget.Visible, Is.False);
    }

    [Test]
    public void TestAutoDocumentationServiceDiscoversShapesAndPrimitives()
    {
        var items = AutoDocumentationService.GetDocumentedItems();
        Assert.That(items, Is.Not.Null);
        Assert.That(items.Count, Is.GreaterThanOrEqualTo(16));

        var names = items.Select(i => i.Name).ToList();
        Assert.That(names, Does.Contain("Box"));
        Assert.That(names, Does.Contain("Cylinder"));
        Assert.That(names, Does.Contain("Sphere"));
        Assert.That(names, Does.Contain("Torus"));
        Assert.That(names, Does.Contain("Circle"));
        Assert.That(names, Does.Contain("Rectangle"));
        Assert.That(names, Does.Contain("Square"));
        Assert.That(names, Does.Contain("Line"));
        Assert.That(names, Does.Contain("Point"));
        Assert.That(names, Does.Contain("Path"));
        Assert.That(names, Does.Contain("Translate"));
        Assert.That(names, Does.Contain("Rotate"));
        Assert.That(names, Does.Contain("Scale"));
        Assert.That(names, Does.Contain("Union"));
        Assert.That(names, Does.Contain("Difference"));
        Assert.That(names, Does.Contain("Intersection"));
        Assert.That(names, Does.Contain("Draw"));
        Assert.That(names, Does.Contain("Extrude"));
        Assert.That(names, Does.Contain("TwistExtrude"));
        Assert.That(names, Does.Contain("Revolve"));
        Assert.That(names, Does.Contain("Loft"));

        // Verify that summaries and mesh builders work for each
        foreach (var item in items)
        {
            Assert.That(item.Summary, Is.Not.Null.And.Not.Empty, $"Item {item.Name} should have a summary");
            if (item.BuildMeshes != null)
            {
                var meshes = item.BuildMeshes();
                Assert.That(meshes, Is.Not.Null, $"Mesh builder for {item.Name} returned null");
                Assert.That(meshes.Count, Is.GreaterThan(0), $"Mesh builder for {item.Name} returned empty mesh list");
                Assert.That(meshes[0].mesh, Is.Not.Null, $"Mesh for {item.Name} was null");
            }
        }
    }

    [Test]
    public void TestAutoDocumentationServiceGeneratesMarkdown()
    {
        string md = AutoDocumentationService.GenerateMarkdown();
        Assert.That(md, Is.Not.Null.And.Not.Empty);
        Assert.That(md, Does.Contain("# OpenSharpCAD API Documentation"));
        Assert.That(md, Does.Contain("### Box"));
        Assert.That(md, Does.Contain("### Cylinder"));
        Assert.That(md, Does.Contain("### Sphere"));
        Assert.That(md, Does.Contain("### Torus"));
        Assert.That(md, Does.Contain("### Circle"));
        Assert.That(md, Does.Contain("### Rectangle"));
        Assert.That(md, Does.Contain("### Translate"));
        Assert.That(md, Does.Contain("### Rotate"));
        Assert.That(md, Does.Contain("### Scale"));
        Assert.That(md, Does.Contain("### Union"));
        Assert.That(md, Does.Contain("### Difference"));
        Assert.That(md, Does.Contain("### Intersection"));
        Assert.That(md, Does.Contain("### Draw"));
        Assert.That(md, Does.Contain("### Extrude"));

        string tempPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "test_api_doc.md");
        AutoDocumentationService.ExportMarkdownFile(tempPath);
        Assert.That(System.IO.File.Exists(tempPath), Is.True);
        System.IO.File.Delete(tempPath);

        string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        string apiDocPath = System.IO.Path.Combine(repoRoot, "API_DOCUMENTATION.md");
        AutoDocumentationService.ExportMarkdownFile(apiDocPath);
        Assert.That(System.IO.File.Exists(apiDocPath), Is.True);
    }

    [Test]
    public void TestAutoDocumentationServiceGeneratesHtml()
    {
        string html = AutoDocumentationService.GenerateHtml();
        Assert.That(html, Is.Not.Null.And.Not.Empty);
        Assert.That(html, Does.Contain("<!DOCTYPE html>"));
        Assert.That(html, Does.Contain("OpenSharpCAD — API Reference"));
        Assert.That(html, Does.Contain("api-sidebar"));
        Assert.That(html, Does.Contain("api-main-content"));
        Assert.That(html, Does.Contain("Box"));
        Assert.That(html, Does.Contain("Cylinder"));
        Assert.That(html, Does.Contain("Sphere"));
        Assert.That(html, Does.Contain("Torus"));
        Assert.That(html, Does.Contain("Translate"));
        Assert.That(html, Does.Contain("Union"));
        Assert.That(html, Does.Contain("Draw"));

        Assert.That(html, Does.Contain("token-keyword"));
        Assert.That(html, Does.Contain("token-type"));
        Assert.That(html, Does.Contain("token-method"));
        Assert.That(html, Does.Contain("token-number"));
        Assert.That(html, Does.Contain("token-operator"));

        string repoRoot = System.IO.Path.GetFullPath(System.IO.Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../.."));
        string apiHtmlPath = System.IO.Path.Combine(repoRoot, "doc/api.html");
        AutoDocumentationService.ExportHtmlFile(apiHtmlPath);
        Assert.That(System.IO.File.Exists(apiHtmlPath), Is.True);
        Assert.That(new System.IO.FileInfo(apiHtmlPath).Length, Is.GreaterThan(1000));
        string savedHtml = System.IO.File.ReadAllText(apiHtmlPath);
        Assert.That(savedHtml, Does.Contain("token-keyword"));
        Assert.That(savedHtml, Does.Contain("token-type"));
    }

    private static T? FindChild<T>(GuiWidget widget) where T : GuiWidget
    {
        foreach (var child in widget.Children)
        {
            if (child is T match) return match;
            var found = FindChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    [Test]
    public void TestSecurityWhitelistBlocksSystemIO()
    {
        string maliciousScript = @"
var path = ""test.txt"";
var text = System.IO.File.ReadAllText(path);
Draw(new Box(10, 10, 10));
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(maliciousScript, out errors);

        Assert.That(compiled, Is.Null, "Compilation of System.IO should fail because System.IO.FileSystem is not whitelisted");
        Assert.That(errors, Is.Not.Empty, "Should report diagnostic error for System.IO");
        Assert.That(errors.Any(e => e.Id == "CS0234" || e.GetMessage().Contains("System.IO")), Is.True);
    }

    [Test]
    public void TestSecurityWhitelistBlocksProcessExecution()
    {
        string maliciousScript = @"
System.Diagnostics.Process.Start(""calc.exe"");
Draw(new Box(10, 10, 10));
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(maliciousScript, out errors);

        Assert.That(compiled, Is.Null, "Compilation of System.Diagnostics.Process should fail because Process assembly is not whitelisted");
        Assert.That(errors, Is.Not.Empty, "Should report diagnostic error for Process");
        Assert.That(errors.Any(e => e.Id == "CS0234" || e.GetMessage().Contains("Process")), Is.True);
    }

    [Test]
    public void TestSecurityWhitelistBlocksNetworkHttpClient()
    {
        string maliciousScript = @"
var client = new System.Net.Http.HttpClient();
Draw(new Box(10, 10, 10));
";
        ICompilerService compiler = new CompilerService();
        List<Diagnostic> errors;
        var compiled = compiler.Compile(maliciousScript, out errors);

        Assert.That(compiled, Is.Null, "Compilation of HttpClient should fail because System.Net.Http is not whitelisted");
        Assert.That(errors, Is.Not.Empty, "Should report diagnostic error for HttpClient");
        Assert.That(errors.Any(e => e.Id == "CS0234" || e.GetMessage().Contains("HttpClient") || e.GetMessage().Contains("System.Net")), Is.True);
    }

    [Test]
    public void TestOpenScadAnimationScriptCompilesAndExecutes()
    {
        string examplePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../examples/openscad.cs"));
        if (!File.Exists(examplePath))
        {
            examplePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../examples/openscad.cs"));
        }
        if (!File.Exists(examplePath))
        {
            examplePath = Path.Combine(Directory.GetCurrentDirectory(), "examples/openscad.cs");
        }
        Assert.That(File.Exists(examplePath), Is.True, $"examples/openscad.cs must exist at {examplePath}");
        string script = File.ReadAllText(examplePath);

        ICompilerService compiler = new CompilerService();
        var result = compiler.Compile(script, out var errors);
        Assert.That(errors, Is.Empty, $"openscad.cs should compile with 0 errors. Errors: {string.Join("; ", errors.Select(e => e.GetMessage()))}");
        Assert.That(result, Is.Not.Null);

        // Execute at t=0
        var buildMethod = result.GetType().GetMethod("Build");
        Assert.That(buildMethod, Is.Not.Null);
        buildMethod.Invoke(result, null);

        var lastErrorProp = result.GetType().GetProperty("LastError");
        var lastError = lastErrorProp?.GetValue(result) as string;
        Assert.That(lastError, Is.Null, $"Execution at t=0 had error: {lastError}");

        // Execute animation across multiple time steps [0.0 .. 1.0]
        var buildTimeMethod = result.GetType().GetMethod("BuildTime");
        Assert.That(buildTimeMethod, Is.Not.Null);

        double[] testTimes = new double[] { 0.0, 0.1, 0.25, 0.5, 0.75, 0.9, 1.0 };
        foreach (var t in testTimes)
        {
            buildTimeMethod.Invoke(result, new object[] { t });
            var err = lastErrorProp?.GetValue(result) as string;
            Assert.That(err, Is.Null, $"Execution at t={t} had error: {err}");
        }

        var timeProp = result.GetType().GetProperty("t");
        Assert.That(timeProp?.GetValue(result), Is.EqualTo(1.0));

        // Test PrecalculateAnimation for whole-animation calculation
        var precalcMethod = result.GetType().GetMethod("PrecalculateAnimation");
        Assert.That(precalcMethod, Is.Not.Null);
        int progressCalls = 0;
        Action<int, int> onProgress = (step, total) => progressCalls++;
        precalcMethod.Invoke(result, new object[] { 10, onProgress });
        Assert.That(progressCalls, Is.EqualTo(10));

        var cachedCountProp = result.GetType().GetProperty("CachedFrameCount");
        Assert.That(cachedCountProp, Is.Not.Null);
        Assert.That((int)cachedCountProp.GetValue(result), Is.EqualTo(10));

        // Test BuildFrame retrieves cached frame without errors
        var buildFrameMethod = result.GetType().GetMethod("BuildFrame");
        Assert.That(buildFrameMethod, Is.Not.Null);
        buildFrameMethod.Invoke(result, new object[] { 0, 10 });
        var frameErr = lastErrorProp?.GetValue(result) as string;
        Assert.That(frameErr, Is.Null);
        Assert.That(timeProp?.GetValue(result), Is.EqualTo(0.0));

        var cachedMeshesField = result.GetType().GetField("_cachedMeshes", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var cachedMeshesList = cachedMeshesField?.GetValue(result) as System.Collections.IList;
        Assert.That(cachedMeshesList, Is.Not.Null);
        Console.WriteLine($"Total cached meshes at frame 0: {cachedMeshesList.Count}");
        foreach (var item in cachedMeshesList)
        {
            Console.WriteLine($"Mesh item: {item}");
        }

        // Test ClearAnimationCache
        var clearCacheMethod = result.GetType().GetMethod("ClearAnimationCache");
        Assert.That(clearCacheMethod, Is.Not.Null);
        clearCacheMethod.Invoke(result, null);
        Assert.That((int)cachedCountProp.GetValue(result), Is.EqualTo(0));

        // Test frames of animation
        for (int frame = 0; frame < 50; frame++)
        {
            double curT = (double)frame / 50.0;
            buildTimeMethod.Invoke(result, new object[] { curT });
            var err = lastErrorProp?.GetValue(result) as string;
            if (err != null)
            {
                Console.WriteLine($"FRAME {frame} (t={curT}) ERROR: {err}");
            }
            var meshes = cachedMeshesField?.GetValue(result) as System.Collections.IList;
            int redCount = 0;
            int greenCount = 0;
            if (meshes != null)
            {
                for (int m = 0; m < meshes.Count; m++)
                {
                    string itemStr = meshes[m]?.ToString() ?? "";
                    if (itemStr.Contains("FF5555")) redCount++;
                    if (itemStr.Contains("50FA7B")) greenCount++;
                }
            }
            if (redCount == 0 || greenCount == 0)
            {
                Console.WriteLine($"FRAME {frame} (t={curT}) MISSING: red={redCount}, green={greenCount}, total={meshes?.Count}");
            }
        }
    }

    [Test]
    public void TestLogoExampleScriptCompilesAndExecutes()
    {
        string examplePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../../examples/logo.cs"));
        if (!File.Exists(examplePath))
        {
            examplePath = Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "../../../examples/logo.cs"));
        }
        if (!File.Exists(examplePath))
        {
            examplePath = Path.Combine(Directory.GetCurrentDirectory(), "examples/logo.cs");
        }
        Assert.That(File.Exists(examplePath), Is.True, $"examples/logo.cs must exist at {examplePath}");
        string script = File.ReadAllText(examplePath);

        ICompilerService compiler = new CompilerService();
        var result = compiler.Compile(script, out var errors);
        Assert.That(errors, Is.Empty, $"logo.cs should compile with 0 errors. Errors: {string.Join("; ", errors.Select(e => e.GetMessage()))}");
        Assert.That(result, Is.Not.Null);

        var buildMethod = result.GetType().GetMethod("Build");
        Assert.That(buildMethod, Is.Not.Null);
        buildMethod.Invoke(result, null);

        var lastErrorProp = result.GetType().GetProperty("LastError");
        var lastError = lastErrorProp?.GetValue(result) as string;
        Assert.That(lastError, Is.Null, $"Execution of logo.cs had error: {lastError}");
    }

    [Test]
    public void TestSecurityWhitelistAllowsPermittedAssemblies()
    {
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.Private.CoreLib"), Is.True);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.Runtime"), Is.True);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.Collections"), Is.True);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.Linq"), Is.True);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("MatterHackers.Csg"), Is.True);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("VectorMath"), Is.True);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("PolygonMesh"), Is.True);

        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.IO.FileSystem"), Is.False);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.Diagnostics.Process"), Is.False);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.Net.Http"), Is.False);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("System.Net.Sockets"), Is.False);
        Assert.That(DynCode.CodeHelper.IsAllowedAssembly("Microsoft.CodeAnalysis"), Is.False);
    }
}


