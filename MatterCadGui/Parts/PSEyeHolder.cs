using System.Diagnostics;
using MatterHackers.Csg;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Processors;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    class PSEyeHolderPartTester
    {
        public static CsgObject SimplePartFunction()
        {
            double pcbHoldLip = 2;
            double standYDepth = 8;
            double pcbYDepth = 2;
            CsgObject pcbTotal;

            CsgObject pcbBottom = new Box(23, pcbYDepth, 40);
            pcbTotal = pcbBottom;

            CsgObject pcbMid = new Box(30, pcbYDepth, 34.5);
            pcbMid = new Align(pcbMid, Face.Top, pcbBottom, Face.Top);
            pcbMid = new SetCenter(pcbMid, pcbBottom.GetCenter(), true, false, false);
            pcbTotal += pcbMid;

            CsgObject pcbTop = new Box(64.5, pcbYDepth, 21.5);
            pcbTop = new Align(pcbTop, Face.Top, pcbBottom, Face.Top);
            pcbTop = new SetCenter(pcbTop, pcbBottom.GetCenter(), true, false, false);
            pcbTotal += pcbTop;

            Box standBaseBox = new Box(pcbTop.XSize + 2 * (standYDepth - pcbHoldLip), 37, standYDepth);
            //standBaseBox.BevelFace(Face.Top, 3);
            //standBaseBox.BevelEdge(Face.Left | Face.Front, 3);
            //standBaseBox.BevelEdge(Face.Left | Face.Back, 3);
            //standBaseBox.BevelEdge(Face.Right | Face.Front, 3);
            //standBaseBox.BevelEdge(Face.Right | Face.Back, 3);
            CsgObject standBase = standBaseBox;
            CsgObject standTotal = standBase;

            Box standSideSupportLeftBox = new Box(standYDepth, standYDepth, pcbBottom.ZSize + standBase.ZSize - 10);
            //standSideSupportLeftBox.BevelEdge(Face.Left | Face.Front, 3);
            //standSideSupportLeftBox.BevelEdge(Face.Left | Face.Back, 3);
            //standSideSupportLeftBox.BevelEdge(Face.Left | Face.Top, 3);
            //standSideSupportLeftBox.BevelEdge(Face.Right | Face.Front, 1);
            //standSideSupportLeftBox.BevelEdge(Face.Right | Face.Back, 1);
            CsgObject standSideSupportLeft = standSideSupportLeftBox;
            standSideSupportLeft = new SetCenter(standSideSupportLeft, standBase.GetCenter(), false, true, false);
            standSideSupportLeft = new Align(standSideSupportLeft, Face.Bottom | Face.Left, standBase, Face.Bottom | Face.Left);
            standTotal += standSideSupportLeft;

            Box insideBevelLeftBox = new Box(standYDepth, standYDepth, standYDepth);
            insideBevelLeftBox.CutAlongDiagonal(Face.Left | Face.Bottom);
            CsgObject insideBevelLeft = insideBevelLeftBox;
            insideBevelLeft = new Align(insideBevelLeft, Face.Left | Face.Front, standSideSupportLeft, Face.Right | Face.Front, -1, 0, -.1);
            insideBevelLeft = new Align(insideBevelLeft, Face.Bottom, standBase, Face.Top);
            standTotal += insideBevelLeft;

            Box standSideSupportRightBox = new Box(standYDepth, standYDepth, pcbBottom.ZSize + standBase.ZSize - 10);
            //standSideSupportRightBox.BevelEdge(Face.Right | Face.Front, 3);
            //standSideSupportRightBox.BevelEdge(Face.Right | Face.Back, 3);
            //standSideSupportRightBox.BevelEdge(Face.Right | Face.Top, 3);
            //standSideSupportRightBox.BevelEdge(Face.Left | Face.Front, 1);
            //standSideSupportRightBox.BevelEdge(Face.Left | Face.Back, 1);
            CsgObject standSideSupportRight = standSideSupportRightBox;
            standSideSupportRight = new SetCenter(standSideSupportRight, standBase.GetCenter(), false, true, false);
            standSideSupportRight = new Align(standSideSupportRight, Face.Bottom | Face.Right, standBase, Face.Bottom | Face.Right);
            standTotal += standSideSupportRight;

            Box insideBevelRightBox = new Box(standYDepth, standYDepth, standYDepth);
            insideBevelRightBox.CutAlongDiagonal(Face.Right | Face.Bottom);
            CsgObject insideBevelRight = insideBevelRightBox;
            insideBevelRight = new Align(insideBevelRight, Face.Right | Face.Front, standSideSupportRight, Face.Left | Face.Front, 1, 0, -.1);
            insideBevelRight = new Align(insideBevelRight, Face.Bottom, standBase, Face.Top);
            standTotal += insideBevelRight;

            CsgObject ringCutOut = new Cylinder(11, 6, 32, Alignment.y);
            ringCutOut = new SetCenter(ringCutOut, standBase.GetCenter());
            ringCutOut = new Align(ringCutOut, Face.Bottom | Face.Back, standBase, Face.Top | Face.Front, 0, 5.9, -4);
            standTotal -= ringCutOut;

            pcbTotal = new SetCenter(pcbTotal, standBase.GetCenter(), true, true, false);
            pcbTotal = new Align(pcbTotal, Face.Bottom, standBase, Face.Top, offsetZ: -pcbHoldLip);
            standTotal -= pcbTotal;

            return standTotal;
        }

        static void Main()
        {
            CsgObject part = SimplePartFunction();
            OpenSCadOutput.Save(part, "temp.scad");

            System.Console.WriteLine("Output the file to 'temp.scad'.");
        }
    }
}
