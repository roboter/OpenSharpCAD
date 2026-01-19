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
                textSide.BackgroundColor = Color.Cyan;  // Debug: textSide color
                {
                    objectEditorView = new GuiWidget(300, 500);
                    objectEditorList = new FlowLayoutWidget();
                    //  objectEditorList.AddChild(new TextEditWidget("Text in box"));

                    //   objectEditorView.AddChild(objectEditorList);
                    //   objectEditorView.BackgroundColor = Color.LightGray;
                    //   matterScriptEditor.LocalBounds = new RectangleDouble(0, 0, 200, 300);
                    //    textSide.AddChild(objectEditorView);
                    var code = new StringBuilder();
                    code.AppendLine("Draw(new Box(8, 20, 10));");
                    //code.AppendLine("MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();");
                    //code.AppendLine("var v0 = mesh.CreateVertex(new Vector3(1, 0, 1));  // V0");
                    //code.AppendLine("var v1 = mesh.CreateVertex(new Vector3(1, 0, -1)); // V1");
                    //code.AppendLine("var v2 = mesh.CreateVertex(new Vector3(-1, 0, -1)); // V2");
                    //code.AppendLine("var v3 = mesh.CreateVertex(new Vector3(-1, 0, 1)); // V3");
                    //code.AppendLine("var v4 = mesh.CreateVertex(new Vector3(0, 1, 0)); // v4");

                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v0, v1, v2, v3 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v3, v0, v4 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v0, v1, v4 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v1, v2, v4 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v2, v3, v4 });");

                    //code.AppendLine("RenderMeshToGl.Render(mesh, new RGBA_Floats(.3, .8, 7)); ");
                    hello = new TextEditWidget(code.ToString().Replace('\r', '\n'));
                    hello.Multiline = true;
                    hello.HAnchor = HAnchor.Stretch;
                    hello.VAnchor = VAnchor.Stretch;
                    hello.TextChanged += Hello_TextChanged;  // Subscribe AFTER setting Multiline
                    textSide.AddChild(hello);
                    textSide.AnchorAll();
                    objectEditorList.AnchorAll();
                    //    textSide.BoundsChanged += new EventHandler(textSide_BoundsChanged);

                    //#region Buttons
                    //FlowLayoutWidget topButtonBar = new FlowLayoutWidget();

                    //Button loadMatterScript = new Button("Load Matter Script");
                    ////        loadMatterScript.Click += loadMatterScript_Click;
                    //topButtonBar.AddChild(loadMatterScript);

                    //outputScad = new Button("Output SCAD");
                    ////        outputScad.Click += outputScad_Click;
                    //topButtonBar.AddChild(outputScad);

                    //textSide.AddChild(topButtonBar);

                    //FlowLayoutWidget bottomButtonBar = new FlowLayoutWidget();

                    //Button loadStl = new Button("Load STL");
                    ////          loadStl.Click += LoadStl_Click;
                    //bottomButtonBar.AddChild(loadStl);

                    //textSide.AddChild(bottomButtonBar);

                    //#endregion
                }

                // pannel 2 stuff
                FlowLayoutWidget renderSide = new FlowLayoutWidget(FlowDirection.TopToBottom);

                renderSide.AnchorAll();
                {
                    var world = new WorldView(800, 600);
                    trackBallWidget = new TrackballTumbleView(world, renderSide);
                    trackBallWidget.DrawContent = glLightedView_DrawGlContent;
                    //   trackBallWidget.BackgroundColor = Color.Yellow;  // Debug: renderSide color
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
            classRef = compilerService.Compile(hello.Text, out errors);

            if (errors.Count > 0)
            {
                StringBuilder sberror = new StringBuilder();
                foreach (var error in errors)
                {
                    sberror.AppendLine(error);
                }
                // TODO: Display errors to user (e.g., txtErrors.Text = sberror.ToString())
                // For now, logging as before might be done by caller or added here if needed, 
                // but simpler to just return.
                return;
            }
            trackBallWidget.Invalidate();
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

