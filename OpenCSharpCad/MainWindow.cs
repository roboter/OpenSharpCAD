using System;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Csg.Operations;
using MatterHackers.VectorMath;
using MatterHackers.PolygonMesh;

using System.CodeDom.Compiler;
using System.Text;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Solids;

namespace OpenCSharpCad
{
    public class MainWindow : SystemWindow
    {
        //private MatterHackers.PolygonMesh.Mesh meshToRender = null;

        private TrackballTumbleWidget trackBallWidget;
        private Button outputScad;
        private Splitter verticleSpliter;

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
            // SuspendLayout();
            verticleSpliter = new Splitter();
            verticleSpliter.SplitterDistance = 400;  // Set left panel width for text editor
            verticleSpliter.Panel1.BackgroundColor = new Color(173, 216, 230);  // Debug: Panel1 light blue
            verticleSpliter.Panel2.BackgroundColor = new Color(144, 238, 144);  // Debug: Panel2 light green
            verticleSpliter.SplitterBackground = Color.Red;  // Debug: Splitter bar color
            {
                // panel 1 stuff
                textSide = new FlowLayoutWidget(FlowDirection.TopToBottom);
                textSide.BackgroundColor = Color.Cyan;  // Debug: textSide color
                {
                    objectEditorView = new GuiWidget(300, 500);
                    objectEditorList = new FlowLayoutWidget();
                    //  objectEditorList.AddChild(new TextEditWidget("Text in box"));

                    //   objectEditorView.AddChild(objectEditorList);
                    objectEditorView.BackgroundColor = Color.LightGray;
                    //   matterScriptEditor.LocalBounds = new RectangleDouble(0, 0, 200, 300);
                    //    textSide.AddChild(objectEditorView);
                    var code = new StringBuilder();

                    code.AppendLine("new Box(8, 20, 10);");
                    //code.AppendLine("MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();");
                    //code.AppendLine("var v0 = mesh.CreateVertex(new Vector3(1, 0, 1));  // V0");
                    //code.AppendLine("var v1 = mesh.CreateVertex(new Vector3(1, 0, -1)); // V1");
                    //code.AppendLine("var v2 = mesh.CreateVertex(new Vector3(-1, 0, -1)); // V2");
                    //code.AppendLine("var v3 = mesh.CreateVertex(new Vector3(-1, 0, 1)); // V3");
                    //code.AppendLine("var v4 = mesh.CreateVertex(new Vector3(0, 1, 0)); // V4");

                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v0, v1, v2, v3 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v3, v0, v4 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v0, v1, v4 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v1, v2, v4 });");
                    //code.AppendLine("mesh.CreateFace(new Vertex[] { v2, v3, v4 });");

                    //code.AppendLine("RenderMeshToGl.Render(mesh, new RGBA_Floats(.3, .8, 7)); ");
                    hello = new TextEditWidget(code.ToString().Replace('\r', '\n'));
                    hello.TextChanged += Hello_TextChanged;
                    hello.Multiline = true;
                    textSide.AddChild(hello);
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
                renderSide.BackgroundColor = Color.Yellow;  // Debug: renderSide color
                renderSide.AnchorAll();
                {
                    trackBallWidget = new TrackballTumbleWidget(new WorldView(Width, Height), renderSide);
                    // trackBallWidget.DrawGlContent += new EventHandler(glLightedView_DrawGlContent);
                    renderSide.AddChild(trackBallWidget);
                }
                verticleSpliter.Panel2.AddChild(renderSide);
                verticleSpliter.Panel1.AddChild(textSide);
            }
            // ResumeLayout();

            AnchorAll();

            verticleSpliter.AnchorAll();

            textSide.AnchorAll();

            trackBallWidget.AnchorAll();

            AddChild(verticleSpliter);

            BackgroundColor = Color.White;
            Compile();
        }

        private void Hello_TextChanged(object sender, EventArgs e)
        {
            Compile();
        }

        private void Compile()
        {

            StringBuilder sb = new StringBuilder();

            //-----------------
            // Create the class as usual
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Windows.Forms;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using MatterHackers.PolygonMesh;");
            sb.AppendLine("using MatterHackers.VectorMath; ");
            sb.AppendLine("using MatterHackers.Csg.Operations; ");
            sb.AppendLine("using MatterHackers.Csg.Solids; ");
            sb.AppendLine("using MatterHackers.RenderOpenGl; ");
            sb.AppendLine("using MatterHackers.Agg;");

            sb.AppendLine("namespace Test");
            sb.AppendLine("{");

            sb.AppendLine("      public class RenderTest");
            sb.AppendLine("      {");

            // My pre-defined class named FilterCountries that receive the sourceListBox
            sb.AppendLine("            public void Render()");
            sb.AppendLine("            {");

            sb.AppendLine(hello.Text);
            sb.AppendLine("            }");
            sb.AppendLine("      }");
            sb.AppendLine("}");




            //-----------------
            // The finished code
            string classCode = sb.ToString();

            //-----------------
            // Dont need any extra assemblies


            try
            {
                // / txtErrors.Clear();

                //------------
                // Pass the class code, the namespace of the class and the list of extra assemblies needed
                classRef = DynCode.CodeHelper.HelperFunction(classCode, "Test.RenderTest", new object[] { });

                //-------------------
                // If the compilation process returned an error, then show to the user all errors
                if (classRef is CompilerErrorCollection)
                {
                    StringBuilder sberror = new StringBuilder();

                    foreach (CompilerError error in (CompilerErrorCollection)classRef)
                    {
                        sberror.AppendLine(string.Format("{0}:{1} {2} {3}", error.Line, error.Column, error.ErrorNumber, error.ErrorText));
                    }

                    //  txtErrors.Text = sberror.ToString();

                    return;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                // If something very bad happened then throw it
                //   MessageBox.Show(ex.Message);
                throw;
            }
        }
        private void glLightedView_DrawGlContent(object sender, EventArgs e)
        {

            try
            {

                classRef.Render();
            }
            catch (Exception) { }
            //-------------
            // Finally call the class to filter the countries with the specific routine provided
            //List<string> targetValues = classRef.FilterCountries(lstSource);

            ////-------------
            //// Move the result to the target listbox
            //lstTarget.Items.Clear();
            //lstTarget.Items.AddRange(targetValues.ToArray());
        }

        private MatterHackers.PolygonMesh.Mesh Pyramid()
        {
            MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();
            mesh.Vertices.Add(new Vector3Float(1, 0, 1));  // V0
            mesh.Vertices.Add(new Vector3Float(1, 0, -1)); // V1
            mesh.Vertices.Add(new Vector3Float(-1, 0, -1)); // V2
            mesh.Vertices.Add(new Vector3Float(-1, 0, 1)); // V3
            mesh.Vertices.Add(new Vector3Float(0, 1, 0)); // V4

            mesh.Faces.Add(0, 1, 2, mesh.Vertices);
            mesh.Faces.Add(2, 3, 0, mesh.Vertices);
            mesh.Faces.Add(3, 0, 4, mesh.Vertices);
            mesh.Faces.Add(0, 1, 4, mesh.Vertices);
            mesh.Faces.Add(1, 2, 4, mesh.Vertices);
            mesh.Faces.Add(2, 3, 4, mesh.Vertices);

            return mesh;
        }


        [STAThread]
        public static void Main(string[] args)
        {
            MainWindow cadWindow = new MainWindow(true);
            cadWindow.UseOpenGL = true;
            cadWindow.Title = "OpenCSharpCad";

            cadWindow.ShowAsSystemWindow();
        }
    }
}
