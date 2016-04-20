/*
Copyright (c) 2012, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies, 
either expressed or implied, of the FreeBSD Project.
*/


using MatterHackers.Agg;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.OpenGlGui;
using MatterHackers.Agg.PlatformAbstract;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.MatterCadGui.CsgEditors;
using MatterHackers.PolygonMesh.Processors;
using MatterHackers.RenderOpenGl;
using MatterHackers.VectorMath;
using System;
using System.IO;
using MatterHackers.Agg.WindowsFileDialogs;

namespace MatterHackers.MatterCad
{
    public class MatterCadGuiWidget : GuiWidget
    {
        PolygonMesh.Mesh meshToRender = null;

        TrackballTumbleWidget trackBallWidget;
        Button outputScad;
        Splitter verticleSpliter;

        GuiWidget objectEditorView;
        FlowLayoutWidget objectEditorList;
        FlowLayoutWidget textSide;

        Csg.Operations.Union rootUnion = new Csg.Operations.Union("root");

        public MatterCadGuiWidget()
        {
            //rootUnion.Add(new Translate(new BoxPrimitive(10, 10, 20), 5, 10, 5));
            //rootUnion.Add(new Box(8, 20, 10));

            SuspendLayout();
            verticleSpliter = new Splitter();
            {
                // pannel 1 stuff
                textSide = new FlowLayoutWidget(FlowDirection.TopToBottom);
                {
                    objectEditorView = new GuiWidget(300, 500);
                    objectEditorList = new FlowLayoutWidget();
                    objectEditorList.AddChild(CsgEditorBase.CreateEditorForCsg(rootUnion));
                    objectEditorView.AddChild(objectEditorList);
                    objectEditorView.BackgroundColor = RGBA_Bytes.LightGray;
                    //matterScriptEditor.LocalBounds = new RectangleDouble(0, 0, 200, 300);
                    textSide.AddChild(objectEditorView);
                    textSide.BoundsChanged += new EventHandler(textSide_BoundsChanged);

                    FlowLayoutWidget topButtonBar = new FlowLayoutWidget();
                    {
                        Button loadMatterScript = new Button("Load Matter Script");
                        //      loadMatterScript.Click += new ButtonBase.ButtonEventHandler(loadMatterScript_Click);
                        topButtonBar.AddChild(loadMatterScript);

                        outputScad = new Button("Output SCAD");
                        //    outputScad.Click += new ButtonBase.ButtonEventHandler(outputScad_Click);
                        topButtonBar.AddChild(outputScad);
                    }
                    textSide.AddChild(topButtonBar);

                    FlowLayoutWidget bottomButtonBar = new FlowLayoutWidget();
                    {
                        Button loadStl = new Button("Load STL");
                        loadStl.Click += LoadStl_Click;
                        //loadStl.Click += loadStl_Click;
                        bottomButtonBar.AddChild(loadStl);
                    }
                    textSide.AddChild(bottomButtonBar);
                }

                //    // pannel 2 stuff
                FlowLayoutWidget renderSide = new FlowLayoutWidget(FlowDirection.TopToBottom);
                renderSide.AnchorAll();
                {
                    trackBallWidget = new TrackballTumbleWidget();
                    trackBallWidget.DrawGlContent += new EventHandler(glLightedView_DrawGlContent);
                    //  renderSide.AddChild(trackBallWidget);
                }
                verticleSpliter.Panel2.AddChild(renderSide);
                verticleSpliter.Panel1.AddChild(textSide);
            }
            ResumeLayout();

            AnchorAll();

            verticleSpliter.AnchorAll();

            textSide.AnchorAll();

            trackBallWidget.AnchorAll();

            AddChild(verticleSpliter);
            //
            BackgroundColor = RGBA_Bytes.White;
        }



        public override void OnParentChanged(EventArgs e)
        {
            verticleSpliter.SplitterDistance = Parent.Width / 2;
            base.OnParentChanged(e);
        }

