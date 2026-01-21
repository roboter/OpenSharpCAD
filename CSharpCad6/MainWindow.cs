using System;
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Csg.Operations;
using MatterHackers.VectorMath;

using System.CodeDom.Compiler;
using System.Text;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Solids;
using MatterHackers.RenderOpenGl;
using System.IO;
using Microsoft.CodeAnalysis;

namespace CSharpCAD
{
    public class TrackballTumbleView : TrackballTumbleWidget
    {
        public Action DrawContent;
        public WorldView World;

        public TrackballTumbleView(WorldView world, GuiWidget sourceWidget)
            : base(world, sourceWidget)
        {
            this.World = world;
        }

        public override void OnDraw(Graphics2D graphics2D)
        {
            base.OnDraw(graphics2D);
            if (DrawContent != null)
            {
                GLHelper.SetGlContext(this.World, this.TransformToScreenSpace(LocalBounds), new LightingData()
                {
                    AmbientLight = new float[] { 0.2f, 0.2f, 0.2f, 1.0f }
                });
                DrawContent();
                GLHelper.UnsetGlContext();
            }
        }
    }

    public class MainWindow : SystemWindow
    {
        //private MatterHackers.PolygonMesh.Mesh meshToRender = null;

        private TrackballTumbleView trackBallWidget;
        private Button outputScad;
        private Splitter verticalSplitter;

        private GuiWidget objectEditorView;
        private FlowLayoutWidget objectEditorList;
        private FlowLayoutWidget textSide;
        private TextEditWidget hello;
        private Union rootUnion = new Union("root");
        dynamic classRef;

        public MainWindow(bool renderRayTrace) : base(800, 600)
        {
            //     rootUnion.Add(new Translate(new BoxPrimitive(10, 10, 20), 5, 10, 5));
            //rootUnion.Add(new Box(8, 20, 10));
            //rootUnion.Add(new Cylinder(10, 40));
            //      rootUnion.Add(new Translate(new Sphere(radius: 30), 15, 20, 40)); //not implemented
            var testUnion = new Translate(new Box(10, 10, 20) - new Box(8, 20, 10), 5, 5, 5); //new Difference(
            //  rootUnion.Add(new LinearExtrude(new double[] { 1.1, 2.2, 3.3, 6.3 }, 7));
            //rootUnion.Add(testUnion);
            rootUnion.Box(8, 20, 40);
            rootUnion.Add(new Translate(new BoxPrimitive(10, 10, 20), 5, 10, 5));
            rootUnion.Add(new BoxPrimitive(8, 20, 10));

            verticalSplitter = new Splitter();
            verticalSplitter.SplitterDistance = 400;  // Set left panel width for text editor
            //verticalSplitter.Panel1.BackgroundColor = new Color(173, 216, 230);  // Debug: Panel1 light blue
            //verticalSplitter.Panel2.BackgroundColor = new Color(144, 238, 144);  // Debug: Panel2 light green
            verticalSplitter.SplitterBackground = Color.Red;  // Debug: Splitter bar color
            {
                // panel 1 stuff
                textSide = new FlowLayoutWidget(FlowDirection.TopToBottom);
                textSide.BackgroundColor = Color.Cyan;
                {
                    objectEditorView = new GuiWidget(300, 500);
                    objectEditorList = new FlowLayoutWidget();

                    objectEditorView.BackgroundColor = Color.LightGray;

                    var code = new StringBuilder();
                    code.AppendLine("// Complex CSG Example");
                    code.AppendLine("");
                    code.AppendLine("// 1. Union: A cross shape");
                    code.AppendLine("var cross = new Union();");
                    code.AppendLine("cross.Add(new Box(10, 30, 10));");
                    code.AppendLine("cross.Add(new Box(30, 10, 10));");
                    code.AppendLine("");
                    code.AppendLine("// 2. Difference: Sphere minus Cylinder");
                    code.AppendLine("var sphere = new Sphere(15);");
                    code.AppendLine("var cylinder = new Cylinder(5, 40);");
                    code.AppendLine("var hollowSphere = new Difference(sphere, cylinder);");
                    code.AppendLine("");
                    code.AppendLine("// 3. Intersection: Box intersection Sphere");
                    code.AppendLine("var box = new Box(20, 20, 20);");
                    code.AppendLine("var ball = new Sphere(14);");
                    code.AppendLine("var intersected = new Intersection(box, ball);");
                    code.AppendLine("");
                    code.AppendLine("// Assemble into a scene");
                    code.AppendLine("var scene = new Union();");
                    code.AppendLine("scene.Add(new Translate(cross, -40, 0, 10));");
                    code.AppendLine("scene.Add(new Translate(hollowSphere, 0, 0, 20));");
                    code.AppendLine("scene.Add(new Translate(intersected, 40, 0, 10));");
                    code.AppendLine("scene.Add(new Translate(new Box(120, 50, 2), 0, 0, -1));");
                    code.AppendLine("");
                    code.AppendLine("Draw(scene);");

                    hello = new TextEditWidget(code.ToString().Replace('\r', '\n'));
                    hello.Multiline = true;
                    hello.HAnchor = HAnchor.Stretch;
                    hello.VAnchor = VAnchor.Stretch;
                    hello.TextChanged += Hello_TextChanged;

                    textSide.AddChild(hello);
                    textSide.AnchorAll();
                    objectEditorList.AnchorAll();
                }

                // pannel 2 stuff
                FlowLayoutWidget renderSide = new FlowLayoutWidget(FlowDirection.TopToBottom);
                renderSide.BackgroundColor = Color.Yellow;
                renderSide.AnchorAll();
                {
                    var world = new WorldView(800, 600);
                    trackBallWidget = new TrackballTumbleView(world, renderSide);
                    trackBallWidget.DrawContent = glLightedView_DrawGlContent;
                    renderSide.AddChild(trackBallWidget);
                }
                verticalSplitter.Panel2.AddChild(renderSide);
                verticalSplitter.Panel1.AddChild(textSide);
            }

            AnchorAll();

            verticalSplitter.AnchorAll();

            textSide.AnchorAll();

            trackBallWidget.AnchorAll();

            AddChild(verticalSplitter);

            BackgroundColor = Color.White;
            Compile();
        }

