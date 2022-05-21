using MatterHackers.Csg; // our constructive solid geometry base classes
using MatterHackers.Csg.Processors;
using MatterHackers.Csg.Solids;

namespace SimplePartScripting
{
    static class SimplePartTester
    {
        static CsgObject TrackConnecor()
        {
            // CsgObject is our base class for all constructive solid geometry primitives
            CsgObject total;

           CsgObject extrude = new LinearExtrude(new double[] { 0, 10, 10, 10, 10, 0, 0, 0}, 10,new Alignment(), 10, "test" );
            //// we create a box object and name it 'link'
            //CsgObject bar = new Box(20, 5.8, 12, "link");

            //// we set it's center to the center of the coordinate system
            //bar = new SetCenter(bar, Vector3.Zero);
            //// and we make the total = the only object we have at this time
            //total = bar;
            total = extrude;

            //// now we make a cyliner for the side
            //CsgObject leftHold = new Cylinder(11.7 / 2, 12, Alignment.z);
            //// position it where we want it
            //leftHold = new SetCenter(leftHold, bar.GetCenter() + new Vector3(12, 0, 0));
            //// and make another one on the other side by mirroring
            //CsgObject rightHold = leftHold.NewMirrorAccrossX();
            //// this is the way we actuall decide what boolean operation to put them together with
            //total += leftHold; 
            //total += rightHold;

            return total;  // and pass it back to the caller
        }

        static void Main()
        {
            // an internal function to save as an .scad file
            OpenSCadOutput.Save(TrackConnecor(), "TrackConnector.scad");
        }
    }
}