        private void LoadStl_Click(object sender, EventArgs e)
        {
            //    throw new NotImplementedException();
            //}
            //void loadStl_Click(object sender, MouseEventArgs mouseEvent)
            //{
            OpenFileDialogParams opeParams = new OpenFileDialogParams("STL Files|*.stl");

            FileDialog.OpenFileDialog(opeParams, (openParams) =>
            {
                var streamToLoadFrom = File.Open(openParams.FileName, FileMode.Open);


                if (streamToLoadFrom != null)
                {
                    var loadedFileName = openParams.FileName;

                    meshToRender = StlProcessing.Load(streamToLoadFrom);

                    ImageBuffer plateInventory = new ImageBuffer((int)(300 * 8.5), 300 * 11, 32, new BlenderBGRA());
                    Graphics2D plateGraphics = plateInventory.NewGraphics2D();
                    plateGraphics.Clear(RGBA_Bytes.White);

                    double inchesPerMm = 0.0393701;
                    double pixelsPerInch = 300;
                    double pixelsPerMm = inchesPerMm * pixelsPerInch;
                    AxisAlignedBoundingBox aabb = meshToRender.GetAxisAlignedBoundingBox();
                    Vector2 lowerLeftInMM = new Vector2(-aabb.minXYZ.x, -aabb.minXYZ.y);
                    Vector3 centerInMM = (aabb.maxXYZ - aabb.minXYZ) / 2;
                    Vector2 offsetInMM = new Vector2(20, 30);

                    {
                        RectangleDouble bounds = new RectangleDouble(offsetInMM.x * pixelsPerMm,
                            offsetInMM.y * pixelsPerMm,
                            (offsetInMM.x + aabb.maxXYZ.x - aabb.minXYZ.x) * pixelsPerMm,
                            (offsetInMM.y + aabb.maxXYZ.y - aabb.minXYZ.y) * pixelsPerMm);
                        bounds.Inflate(3 * pixelsPerMm);
                        RoundedRect rect = new RoundedRect(bounds, 3 * pixelsPerMm);
                        plateGraphics.Render(rect, RGBA_Bytes.LightGray);
                        Stroke rectOutline = new Stroke(rect, .5 * pixelsPerMm);
                        plateGraphics.Render(rectOutline, RGBA_Bytes.DarkGray);
                    }

                    OrthographicZProjection.DrawTo(plateGraphics, meshToRender, lowerLeftInMM + offsetInMM, pixelsPerMm);
                    plateGraphics.DrawString(Path.GetFileName(openParams.FileName), (offsetInMM.x + centerInMM.x) * pixelsPerMm, (offsetInMM.y - 10) * pixelsPerMm, 50, Agg.Font.Justification.Center);

                    ImageBuffer logoImage = new ImageBuffer();
                    ImageIO.LoadImageData("Logo.png", logoImage);
                    plateGraphics.Render(logoImage, (plateInventory.Width - logoImage.Width) / 2, plateInventory.Height - logoImage.Height - 10 * pixelsPerMm);

                    ImageIO.SaveImageData("plate Inventory.jpeg", plateInventory);
                }
            });
        }

        void textSide_BoundsChanged(object sender, EventArgs e)
        {
            objectEditorView.LocalBounds = new RectangleDouble(0, 0, ((GuiWidget)sender).Width - 10, objectEditorView.Height);
            Invalidate();
        }

        void glLightedView_DrawGlContent(object sender, EventArgs e)
        {
            if (rootUnion != null)
            {
                RenderCsgToGl.Render(rootUnion);
            }
            if (meshToRender != null)
            {
                RenderMeshToGl.Render(meshToRender, RGBA_Bytes.Gray);
            }
        }

        string loadedFileName;
        void loadMatterScript_Click(object sender, MouseEventArgs mouseEvent)
        {
            // this should save and load json
            throw new NotImplementedException();

#if false
            OpenFileDialogParams openParams = new OpenFileDialogParams("MatterScript Files, c-sharp code|*.part;*.cs");

            Stream streamToLoadFrom = FileDialog.OpenFileDialog(ref openParams);
            if (streamToLoadFrom != null)
            {
                loadedFileName = openParams.FileName;
                string extension = System.IO.Path.GetExtension(openParams.FileName).ToUpper(CultureInfo.InvariantCulture);
                if (extension == ".CS")
                {
                }
                else if (extension == ".VB")
                {
                }

                //string text = System.IO.File.ReadAllText(loadedFileName);

                StreamReader streamReader = new StreamReader(streamToLoadFrom);
                objectEditorView.Text = streamReader.ReadToEnd();
                streamToLoadFrom.Close();

                verticleSpliter.SplitterDistance = verticleSpliter.SplitterDistance - 1;
                verticleSpliter.SplitterDistance = verticleSpliter.SplitterDistance + 1;
            }
#endif
        }

        void outputScad_Click(object sender, MouseEventArgs mouseEvent)
        {
            //if (rootUnion != null)
            //{
            //    SaveFileDialogParams saveParams = new SaveFileDialogParams("Text files (*.scad)|*.scad");
            //    Stream streamToSaveTo = FileDialog.SaveFileDialog(ref saveParams);

            //    if (streamToSaveTo != null)
            //    {
            //        OpenSCadOutput.Save(Utilities.PutOnPlatformAndCenter(rootUnion), streamToSaveTo);
            //    }
            //}
        }

        public override void OnDraw(Graphics2D graphics2D)
        {
            graphics2D.Clear(RGBA_Bytes.White);

            base.OnDraw(graphics2D);
        }
    }
}
