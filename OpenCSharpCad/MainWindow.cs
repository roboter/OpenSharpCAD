﻿using MatterHackers.Agg;
using MatterHackers.Agg.OpenGlGui;
using MatterHackers.Agg.UI;
using MatterHackers.Csg;
using MatterHackers.Csg.Processors;
using MatterHackers.RenderOpenGl;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Text;

namespace OpenSharpCAD
{
    public class MainWindow : SystemWindow
    {
        // private MatterHackers.PolygonMesh.Mesh meshToRender = null;
        private TrackballTumbleWidget trackBallWidget;
        private Button outputScad;
        private Splitter varticalSplitter;

        private GuiWidget objectEditorView;
        private FlowLayoutWidget objectEditorList;
        private FlowLayoutWidget textSide;
        private TextEditWidget textEdit;
        private TextEditWidget outputEdit;
        //private Union rootUnion = new Union("root");
        dynamic classRef;
        private CsgObject previousRender;
        public MainWindow() : base(1000, 1000)
        {
            //new BoxPrimitive(8, 20, 10);
            //rootUnion.Add(new Translate(new BoxPrimitive(10, 10, 20), 5, 10, 5));
            //rootUnion.Add(new Box(8, 20, 10));
            ////rootUnion.Add(new Cylinder(10, 40));
            //rootUnion.Add(new Translate(new Sphere(radius: 30), 15, 20, 40)); //not implemented
            //var testUnion = new Translate(new Box(10, 10, 20) - new Box(8, 20, 10), 5, 5, 5); //new Difference(
            //rootUnion.Add(new LinearExtrude(new double[] { 1.1, 2.2, 3.3, 6.3 }, 7)); //not implemented
            //rootUnion.Add(testUnion);
            //rootUnion.Box(8, 20, 40);

            SuspendLayout();
            varticalSplitter = new Splitter();
            varticalSplitter.SplitterDistance = 700;
            // panel 1 stuff
            #region TextSide
            textSide = new FlowLayoutWidget(FlowDirection.TopToBottom);

            objectEditorView = new GuiWidget();
            objectEditorList = new FlowLayoutWidget();
            objectEditorList.AddChild(new TextEditWidget("Text in box"));

            objectEditorView.AddChild(objectEditorList);
            objectEditorView.BackgroundColor = RGBA_Bytes.LightGray;
            //matterScriptEditor.LocalBounds = new RectangleDouble(0, 0, 200, 300);
            textSide.AddChild(objectEditorView);
            var code = new StringBuilder();
            code.AppendLine("var rootUnion = new MatterHackers.Csg.Operations.Union(\"root\");");
            code.AppendLine("//rootUnion.Add(new LinearExtrude(new double[] { 1.1, 2.2, 3.3, 6.3 }, 7));");
            code.AppendLine("rootUnion.Add(new Translate(new Cylinder(10, 40), 5, 10, 5));");
            code.AppendLine("rootUnion.Add(new BoxPrimitive(8, 20, 10));");

            code.AppendLine("for (int i = 0; i != 10; i++)");
            code.AppendLine("{");
            code.AppendLine("   rootUnion.Add(new Translate(new BoxPrimitive(18, i, 14), 10 * i, 10+i, 10+i*i));");
            code.AppendLine("}");

            code.AppendLine("return rootUnion;");

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
            textEdit = new TextEditWidget(code.ToString().Replace('\r', '\n'));
            //   textEdit.BackgroundColor = RGBA_Bytes.Yellow;
            textEdit.TextChanged += Hello_TextChanged;
            textEdit.Multiline = true;
            //     hello.Text = code.ToString();
            textSide.AddChild(textEdit);

            outputEdit = new TextEditWidget("Hello world!")
            {
                Multiline = true
            };
            outputEdit.AnchorAll();
            textSide.AddChild(outputEdit);
            textSide.AnchorAll();
            textEdit.AnchorAll();
            objectEditorList.AnchorAll();
            textSide.BoundsChanged += textSide_BoundsChanged;

            #region Buttons
            FlowLayoutWidget topButtonBar = new FlowLayoutWidget();

            Button load = new Button("Load OpenSharpCAD Script");
            load.Click += loadMatterScript_Click;
            topButtonBar.AddChild(load);
            Button save = new Button("Save OpenSharpCAD Script");
            save.Click += SaveMatterScript_Click;
            topButtonBar.AddChild(save);

            outputScad = new Button("Output SCAD");
            outputScad.Click += outputScad_Click;
            topButtonBar.AddChild(outputScad);

            textSide.AddChild(topButtonBar);

            FlowLayoutWidget bottomButtonBar = new FlowLayoutWidget();

            //Button loadStl = new Button("Load STL");
            //loadStl.Click += LoadStl_Click;
            //bottomButtonBar.AddChild(loadStl);

            textSide.AddChild(bottomButtonBar);

            #endregion
            #endregion

            // pannel 2 stuff
            FlowLayoutWidget renderSide = new FlowLayoutWidget(FlowDirection.TopToBottom);
            renderSide.AnchorAll();

            trackBallWidget = new TrackballTumbleWidget();
            trackBallWidget.DrawGlContent += glLightedView_DrawGlContent;

            trackBallWidget.TrackBallController.Scale = 0.05;

            renderSide.AddChild(trackBallWidget);

            varticalSplitter.Panel2.AddChild(renderSide);
            varticalSplitter.Panel1.AddChild(textSide);

            ResumeLayout();

            AnchorAll();

            varticalSplitter.AnchorAll();

            textSide.AnchorAll();

            trackBallWidget.AnchorAll();

            AddChild(varticalSplitter);

            BackgroundColor = RGBA_Bytes.White;
            Compile();
        }

