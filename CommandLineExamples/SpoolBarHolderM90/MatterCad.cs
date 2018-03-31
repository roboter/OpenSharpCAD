using System;
using System.Diagnostics;
using MatterHackers.Csg;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    static class SimplePartTester
    {
        static double wallWidth = 4;
        static double mendal90FrameWidth = 6.5;
        static double clipReachZUp = 15;
        static double clipReachZDown = 20;
        static double clipLengthY = 30;
        static double rodDiameter = 8.5;
        static double attacheHoleDiameter = 3.3;

        static double attachHoleOffsetZ = 10;

        static CsgObject SpoolBarHolderM90()
        {
            CsgObject total;

            CsgObject centerSupport = new Box(mendal90FrameWidth + wallWidth * 2, clipLengthY, wallWidth);
            total = centerSupport;

            CsgObject clipWallLeft = new Box(wallWidth, clipLengthY, clipReachZDown);
            clipWallLeft = new Align(clipWallLeft, Face.Left | Face.Top, centerSupport, Face.Left | Face.Top);
            CsgObject attachHole = new Cylinder(attacheHoleDiameter/2, wallWidth + .2, Alignment.x);
            attachHole = new Align(attachHole, Face.Top, centerSupport, Face.Bottom, offsetZ: -attachHoleOffsetZ);
            attachHole = new SetCenter(attachHole, clipWallLeft.GetCenter(), onY: false, onZ: false);
            clipWallLeft -= attachHole;

            total += clipWallLeft;

            CsgObject clipWallRight = clipWallLeft.NewMirrorAccrossX();
            total += clipWallRight;

            Box spoolHoldBox = new Box(wallWidth, rodDiameter * 3, clipReachZUp);
            //spoolHoldBox.BevelEdge(Face.Front | Face.Top, rodDiameter/2);
            //spoolHoldBox.BevelEdge(Face.Back | Face.Top, rodDiameter / 2);
            CsgObject spoolHold = spoolHoldBox;
            spoolHold -= new Align(new Cylinder(rodDiameter / 2, wallWidth + 2, Alignment.x), Face.Top, spoolHold, Face.Top);
            spoolHold -= new Align(new Box(wallWidth + 2, rodDiameter, rodDiameter/2), Face.Top, spoolHold, Face.Top, offsetZ: .1);
            total += new Align(spoolHold, Face.Bottom, centerSupport, Face.Bottom);

            total += Round.CreateFillet(spoolHold, Face.Left, centerSupport, Face.Top, 2);
            total += Round.CreateFillet(spoolHold, Face.Right, centerSupport, Face.Top, 2);
            total -= Round.CreateBevel(centerSupport, Edge.LeftTop, 2);
            total -= Round.CreateBevel(centerSupport, Edge.RightTop, 2);

            return total;
        }

        static void Main()
        {
            CsgObject spoolBarHolderM90 = SpoolBarHolderM90();

            OpenSCadOutput.Save(spoolBarHolderM90, "SpoolBarHolderM90.scad");
        }
    }
}
