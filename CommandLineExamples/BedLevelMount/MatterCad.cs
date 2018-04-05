using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Processors;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    static class SimplePartTester
    {
        static void BedLevelTestCode()
        {
            Vector3 x20y20 = new Vector3(20, 20, 21.56);
            Vector3 x120y20 = new Vector3(120, 20, 18.44);
            Vector3 x20y120 = new Vector3(20, 120, 21.03);

            Vector3 xPositive = (x120y20 - x20y20).GetNormal();
            Vector3 yPositive = (x20y120 - x20y20).GetNormal();
            Vector3 planNormal = Vector3.Cross(yPositive, xPositive);

            Matrix4X4 bedLevel = Matrix4X4.LookAt(Vector3.Zero, planNormal, yPositive);

            Vector3 fixedX20Y20 = Vector3.Transform(x20y20, bedLevel);
            Vector3 fixedX120Y20 = Vector3.Transform(x120y20, bedLevel);
            Vector3 fixedX20Y120 = Vector3.Transform(x20y120, bedLevel);
            double zAtX0Y0 = fixedX120Y20.z;

            Matrix4X4 inverseBedLevel = Matrix4X4.Invert(bedLevel);

            bedLevel = Matrix4X4.Identity;
            Matrix4X4 A = bedLevel;
            Matrix4X4 result = new Matrix4X4();
            double determinant = +A[0, 0] * (A[1, 1] * A[2, 2] - A[2, 1] * A[1, 2])
                        - A[0, 1] * (A[1, 0] * A[2, 2] - A[1, 2] * A[2, 0])
                        + A[0, 2] * (A[1, 0] * A[2, 1] - A[1, 1] * A[2, 0]);
            double invdet = 1 / determinant;
            result[0, 0] = (A[1, 1] * A[2, 2] - A[2, 1] * A[1, 2]) * invdet;
            result[0, 1] = -(A[0, 1] * A[2, 2] - A[0, 2] * A[2, 1]) * invdet;
            result[0, 2] = (A[0, 1] * A[1, 2] - A[0, 2] * A[1, 1]) * invdet;
            result[1, 0] = -(A[1, 0] * A[2, 2] - A[1, 2] * A[2, 0]) * invdet;
            result[1, 1] = (A[0, 0] * A[2, 2] - A[0, 2] * A[2, 0]) * invdet;
            result[1, 2] = -(A[0, 0] * A[1, 2] - A[1, 0] * A[0, 2]) * invdet;
            result[2, 0] = (A[1, 0] * A[2, 1] - A[2, 0] * A[1, 1]) * invdet;
            result[2, 1] = -(A[0, 0] * A[2, 1] - A[2, 0] * A[0, 1]) * invdet;
            result[2, 2] = (A[0, 0] * A[1, 1] - A[1, 0] * A[0, 1]) * invdet;

            Vector3 stepPositionX20Y20 = Vector3.Transform(fixedX20Y20, inverseBedLevel);
        }

        static double wallWidth = 4;
        static double switchHoleSeparation = 10;
        static double switchHoleDiameter = 2.5;

        static double switchHeight = 10;
        static double switchWidth = 20;
        static double armLength = 30;
        static double magnetHoldOffset = 15;
        static double pivotHeight = 11;
        static double pivotHoleRadius = 3.3;
        static double pivotRingRadius = pivotHoleRadius + wallWidth;
        static double magnetAttractorHoleRadius = 2.6;
        static double wireClearence = 6;
        static double magnetAttractorYSize = 8;
        static CsgObject OnOffArm()
        {
            CsgObject total;

            CsgObject mainBar = new Box(wallWidth * 1.5, 37, 2.5);
            total = mainBar;

            CsgObject magnetAttractorHole = new Cylinder(magnetAttractorHoleRadius / 2, mainBar.ZSize + .1, Alignment.z);
            magnetAttractorHole = new SetCenter(magnetAttractorHole, mainBar.GetCenter() + new Vector3(0, -magnetAttractorHoleRadius, 0));

            CsgObject magnetAttractorHole2 = new Cylinder(magnetAttractorHoleRadius / 2, mainBar.ZSize + .1, Alignment.z);
            magnetAttractorHole2 = new SetCenter(magnetAttractorHole2, mainBar.GetCenter() + new Vector3(0, magnetAttractorHoleRadius, 0));

            CsgObject bothHoles = magnetAttractorHole + magnetAttractorHole2;
            bothHoles = new Align(bothHoles, Face.Front, mainBar, Face.Front, offsetY: magnetAttractorHoleRadius);

            total -= bothHoles;

            CsgObject sideSupportBar = new Box(3, mainBar.YSize, mainBar.ZSize*2);
            sideSupportBar = new Align(sideSupportBar, Face.Left | Face.Bottom, mainBar, Face.Right | Face.Bottom, -.02);
            total += sideSupportBar;

            CsgObject pressSpot = new Box(total.XSize, 5, total.ZSize);
            pressSpot = new Align(pressSpot, Face.Left | Face.Back | Face.Bottom, total, Face.Left | Face.Back | Face.Bottom);
            total += pressSpot;

            return total;
        }

        static double mountPivotHeight = 15;
        static double backMagnetHeight = 30;
        static double baseMagnetHeight = 7.5;
        static CsgObject MountMount()
        {
            Vector3 magnetSize = new Vector3(10, 5, 2.5);

            CsgObject total;

            CsgObject baseBox = new Box(37, 26, wallWidth * 2);
            Round baseBevels = new Round(baseBox.Size);
            baseBevels.RoundEdge(Edge.LeftFront, 4);
            baseBevels.RoundEdge(Edge.LeftBack, 4);
            baseBox -= baseBevels;
            CsgObject fakeXCarriage = new Translate(baseBox, 0.5);
            total = fakeXCarriage;

            double pivotRingYOffset = -pivotHeight / 2 - wallWidth/2 - .5;
            CsgObject pivotRingFront = new Cylinder(pivotRingRadius / 2, wallWidth, Alignment.y);
            CsgObject pivotFrontWall = new Box(pivotRingRadius, wallWidth, mountPivotHeight);
            pivotFrontWall = new Translate(pivotFrontWall, 0, 0, -mountPivotHeight / 2);
            CsgObject pivotSupportFront = pivotRingFront + pivotFrontWall;
            CsgObject pivotHole = new Cylinder(pivotHoleRadius / 2, pivotRingFront.YSize + .1, Alignment.y);
            pivotHole = new SetCenter(pivotHole, pivotRingFront.GetCenter());
            pivotSupportFront -= pivotHole;
            pivotSupportFront = new Translate(pivotSupportFront, 0, pivotRingYOffset, mountPivotHeight);
            pivotSupportFront += Round.CreateFillet(pivotSupportFront, Face.Back, fakeXCarriage, Face.Top, 3);
            pivotSupportFront += Round.CreateFillet(pivotSupportFront, Face.Front, fakeXCarriage, Face.Top, 3);
            total += pivotSupportFront;

            CsgObject pivotSupportBack = pivotSupportFront.NewMirrorAccrossY();
            total += pivotSupportBack;

            CsgObject backMagnetHolder = new Box(wallWidth * 2, magnetSize.x + wallWidth, backMagnetHeight);
            backMagnetHolder = new Align(backMagnetHolder, Face.Bottom | Face.Right, fakeXCarriage, Face.Top | Face.Right, 0, 0, -.1);
            CsgObject backMagnetHole = new Box(magnetSize.z, magnetSize.x, magnetSize.y);
            backMagnetHole = new SetCenter(backMagnetHole, backMagnetHolder.GetCenter());
            backMagnetHole = new Align(backMagnetHole, Face.Left | Face.Top, backMagnetHolder, Face.Left | Face.Top, -.1, 0, -wallWidth / 2);

            total += Round.CreateFillet(backMagnetHolder, Face.Front, fakeXCarriage, Face.Top, 3);
            total += Round.CreateFillet(backMagnetHolder, Face.Back, fakeXCarriage, Face.Top, 3);
            total += Round.CreateFillet(backMagnetHolder, Face.Left, fakeXCarriage, Face.Top, 3);

            backMagnetHolder -= backMagnetHole;
            total += backMagnetHolder;

            CsgObject baseMagnetHolder = new Box(wallWidth * 2, magnetSize.x + wallWidth, baseMagnetHeight);
            baseMagnetHolder = new Align(baseMagnetHolder, Face.Bottom, fakeXCarriage, Face.Top, -14, 0, -.1);
            CsgObject baseMagnetHole = new Box(magnetSize.y, magnetSize.x, magnetSize.z);
            baseMagnetHole = new SetCenter(baseMagnetHole, baseMagnetHolder.GetCenter());
            baseMagnetHole = new Align(baseMagnetHole, Face.Top, baseMagnetHolder, Face.Top, 0, 0, .1);
            baseMagnetHolder -= baseMagnetHole;
            total += baseMagnetHolder;

            return total;
        }

        static CsgObject BedLevelMount()
        {
            // CSG object is a Constructive Solid Geometry Object (a basic part in our system for doing boolean operations).
            CsgObject totalMount;  // the csg object we will use as the master part.

            CsgObject pivotHole = new Cylinder(pivotHoleRadius / 2, pivotHeight + .1);

            CsgObject pivotMount = new Cylinder(pivotRingRadius / 2, pivotHeight);
            pivotMount = new Align(pivotMount, Face.Bottom, pivotHole, Face.Bottom, offsetZ: .02);
            totalMount = pivotMount;

            CsgObject holdArm = new Box(armLength, wallWidth * 2, wallWidth);
            holdArm = new Align(holdArm, Face.Left | Face.Front | Face.Bottom, pivotMount, Face.Left | Face.Front | Face.Bottom, offsetX: pivotRingRadius/2);
            totalMount += holdArm;

            CsgObject switchMount = new Box(switchHeight, switchWidth, wallWidth);
            switchMount -= new Translate(new Cylinder(switchHoleDiameter / 2, switchMount.ZSize + .1), new Vector3(0, switchHoleSeparation / 2, 0));
            switchMount -= new Translate(new Cylinder(switchHoleDiameter / 2, switchMount.ZSize + .1), new Vector3(0, -switchHoleSeparation / 2, 0));
            switchMount = new Align(switchMount, Face.Left | Face.Front | Face.Bottom, holdArm, Face.Right | Face.Front | Face.Bottom, offsetX: -.02, offsetY: -5);
            totalMount += switchMount;

            CsgObject magnetAttractor = new Box(wallWidth + magnetAttractorHoleRadius, magnetAttractorYSize, wallWidth * 3);
            // align it from the pivot point
            magnetAttractor = new Align(magnetAttractor, Face.Left | Face.Front | Face.Bottom, pivotMount, Face.Left | Face.Front | Face.Bottom, offsetX: magnetHoldOffset);
            totalMount += magnetAttractor;

            CsgObject bracingWall = new Box(wallWidth, wallWidth, pivotMount.ZSize - wallWidth);
            bracingWall = new SetCenter(bracingWall, holdArm.GetCenter());
            bracingWall = new Align(bracingWall, Face.Right | Face.Top, pivotMount, Face.Right | Face.Top);
            totalMount += bracingWall;

            CsgObject bracingArm = new Box(armLength - wireClearence, wallWidth, wallWidth);
            bracingArm = new SetCenter(bracingArm, holdArm.GetCenter());
            bracingArm = new Align(bracingArm, Face.Bottom | Face.Left, holdArm, Face.Top | Face.Left, offsetZ: -.1);
            totalMount += bracingArm;

            totalMount += Round.CreateFillet(bracingWall, Face.Right, bracingArm, Face.Top, 3);
            totalMount += Round.CreateFillet(bracingArm, Face.Right, holdArm, Face.Top, wallWidth);

            totalMount += Round.CreateFillet(holdArm, Face.Back, switchMount, Face.Left, wallWidth);
            totalMount += Round.CreateFillet(holdArm, Face.Front, switchMount, Face.Left, wallWidth);

            totalMount -= pivotHole;

            CsgObject magnetAttractorHole = new Cylinder(magnetAttractorHoleRadius / 2, magnetAttractor.YSize + .1, Alignment.y);
            magnetAttractorHole = new SetCenter(magnetAttractorHole, magnetAttractor.GetCenter() + new Vector3(0, 0, -magnetAttractorHoleRadius));
            totalMount -= magnetAttractorHole;

            CsgObject magnetAttractorHole2 = new Cylinder(magnetAttractorHoleRadius / 2, magnetAttractor.YSize + .1, Alignment.y);
            magnetAttractorHole2 = new SetCenter(magnetAttractorHole2, magnetAttractor.GetCenter() + new Vector3(0, 0, magnetAttractorHoleRadius));
            totalMount -= magnetAttractorHole2;

            return totalMount;
        }

        static void Main()
        {
            CsgObject bedLevelMount = BedLevelMount();
            OpenSCadOutput.Save(bedLevelMount, "z-probe-mount.scad");

            CsgObject onOffArm = OnOffArm();
            OpenSCadOutput.Save(onOffArm, "up-down-bar.scad");

            bedLevelMount += new Translate(new Rotate(onOffArm, MathHelper.DegreesToRadians(90)), 14, -5, 13);
            CsgObject mount = new Rotate(bedLevelMount, MathHelper.DegreesToRadians(90));
            CsgObject mountRotations = new Rotate(mount, y: MathHelper.DegreesToRadians(-90));
            mountRotations += new Rotate(mount, y: MathHelper.DegreesToRadians(-170));
            mountRotations = new Translate(mountRotations, 6, -3.5, -30);

            CsgObject bedLevelMountMount = MountMount();

            bedLevelMountMount += new Translate(mountRotations, -6, 3.5, 45, "%up part");

            OpenSCadOutput.Save(bedLevelMountMount, "x-carriage-addition.scad");
        }
    }
}