        private void SaveMatterScript_Click(object sender, EventArgs e)
        {
            FileDialog.SaveFileDialog(new SaveFileDialogParams("CS files (*.cs)|*.cs"), (saveParams) =>
            {
                if (saveParams != null)
                {
                    File.WriteAllText(saveParams.FileName, textEdit.Text);
                }
            });
        }

        private void outputScad_Click(object sender, EventArgs e)
        {
            if (previousRender != null)
            {
                SaveFileDialogParams saveParams = new SaveFileDialogParams("Text files (*.scad)|*.scad");
                FileDialog.SaveFileDialog(saveParams, (streamToSaveTo) =>
                {
                    if (streamToSaveTo != null)
                    {
                        OpenSCadOutput.Save(previousRender, streamToSaveTo.FileName);
                    }
                });
            }
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
            //sb.AppendLine("using System;");
            //sb.AppendLine("using System.Windows.Forms;");
            //sb.AppendLine("using System.Collections.Generic;");
            //sb.AppendLine("using MatterHackers.PolygonMesh;");
            sb.AppendLine("using MatterHackers.VectorMath; ");

            //sb.AppendLine("using MatterHackers.Csg.Operations;");
            sb.AppendLine("using MatterHackers.Csg;");
            sb.AppendLine("using MatterHackers.Csg.Solids;");
            ////sb.AppendLine("using .Alignment;");
            //sb.AppendLine("using MatterHackers.RenderOpenGl;");
            //sb.AppendLine("using MatterHackers.Agg;");
            sb.AppendLine("using MatterHackers.Csg.Transform;");

            //     sb.AppendLine();
            sb.AppendLine("namespace OpenSharpCadSnippet");
            sb.AppendLine("{");

            sb.AppendLine("      public class RenderTest");
            sb.AppendLine("      {");

            // My pre-defined class named FilterCountries that receive the sourceListBox
            sb.AppendLine("            public CsgObject Render()");
            sb.AppendLine("            {");

            sb.AppendLine(textEdit.Text);
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
                outputEdit.Text = string.Empty;
                //------------
                // Pass the class code, the namespace of the class and the list of extra assemblies needed
                classRef = DynCode.CodeHelper.HelperFunction(classCode, "OpenSharpCadSnippet.RenderTest", new object[] { });

                //-------------------
                // If the compilation process returned an error, then show to the user all errors
                if (classRef is CompilerErrorCollection)
                {
                    StringBuilder sberror = new StringBuilder();

                    foreach (CompilerError error in (CompilerErrorCollection)classRef)
                    {
                        var err = string.Format("{0}:{1} {2} {3}", error.Line, error.Column, error.ErrorNumber, error.ErrorText);
                        sberror.AppendLine(err);
                        Debug.WriteLine(err);
                        outputEdit.Text += err + "\n";
                    }
                    return;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                outputEdit.Text += ex.Message + "\n";
                throw;
            }

        }
        private void glLightedView_DrawGlContent(object sender, EventArgs e)
        {
            try
            {
                var exists = IsPropertyExist(classRef, "HasErrors");

                if (!exists)
                {
                    CsgObject ren = classRef.Render();
                    RenderCsgToGl.Render(ren);
                    previousRender = ren;
                }
                else
                {
                    if (previousRender != null)
                    {
                        RenderCsgToGl.Render(previousRender);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                outputEdit.Text = ex.Message + "\n";
            }
        }

        public static bool IsPropertyExist(dynamic settings, string name)
        {
            if (settings is ExpandoObject)
            {
                return ((IDictionary<string, object>)settings).ContainsKey(name);
            }
            return settings.GetType().GetProperty(name) != null;
        }


        void textSide_BoundsChanged(object sender, EventArgs e)
        {
            objectEditorView.LocalBounds = new RectangleDouble(0, 0, ((GuiWidget)sender).Width - 10, objectEditorView.Height);
            Invalidate();
        }
        void loadMatterScript_Click(object sender, EventArgs mouseEvent)
        {
            OpenFileDialogParams openParams = new OpenFileDialogParams("MatterScript Files, c-sharp code|*.part;*.cs");

            FileDialog.OpenFileDialog(openParams, (streamToLoadFrom) =>
            {

                if (streamToLoadFrom != null)
                {
                    SuspendLayout();
                    var loadedFileName = openParams.FileName;
                    string extension = Path.GetExtension(openParams.FileName).ToUpper(CultureInfo.InvariantCulture);

                    string text = File.ReadAllText(loadedFileName);

                    StreamReader streamReader = new StreamReader(streamToLoadFrom.FileName);
                    textEdit.Text = streamReader.ReadToEnd();
                    streamReader.Close();

                    varticalSplitter.SplitterDistance = varticalSplitter.SplitterDistance - 1;
                    varticalSplitter.SplitterDistance = varticalSplitter.SplitterDistance + 1;

                    ResumeLayout();
                    AnchorAll();
                    varticalSplitter.AnchorAll();
                    textSide.AnchorAll();
                    objectEditorView.Invalidate();
                    textSide.PerformLayout();
                    trackBallWidget.AnchorAll();
                    Invalidate();
                }
            });

        }

        //private void LoadStl_Click(object sender, EventArgs e)
        //{
        //    OpenFileDialogParams opeParams = new OpenFileDialogParams("STL Files|*.stl");

        //    FileDialog.OpenFileDialog(opeParams, (openParams) =>
        //    {
        //        var streamToLoadFrom = File.Open(openParams.FileName, FileMode.Open);

        //        if (streamToLoadFrom != null)
        //        {
        //            var loadedFileName = openParams.FileName;

        //            meshToRender = StlProcessing.Load(streamToLoadFrom);

        //            ImageBuffer plateInventory = new ImageBuffer((int)(300 * 8.5), 300 * 11, 32, new BlenderBGRA());
        //            Graphics2D plateGraphics = plateInventory.NewGraphics2D();
        //            plateGraphics.Clear(RGBA_Bytes.White);

        //            double inchesPerMm = 0.0393701;
        //            double pixelsPerInch = 300;
        //            double pixelsPerMm = inchesPerMm * pixelsPerInch;
        //            AxisAlignedBoundingBox aabb = meshToRender.GetAxisAlignedBoundingBox();
        //            Vector2 lowerLeftInMM = new Vector2(-aabb.minXYZ.x, -aabb.minXYZ.y);
        //            Vector3 centerInMM = (aabb.maxXYZ - aabb.minXYZ) / 2;
        //            Vector2 offsetInMM = new Vector2(20, 30);


        //            RectangleDouble bounds = new RectangleDouble(offsetInMM.x * pixelsPerMm,
        //                offsetInMM.y * pixelsPerMm,
        //                (offsetInMM.x + aabb.maxXYZ.x - aabb.minXYZ.x) * pixelsPerMm,
        //                (offsetInMM.y + aabb.maxXYZ.y - aabb.minXYZ.y) * pixelsPerMm);
        //            bounds.Inflate(3 * pixelsPerMm);
        //            RoundedRect rect = new RoundedRect(bounds, 3 * pixelsPerMm);
        //            plateGraphics.Render(rect, RGBA_Bytes.LightGray);
        //            Stroke rectOutline = new Stroke(rect, .5 * pixelsPerMm);
        //            plateGraphics.Render(rectOutline, RGBA_Bytes.DarkGray);


        //            OrthographicZProjection.DrawTo(plateGraphics, meshToRender, lowerLeftInMM + offsetInMM, pixelsPerMm);
        //            plateGraphics.DrawString(Path.GetFileName(openParams.FileName), (offsetInMM.x + centerInMM.x) * pixelsPerMm, (offsetInMM.y - 10) * pixelsPerMm, 50, MatterHackers.Agg.Font.Justification.Center);

        //            //ImageBuffer logoImage = new ImageBuffer();
        //            //ImageIO.LoadImageData("Logo.png", logoImage);
        //            //plateGraphics.Render(logoImage, (plateInventory.Width - logoImage.Width) / 2, plateInventory.Height - logoImage.Height - 10 * pixelsPerMm);

        //            //ImageIO.SaveImageData("plate Inventory.jpeg", plateInventory);
        //        }
        //    });
        //}
    }
}
