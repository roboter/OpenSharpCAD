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
                RectangleDouble screenRect = this.TransformToScreenSpace(LocalBounds);
                // Scale viewport for Retina displays
                screenRect.Left *= GuiWidget.DeviceScale;
                screenRect.Bottom *= GuiWidget.DeviceScale;
                screenRect.Right *= GuiWidget.DeviceScale;
                screenRect.Top *= GuiWidget.DeviceScale;

                GLHelper.SetGlContext(this.World, screenRect, new LightingData()
                {
                    AmbientLight = new float[] { 0.2f, 0.2f, 0.2f, 1.0f }
                });
                DrawContent();
                GLHelper.UnsetGlContext();
            }
        }

        public override void OnMouseDown(MouseEventArgs mouseEvent)
        {
            System.Console.WriteLine("TrackballTumbleView.OnMouseDown");
            base.OnMouseDown(mouseEvent);
        }

        public override void OnMouseMove(MouseEventArgs mouseEvent)
        {
            // Logging mouse move might be too noisy, but useful for initial check
            // System.Console.WriteLine("TrackballTumbleView.OnMouseMove");
            base.OnMouseMove(mouseEvent);
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
        private ListBox errorListBox;
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

            verticalSplitter = new Splitter()
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                SplitterDistance = 400,
                SplitterBackground = Color.Red,
            };
            verticalSplitter.Panel1.BackgroundColor = new Color(173, 216, 230);
            verticalSplitter.Panel2.BackgroundColor = new Color(144, 238, 144);
            AddChild(verticalSplitter);

            {
                // panel 1 stuff
                textSide = new FlowLayoutWidget(FlowDirection.TopToBottom)
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                    BackgroundColor = Color.Cyan,
                };

                var code = new StringBuilder();
                code.AppendLine("var holesize =4;");
                code.AppendLine("// draw a box");
                code.AppendLine("var box = new Box(20, 20, 20);");
                code.AppendLine("var rotated = new Rotate(box, x: MathHelper.DegreesToRadians(45));");
                code.AppendLine("var cylinder = new Cylinder(50,20,60);");
                code.AppendLine("// draw the rotated box on top of the original");
                code.AppendLine("var translated = new Translate(new Box(40, 10, 10), 0, holesize, holesize);");
                code.AppendLine("Draw(box + rotated - translated + cylinder);");


                hello = new TextEditWidget(code.ToString().Replace('\r', '\n'))
                {
                    Multiline = true,
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                };
                hello.TextChanged += Hello_TextChanged;
                textSide.AddChild(hello);
                verticalSplitter.Panel1.AddChild(textSide);
            }

            {
                // panel 2 stuff
                Splitter rightSplitter = new Splitter()
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                    Orientation = Orientation.Horizontal,
                    SplitterDistance = 150,
                };
                verticalSplitter.Panel2.AddChild(rightSplitter);

                var world = new WorldView(800, 600);
                world.TranslationMatrix = Matrix4X4.CreateTranslation(0, 0, -50);
                trackBallWidget = new TrackballTumbleView(world, rightSplitter.Panel1)
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                    DrawContent = glLightedView_DrawGlContent,
                    TransformState = MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Rotation,
                };
                rightSplitter.Panel1.AddChild(trackBallWidget);

                errorListBox = new ListBox()
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                    BackgroundColor = Color.White,
                };
                errorListBox.SelectedValueChanged += ErrorListBox_SelectedValueChanged;
                rightSplitter.Panel2.AddChild(errorListBox);
            }

            BackgroundColor = Color.White;
            PerformLayout();
            Compile();
        }

        private void Hello_TextChanged(object sender, EventArgs e)
        {
            hello.ErrorLineIndices.Clear();
            Compile();
        }

        private void ErrorListBox_SelectedValueChanged(object sender, EventArgs e)
        {
            if (errorListBox.SelectedItem is ListBoxTextItem item)
            {
                if (int.TryParse(item.ItemValue, out int line))
                {
                    hello.JumpToLine(line);
                }
            }
        }

        private void Compile()
        {
            var compilerService = new CompilerService();
            List<Diagnostic> errors;

            // The service expects just the content inside Render(), which comes from hello.Text
            classRef = compilerService.Compile(hello.Text, out errors);

            errorListBox.Clear();

            if (errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    var lineSpan = error.Location.GetLineSpan();
                    int line = lineSpan.StartLinePosition.Line - 17; // offset from preamble
                    string message = $"{line + 1}: {error.GetMessage()}";
                    errorListBox.AddChild(new ListBoxTextItem(message, line.ToString()));
                    hello.ErrorLineIndices.Add(line);
                }
                hello.Invalidate();
                return;
            }
            trackBallWidget.Invalidate();
        }
        private void LogException(Exception ex)
        {
            // Always output to console so the user can see what happened
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);

            try
            {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(baseDir))
                {
                    baseDir = Path.GetTempPath();
                }

                string logDir = Path.Combine(baseDir, "CSharpCAD", "logs");

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
                if (classRef == null) return;
                classRef.Render();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during classRef.Render(): " + ex.Message);
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