        private void Hello_TextChanged(object sender, EventArgs e)
        {
            Compile();
        }

        private void Compile()
        {
            var compilerService = new CompilerService();
            List<string> errors;

            // The service expects just the content inside Render(), which comes from hello.Text
            var newClassRef = compilerService.Compile(hello.Text, out errors);

            if (errors.Count > 0)
            {
                StringBuilder sberror = new StringBuilder();
                foreach (var error in errors)
                {
                    sberror.AppendLine(error);
                }

                // Parse the first error to find the line number
                // Format example: (19,28): error CS0012: ...
                // The line number in the error message matches the line in the "class wrapper" code, not necessarily the editor text.
                // However, we constructed the wrapper such that the user code starts at specific line.
                // We need to map wrapper line to editor line, or better yet, since we know exactly how many lines we added:
                // CompilerService adds lines before user code. Checking CompilerService.cs...
                // It adds 31 lines before 'scriptSource'.
                // So UserLine = ErrorLine - 31.

                if (errors.Count > 0)
                {
                    var error = errors[0];
                    if (error.StartsWith("("))
                    {
                        int endIndex = error.IndexOf(",");
                        if (endIndex > 1)
                        {
                            string lineStr = error.Substring(1, endIndex - 1);
                            if (int.TryParse(lineStr, out int errorLine))
                            {
                                int editorLine = errorLine - 14; // Offset from CompilerService
                                HighlightLine(editorLine);
                            }
                        }
                    }
                }

                return;
            }

            classRef = newClassRef;
            trackBallWidget.Invalidate();
        }

        private void HighlightLine(int lineNumberOneBased)
        {
            //if (lineNumberOneBased < 1) return;

            //string text = hello.Text;
            //string[] lines = text.Split('\n');

            //if (lineNumberOneBased > lines.Length) return;

            //int startIndex = 0;
            //for (int i = 0; i < lineNumberOneBased - 1; i++)
            //{
            //    startIndex += lines[i].Length + 1; // +1 for \n which replace replaced \r with
            //}

            //int length = lines[lineNumberOneBased - 1].Length;

            //// Select the line
            //hello.InternalTextEditWidget.SetSelection(startIndex, startIndex + length - 1);
        }
        private void LogException(Exception ex)
        {
            try
            {
                string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
                string logDir = Path.Combine(projectRoot, "logs");
                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                string logPath = Path.Combine(logDir, "errors.log");
                string logMessage = $"[{DateTime.Now}] {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{new string('-', 30)}{Environment.NewLine}";
                File.AppendAllText(logPath, logMessage);
            }
            catch { }
        }

        private void glLightedView_DrawGlContent()
        {
            try
            {
                classRef?.Render();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        private MatterHackers.PolygonMesh.Mesh Pyramid()
        {
            MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();
            mesh.Vertices.Add(new Vector3Float(1, 0, 1));  // V0
            mesh.Vertices.Add(new Vector3Float(1, 0, -1)); // v1
            mesh.Vertices.Add(new Vector3Float(-1, 0, -1)); // v2
            mesh.Vertices.Add(new Vector3Float(-1, 0, 1)); // v3
            mesh.Vertices.Add(new Vector3Float(0, 1, 0)); // v4

            mesh.Faces.Add(0, 1, 2, mesh.Vertices);
            mesh.Faces.Add(0, 2, 3, mesh.Vertices);
            mesh.Faces.Add(3, 0, 4, mesh.Vertices);
            mesh.Faces.Add(0, 1, 4, mesh.Vertices);
            mesh.Faces.Add(1, 2, 4, mesh.Vertices);
            mesh.Faces.Add(2, 3, 4, mesh.Vertices);

            return mesh;
        }


    }
}

