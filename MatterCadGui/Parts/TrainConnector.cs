using System.Diagnostics;
using MatterHackers.Csg;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Processors;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    class SimplePartTester
    {
        public static CsgObject SimplePartFunction()
        {
            CsgObject total;
            CsgObject bar = new Box(20, 5.8, 12, "link");
            bar = new SetCenter(bar, Vector3.Zero);
            total = bar;
            CsgObject leftHold = new Cylinder(11.7 / 2, 12, Alignment.z);
            leftHold = new SetCenter(leftHold, bar.GetCenter() + new Vector3(12, 0, 0));
            CsgObject rightHold = leftHold.NewMirrorAccrossX();
            total += leftHold;
            total += rightHold;

            return total;
        }

        static void Main()
        {
            CsgObject part = SimplePartFunction();
            OpenSCadOutput.Save(part, "temp.scad");

            System.Console.WriteLine("Output the file to 'temp.scad'.");
        }
    }
}
