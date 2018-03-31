using System;
using System.Diagnostics;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Processors;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    public class PowerSupplyMountingFoot
    {
        static double wallWidth = 4;
        static double footBevelRadius = 2;
        static double wallBevelRadius = 2;
        static double footMountHoleDiameter = 3.3;
        static double powerSupplyHoleDiameter = 4.2;

        static double powerSupplyHoleCenterHeight = 11.5;

        static CsgObject MakeOne()
        {
            CsgObject total = null;

            Box footBaseBox = new Box(10 + wallWidth, wallWidth * 2 + footMountHoleDiameter, wallWidth);
            footBaseBox.BevelEdge(Face.Left | Face.Front, footBevelRadius);
            footBaseBox.BevelEdge(Face.Left | Face.Back, footBevelRadius);
            CsgObject footBase = footBaseBox;
            CsgObject mountHole = new Cylinder(footMountHoleDiameter/2, footBase.ZSize + .2);
            mountHole = new Align(mountHole, Face.Left, footBase, Face.Left, wallWidth);
            footBase -= mountHole;
            total = footBase;

            Box risingWallBox = new Box(wallWidth, footBase.YSize, powerSupplyHoleCenterHeight + powerSupplyHoleDiameter / 2 + wallWidth * 2);
            risingWallBox.BevelEdge(Face.Top | Face.Back, wallBevelRadius);
            risingWallBox.BevelEdge(Face.Top | Face.Front, wallBevelRadius);
            CsgObject risingWall = risingWallBox;
            risingWall = new Align(risingWall, Face.Right | Face.Bottom, footBase, Face.Right | Face.Bottom);
            CsgObject powerScrewHole = new Cylinder(powerSupplyHoleDiameter / 2, footBase.ZSize + .2, Alignment.x);
            powerScrewHole = new Align(powerScrewHole, Face.Top | Face.Right, risingWall, Face.Top | Face.Right, offsetX: .02, offsetZ: -wallWidth);
            risingWall -= powerScrewHole;

            total += risingWall;

            return total;
        }

        static void Main()
        {
            OpenSCadOutput.Save(MakeOne(), "PowerSupplyMountingFoot.scad");

            double spacing = 5;
            CsgObject foot = MakeOne();
            CsgObject total = foot;
            total += new Translate(foot, foot.XSize + spacing);
            total += new Translate(foot, 0, foot.YSize + spacing);
            total += new Translate(foot, foot.XSize + spacing, foot.YSize + spacing);
            OpenSCadOutput.Save(total, "PowerSupplyMountingFoot 4.scad");
        }
    }
}
