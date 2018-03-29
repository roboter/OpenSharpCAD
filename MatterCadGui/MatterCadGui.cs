using System;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Processors;

namespace MatterHackers.MatterCad
{
    public class MatterCadGui : SystemWindow
    {
        MatterCadGuiWidget matterCadGuiWidget;

        CsgObject MakerGearXCariage()
        {
            CsgObject frontRodHolder = new Cylinder(11, 70, name: "front rod holder");
            CsgObject total = frontRodHolder;

            CsgObject backRodHolder = new Cylinder(11, 70, name: "back rod holder");
            backRodHolder = new Translate(backRodHolder, 0, 50, 0);
            total += backRodHolder;

            CsgObject plate = new Box(7, 32, 65, createCentered: false);
            plate = new SetCenter(plate, total.GetCenter());
            plate = new Align(plate, Face.Bottom, frontRodHolder, Face.Bottom);
            total += plate;

            CsgObject beltMount = new Box(7, 32, 30, createCentered: false);
            beltMount = new SetCenter(beltMount, frontRodHolder.GetCenter() + new Vector3(6, -19, 0));
            
            // belt mount screw holes
            CsgObject screwHole = new Cylinder(2, beltMount.XSize+1, Alignment.x);
            screwHole = new SetCenter(screwHole, beltMount.GetCenter());
            beltMount -= new Align(screwHole, Face.Front | Face.Top, beltMount, Face.Front | Face.Top, 0, 3, -4);
            beltMount -= new Align(screwHole, Face.Front | Face.Top, beltMount, Face.Front | Face.Top, 0, 18, -4);
            beltMount -= new Align(screwHole, Face.Front | Face.Bottom, beltMount, Face.Front | Face.Bottom, 0, 3, 4);
            beltMount -= new Align(screwHole, Face.Front | Face.Bottom, beltMount, Face.Front | Face.Bottom, 0, 18, 4);
            
            total += beltMount;

            // smooth rod holes
            total -= new Cylinder(8, 71, name: "back rod bearing hole");
            total -= new SetCenter(new Cylinder(8, 71, name: "front rod bearing hole"), backRodHolder.GetCenter());

            return total;
        }

