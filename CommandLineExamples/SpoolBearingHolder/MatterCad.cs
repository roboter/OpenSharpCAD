using System;
using System.Diagnostics;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Processors;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    public abstract class SpoolType
    {
        public double spoolDiameter;
        public double bearingDiameter;
        public double bearingHeight;
        public double retainingWallHeight;
        public double wallWidth;

        public SpoolType()
        {
            bearingDiameter = 22;
            bearingHeight = 7;
            retainingWallHeight = bearingHeight;
            wallWidth = 4;
        }
    }

    public class EsunSmallSpool : SpoolType
    {
        public EsunSmallSpool()
        {
            spoolDiameter = 32;
        }
    }

    public class PackingBagSpool : SpoolType
    {
        public PackingBagSpool()
        {
            retainingWallHeight = bearingHeight + 10;
            wallWidth = 6;
            spoolDiameter = 75;
        }
    }

    static class CreateBearingHolder
    {
        //static double spoolDiameter = 40;

        static CsgObject SpoolBearingHolder(SpoolType spoolType)
        {
            // CSG object is a Constructive Solid Geometry Object (a basic part in our system for doing boolean operations).
            CsgObject spoolBearingHolder;  // the csg object we will use as the master part.

            spoolBearingHolder = new Cylinder(spoolType.spoolDiameter / 2 + 1, spoolType.spoolDiameter / 2 - 1, spoolType.retainingWallHeight + spoolType.wallWidth);

            CsgObject insideHole = new Cylinder(spoolType.spoolDiameter / 2 - spoolType.wallWidth, spoolType.retainingWallHeight);
            insideHole = new Align(insideHole, Face.Top, spoolBearingHolder, Face.Top, offsetZ: .02);
            spoolBearingHolder -= insideHole;

            CsgObject spoolHoldLip = new Cylinder(spoolType.spoolDiameter / 2 + spoolType.wallWidth, spoolType.wallWidth);
            spoolHoldLip = new Align(spoolHoldLip, Face.Bottom, spoolBearingHolder, Face.Bottom);
            spoolBearingHolder += spoolHoldLip;

            CsgObject bearingHolder = new Cylinder((spoolType.bearingDiameter + spoolType.wallWidth) / 2, spoolType.bearingHeight + spoolType.wallWidth);
            bearingHolder = new Align(bearingHolder, Face.Bottom, spoolBearingHolder, Face.Bottom);
            spoolBearingHolder += bearingHolder;

            CsgObject bearingHole = new Cylinder(spoolType.bearingDiameter / 2, spoolType.bearingHeight);
            bearingHole = new Align(bearingHole, Face.Bottom, spoolBearingHolder, Face.Bottom, offsetZ: -.02);
            spoolBearingHolder -= bearingHole;

            CsgObject revolution = new RotateExtrude(new double[] { 0, -.02, spoolType.wallWidth, 0, 0, spoolType.wallWidth + .02 }, spoolType.bearingDiameter / 2 - spoolType.wallWidth);
            revolution = new Align(revolution, Face.Top, bearingHolder, Face.Top);

            spoolBearingHolder -= revolution;

            CsgObject rodHole = new Cylinder(spoolType.bearingDiameter / 2 - spoolType.wallWidth, spoolBearingHolder.ZSize + 2);
            rodHole = new Align(rodHole, Face.Top, spoolBearingHolder, Face.Top, offsetZ: .02);
            spoolBearingHolder -= rodHole;

            return spoolBearingHolder;
        }

        static void Main()
        {
            OpenSCadOutput.Save(SpoolBearingHolder(new EsunSmallSpool()), "SpoolBearingHolder.scad");
            OpenSCadOutput.Save(SpoolBearingHolder(new PackingBagSpool()), "PackingBagsBearingHolder.scad");
        }
    }
}
