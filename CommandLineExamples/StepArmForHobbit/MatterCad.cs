using System;
using System.Diagnostics;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Processors;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    static class SimplePartTester
    {
        static double wallWidth = 4;

        static double armLength = 40;
        static double pivotHeight = 11;
        static double pivotHoleRadius = 3.3;
        static double pivotRingRadius = pivotHoleRadius + wallWidth;

        static CsgObject StepArm()
        {
            CsgObject totalMount;  // the csg object we will use as the master part.

            CsgObject pivotHole = new Cylinder(pivotHoleRadius / 2, pivotHeight + .1 ,2);

            CsgObject pivotMount = new Cylinder(pivotRingRadius / 2, pivotHeight, 2);
            pivotMount = new Align(pivotMount, Face.Bottom, pivotHole, Face.Bottom, offsetZ: .02);
            totalMount = pivotMount;

            CsgObject holdArm = new Box(armLength, pivotRingRadius, wallWidth);
            holdArm = new Align(holdArm, Face.Left | Face.Front | Face.Bottom, pivotMount, Face.Left | Face.Front | Face.Bottom, offsetX: pivotRingRadius / 2);
            totalMount += holdArm;

            CsgObject bracingWall = new Box(wallWidth, wallWidth, pivotMount.ZSize - wallWidth);
            bracingWall = new SetCenter(bracingWall, holdArm.GetCenter());
            bracingWall = new Align(bracingWall, Face.Right | Face.Top, pivotMount, Face.Right | Face.Top);
            totalMount += bracingWall;

            CsgObject bracingArm = new Box(armLength, wallWidth, wallWidth);
            bracingArm = new SetCenter(bracingArm, holdArm.GetCenter());
            bracingArm = new Align(bracingArm, Face.Bottom | Face.Left, holdArm, Face.Top | Face.Left, offsetZ: -.1);
            totalMount += bracingArm;

            totalMount += Round.CreateFillet(bracingWall, Face.Right, bracingArm, Face.Top, 3);
            //totalMount += Round.CreateFillet(bracingArm, Face.Right, holdArm, Face.Top, wallWidth);

            double toothSelectorSize = 4;
            double diagonalSize = Math.Sqrt(toothSelectorSize * toothSelectorSize / 2);
            CsgObject toothSelectorBack = new Box(diagonalSize*2, diagonalSize*2, wallWidth * 2);
            toothSelectorBack = new Align(toothSelectorBack, Face.Right | Face.Front | Face.Bottom, totalMount, Face.Right | Face.Front | Face.Bottom);
            totalMount += toothSelectorBack;

            CsgObject toothSelector = new Box(toothSelectorSize, toothSelectorSize, wallWidth * 2);
            toothSelector = new Rotate(toothSelector, 0, 0, MathHelper.Tau / 8);
            toothSelector = new Align(toothSelector, Face.Right | Face.Front | Face.Bottom, totalMount, Face.Right | Face.Front | Face.Bottom, offsetY: -diagonalSize);
            totalMount += toothSelector;


            totalMount -= pivotHole;

            return totalMount;
        }

        static void Main()
        {
            OpenSCadOutput.Save(StepArm(), "hobbit step arm.scad");
        }
    }
}