        CsgObject PSEyeCameraHolder()
        {
            double pcbHoldLip = 2;
            double standYDepth = 8;
            double pcbYDepth = 2;
            CsgObject pcbTotal;

            CsgObject pcbBottom = new Box(23, pcbYDepth, 40, createCentered: false);
            pcbTotal = pcbBottom;

            CsgObject pcbMid = new Box(30, pcbYDepth, 34.5, createCentered: false);
            pcbMid = new Align(pcbMid, Face.Top, pcbBottom, Face.Top);
            pcbMid = new SetCenter(pcbMid, pcbBottom.GetCenter(), true, false, false);
            pcbTotal += pcbMid;

            CsgObject pcbTop = new Box(64.5, pcbYDepth, 21.5, createCentered: false);
            pcbTop = new Align(pcbTop, Face.Top, pcbBottom, Face.Top);
            pcbTop = new SetCenter(pcbTop, pcbBottom.GetCenter(), true, false, false);
            pcbTotal += pcbTop;

            Box standBaseBox = new Box(pcbTop.XSize + 2 * (standYDepth - pcbHoldLip), 37, standYDepth, createCentered: false);
            standBaseBox.BevelFace(Face.Top, 3);
            standBaseBox.BevelEdge(Face.Left | Face.Front, 3);
            standBaseBox.BevelEdge(Face.Left | Face.Back, 3);
            standBaseBox.BevelEdge(Face.Right | Face.Front, 3);
            standBaseBox.BevelEdge(Face.Right | Face.Back, 3);
            CsgObject standBase = standBaseBox;
            CsgObject standTotal = standBase;

            Box standSideSupportLeftBox = new Box(standYDepth, standYDepth, pcbBottom.ZSize + standBase.ZSize - 10, createCentered: false);
            standSideSupportLeftBox.BevelEdge(Face.Left | Face.Front, 3);
            standSideSupportLeftBox.BevelEdge(Face.Left | Face.Back, 3);
            standSideSupportLeftBox.BevelEdge(Face.Left | Face.Top, 3);
            standSideSupportLeftBox.BevelEdge(Face.Right | Face.Front, 1);
            standSideSupportLeftBox.BevelEdge(Face.Right | Face.Back, 1);
            CsgObject standSideSupportLeft = standSideSupportLeftBox;
            standSideSupportLeft = new SetCenter(standSideSupportLeft, standBase.GetCenter(), false, true, false);
            standSideSupportLeft = new Align(standSideSupportLeft, Face.Bottom | Face.Left, standBase, Face.Bottom | Face.Left);
            standTotal += standSideSupportLeft;

            Box insideBevelLeftBox = new Box(standYDepth, standYDepth, standYDepth, createCentered: false);
            insideBevelLeftBox.CutAlongDiagonal(Face.Left | Face.Bottom);
            CsgObject insideBevelLeft = insideBevelLeftBox;
            insideBevelLeft = new Align(insideBevelLeft, Face.Left | Face.Front, standSideSupportLeft, Face.Right | Face.Front, -1, 0, -.1);
            insideBevelLeft = new Align(insideBevelLeft, Face.Bottom, standBase, Face.Top);
            standTotal += insideBevelLeft;

            Box standSideSupportRightBox = new Box(standYDepth, standYDepth, pcbBottom.ZSize + standBase.ZSize - 10, createCentered: false);
            standSideSupportRightBox.BevelEdge(Face.Right | Face.Front, 3);
            standSideSupportRightBox.BevelEdge(Face.Right | Face.Back, 3);
            standSideSupportRightBox.BevelEdge(Face.Right | Face.Top, 3);
            standSideSupportRightBox.BevelEdge(Face.Left | Face.Front, 1);
            standSideSupportRightBox.BevelEdge(Face.Left | Face.Back, 1);
            CsgObject standSideSupportRight = standSideSupportRightBox;
            standSideSupportRight = new SetCenter(standSideSupportRight, standBase.GetCenter(), false, true, false);
            standSideSupportRight = new Align(standSideSupportRight, Face.Bottom | Face.Right, standBase, Face.Bottom | Face.Right);
            standTotal += standSideSupportRight;

            Box insideBevelRightBox = new Box(standYDepth, standYDepth, standYDepth, createCentered: false);
            insideBevelRightBox.CutAlongDiagonal(Face.Right | Face.Bottom);
            CsgObject insideBevelRight = insideBevelRightBox;
            insideBevelRight = new Align(insideBevelRight, Face.Right | Face.Front, standSideSupportRight, Face.Left | Face.Front, 1, 0, -.1);
            insideBevelRight = new Align(insideBevelRight, Face.Bottom, standBase, Face.Top);
            standTotal += insideBevelRight;

            CsgObject ringCutOut = new Cylinder(11, 6, Alignment.y);
            ringCutOut = new SetCenter(ringCutOut, standBase.GetCenter());
            ringCutOut = new Align(ringCutOut, Face.Bottom | Face.Back, standBase, Face.Top | Face.Front, 0, 5.9, -4);
            standTotal -= ringCutOut;

            pcbTotal = new SetCenter(pcbTotal, standBase.GetCenter(), true, true, false);
            pcbTotal = new Align(pcbTotal, Face.Bottom, standBase, Face.Top, offsetZ: -pcbHoldLip);
            standTotal -= pcbTotal;

            return standTotal;
        }

