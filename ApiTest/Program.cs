﻿using MatterHackers.Csg;
using MatterHackers.Csg.Processors;
using MatterHackers.Csg.Solids;
using MatterHackers.VectorMath;

namespace ApiTest
{
    class Program
    {

        private static CsgObject Render()
        {
            CsgObject box = new Box(100, 100, 100);
            //return box;
            //CsgObject cylinder = new Cylinder(50, 50, 50);
            // return cylinder;
            //CsgObject tor = new Torus(10, 20);
            //return tor;

            Round round = new Round(100, 100, 100);
            round.RoundFace(Face.Front, 10);
            // return box-round;

            return new LinearExtrude(new[] { new Vector2(0, 0), new Vector2(0,10), new Vector2(10,10) }, 10);
        }

        static void Main()
        {
            CsgObject bedLevelMount = Render();
            OpenSCadOutput.Save(bedLevelMount, "test.scad");
        }
    }
}
