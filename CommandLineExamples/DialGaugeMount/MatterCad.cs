using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Processors;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    static class SimplePartTester
    {
        static double xCarriageDistanceBetweenBars = 47.8;
        static double xCarriageBarWidth = 8;
        static double xCarriageSeparation = xCarriageDistanceBetweenBars + xCarriageBarWidth;
        static double diameterOfDialGaugeBar = 9.5 + .3;
        static double wallWidth = 4;
        static double wallHeight = 15;
        static double dialGaugeBarOffsetFromCenter = 10;

        static CsgObject DialGaugeMount()
        {
            // CSG object is a Constructive Solid Geometry Object (a basic part in our system for doing boolean operations).
            CsgObject dialGauge;  // the csg object we will use as the master part.

            CsgObject frontBarRide = new Box(50, xCarriageBarWidth + 4, xCarriageBarWidth / 2 + 4);
            CsgObject barGroove = new Cylinder(xCarriageBarWidth/2, frontBarRide.XSize + .2, 2);
            barGroove = new Translate(barGroove, 0, 0, -barGroove.ZSize / 2);
            frontBarRide -= barGroove;
            dialGauge = frontBarRide;

            CsgObject backBarRide = frontBarRide.NewMirrorAccrossY();
            backBarRide = new Translate(backBarRide, 0, xCarriageSeparation, 0);
            dialGauge += backBarRide;

            CsgObject frontSideWall = new Box(frontBarRide.XSize, wallWidth, wallHeight);
            frontSideWall = new Align(frontSideWall, Face.Front | Face.Top, frontBarRide, Face.Back | Face.Top, offsetY: -.02); // offset it to ensure good stl geometry
            dialGauge += frontSideWall;

            CsgObject backSideWall = new Box(frontBarRide.XSize, wallWidth, wallHeight);
            backSideWall = new Align(backSideWall, Face.Back | Face.Top, backBarRide, Face.Front | Face.Top, offsetY: .02); // offset it to ensure good stl geometry
            dialGauge += backSideWall;

            // make the plate on the bottom
            double distanceAccrossBottom = backSideWall.GetAxisAlignedBoundingBox().MinXYZ.Y - frontSideWall.GetAxisAlignedBoundingBox().MaxXYZ.Y;
            CsgObject dialBase = new Box(frontBarRide.XSize, distanceAccrossBottom + .02, wallWidth); // make it bigger so it is manifold
            dialBase = new Align(dialBase, Face.Front | Face.Bottom, frontSideWall, Face.Back | Face.Bottom, offsetY: .01);
            dialGauge += dialBase;
            
            // an some side walls for strength
            CsgObject leftSideWall = new Box(wallWidth, dialBase.YSize, frontSideWall.ZSize, "#leftsidewall");
            leftSideWall = new Align(leftSideWall, Face.Left | Face.Bottom | Face.Front, dialBase, Face.Left | Face.Bottom | Face.Front);
            dialGauge += leftSideWall;

            CsgObject rightSideWall = new Box(wallWidth, dialBase.YSize, frontSideWall.ZSize, "#rightsidewall");
            rightSideWall = new Align(rightSideWall, Face.Right | Face.Bottom | Face.Front, dialBase, Face.Right | Face.Bottom | Face.Front);
            dialGauge += rightSideWall;

            CsgObject dialGaugeHole = new Cylinder(diameterOfDialGaugeBar / 2, dialBase.ZSize + .02, 2);
            dialGaugeHole = new SetCenter(dialGaugeHole, dialBase.GetCenter() + new Vector3(0, dialGaugeBarOffsetFromCenter, 0));
            dialGauge -= dialGaugeHole;

            CsgObject bridgeSupport = new Box(dialBase.XSize, dialBase.YSize, .5, "#test");
            bridgeSupport = new SetCenter(bridgeSupport, dialBase.GetCenter());
            bridgeSupport = new Align(bridgeSupport, Face.Top, dialBase, Face.Top);
            dialGauge += bridgeSupport;

            return dialGauge;
        }

        static void Main()
        {
            CsgObject dialGauge = DialGaugeMount();
            OpenSCadOutput.Save(dialGauge, "DialGaugeMount.scad");
            OpenSCadOutput.Save(new Rotate(dialGauge, 0, MathHelper.DegreesToRadians(180)), "DialGaugeMountRotatedForPrint.scad");
        }
    }
}