        CsgObject TrainConnector()
        {
            CsgObject total;
            CsgObject bar = new Box(20, 5.8, 12, createCentered: false, name: "link");
            bar = new SetCenter(bar, Vector3.Zero);
            total = bar;
            CsgObject leftHold = new Cylinder(11.7 / 2, 12, Alignment.z);
            leftHold = new SetCenter(leftHold, bar.GetCenter() + new Vector3(12, 0, 0));
            CsgObject rightHold = leftHold.NewMirrorAccrossX();
            total += leftHold;
            total += rightHold;
            
            return total;
        }

        public MatterCadGui(bool renderRayTrace)
            : base(800, 600)
        {
            CsgObject testObject = TrainConnector();
            // CsgObject testObject = PSEyeCameraHolder();
            //CsgObject testObject = ZenTableParts.ZenTableTestPart();
         //   testObject = Model.Flatten(testObject);

            //CsgObject testObject = BuildBot.B
            
         //   BoxCSG boxObject = new BoxCSG(20, 20, 20, "base box");
            //CsgObject testObject = YBedBeltClamp.CreateYBedBeltClamp();
            //CsgObject testObject = ZenTableParts.YRodMounts();
            //CsgObject testObject = new YRodMount(ZenTableUtilities.zenTableMajorAxisRodHeightAtCenterY);
            //CsgObject testObject = new MinorCariageMotorMount();
            //CsgObject testObject = new ZenTable.YIdler();
            //CsgObject testObject = YPlateBearingMount.CreateYPlateBearingMount();
            //CsgObject testObject = MakerbotZRodMounts();
            //CsgObject testObject = new MotorMount(MotorMount.MotorSize.Nema17, ZenTableUtilities.wallThickness);

            //WedgeCSG wedge = new WedgeCSG(new Vector2(), new Vector2(0, 25), new Vector2(10, 0), 5);

            //CsgObject testObject = Bevel.GetBevelSubtract(new Vector3(0, 0, 0), new Vector3(0, 0, 20), -Vector3.UnitX, -Vector3.UnitY, 3);
            //CsgObject testObject = Round.GetRound(new Vector3(0, 25, 0), new Vector3(0, 25, 20), -Vector3.UnitX, -Vector3.UnitY, 3);

            //testObject = new Union(testObject, wedge);

//            CsgObject testObject = MakerGearXCariage();

  //          OpenSCadOutput.Save(testObject, "test.scad", "");//, prepend: "%import_stl(\"mg.stl\", convexity = 5);\n");

            //OpenSCadOutput.Save(testObject, "test.scad", prepend: (new OutputNamedCenters("Mounting Hole", true)).LookForNamedPartRecursive((dynamic)testObject, Matrix4X4.Identity));

    //        OutputNamedCenters.Save(testObject, "Mounting Hole", "throug holes.txt", false);

            SuspendLayout();
            //CalculateIntersectCostsAndSaveToFile(); // you should do this after you optomize and then put the numbers back in.

            if (renderRayTrace)
            {
                matterCadGuiWidget = new MatterCadGuiWidget();
                AddChild(matterCadGuiWidget);
                matterCadGuiWidget.AnchorAll();
                AnchorAll();
            }
        }

        string GetStringForFile(string name, long timeMs, long overheadMs)
        {
            return "\r\n" + name + ": " + timeMs.ToString() + " : minus overhead = " + (timeMs - overheadMs).ToString();
        }


        [STAThread]
        public static void Main(string[] args)
        {
            //Clipboard.SetSystemClipboardFunctions(System.Windows.Forms.Clipboard.GetText, System.Windows.Forms.Clipboard.SetText, System.Windows.Forms.Clipboard.ContainsText);
            //MatterHackers.Agg.UI.Tests.UnitTests.Run();
            //MatterHackers.PolygonMesh.UnitTests.UnitTests.Run();

            MatterCadGui cadWindow = new MatterCadGui(true);
            cadWindow.UseOpenGL = true;
            cadWindow.Title = "MatterCADGui";

            cadWindow.ShowAsSystemWindow();
        }
    }
}
