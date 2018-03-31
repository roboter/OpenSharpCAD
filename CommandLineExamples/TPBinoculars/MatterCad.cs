using System;
using System.Diagnostics;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Processors;
using MatterHackers.VectorMath;

namespace SimplePartScripting
{
    static class SimplePartTester
    {
        static double centerXSize = 15;

        static CsgObject TPBinoculars()
        {
            CsgObject total;

            Box centerBarHoldBox = new Box(centerXSize + 10, 30, 10);
            centerBarHoldBox.BevelFace(Face.Front, 3);
            CsgObject centerBarHold = centerBarHoldBox;
            CsgObject leftTube = new Align(LeftTube(), Face.Front, centerBarHold, Face.Front, 0, 4);
            CsgObject rightTube = new Align(RightTube(), Face.Front, centerBarHold, Face.Front, 0, 4);
            centerBarHold -= leftTube;
            centerBarHold -= rightTube;
            total = centerBarHold;

            Box centerBarBox = new Box(centerXSize + 5, 30, 10);
            centerBarBox.BevelFace(Face.Back, 3);
            CsgObject centerBar = centerBarBox;
            centerBar -= new Align(LeftTube(false), Face.Front, centerBar, Face.Front, 0, -.1);
            centerBar -= new Align(RightTube(false), Face.Front, centerBar, Face.Front, 0, -.1);
            centerBar = new Align(centerBar, Face.Front, centerBarHold, Face.Back, offsetY: -1);
            total += centerBar;

            CsgObject noseSpace = new Cylinder(centerXSize / 2 + .5, centerXSize / 2 - 2, centerBarHoldBox.ZSize + 1, Alignment.z);
            noseSpace = new Align(noseSpace, Face.Front, centerBarHold, Face.Front, 0, -noseSpace.YSize/2);
            total -= noseSpace;

            CsgObject spinner = new Cylinder(6, 10, Alignment.y);
            CsgObject spinnerRidge = new Cylinder(.5, spinner.YSize - 2 , Alignment.y);
            spinnerRidge = new Translate(spinnerRidge, spinner.XSize / 2);
            for (int i = 0; i <= 10; i++)
            {
                spinner += new Rotate(spinnerRidge, 0, MathHelper.DegreesToRadians(-18 * i));
            }

            spinner = new Translate(spinner, 0, 25, 3);
            CsgObject spinnerGap = new Box(13, spinner.YSize + 2, 1);
            spinnerGap = new Align(spinnerGap, Face.Top, centerBar, Face.Top, offsetZ: .1);
            spinnerGap = new Align(spinnerGap, Face.Front, spinner, Face.Front, offsetY: -1);
            total -= spinnerGap;
            total += spinner;

            total += new Translate(new Sphere(2), -3, 5, 4);
            total += new Translate(new Sphere(2), 3, 5, 4);
            total += new Translate(new Sphere(4), 0, 37, 4);
            //total -= new Translate(new Sphere(4), 1, 37, 9);
            total += new Translate(new Sphere(4), 0, -3, 5);
            //total -= new Translate(new Sphere(4), -1, -3, 11);

            total -= new Translate(new Sphere(4), 2, 12, 6);
            total -= new Translate(new Sphere(4), 1, 12, 6);
            total -= new Translate(new Sphere(4), 0, 12, 6);
            total -= new Translate(new Sphere(4), -1, 12, 6);
            total -= new Translate(new Sphere(4), -2, 12, 6);
            total += new Translate(new Sphere(4), 0, 12, 1);

            total += new Translate(leftTube, name: "%see only");
            total += new Translate(rightTube, name: "%see only");

            return total;
        }

        static CsgObject LeftTube(bool hollow = true)
        {
            CsgObject tPTube = new Cylinder(40 / 2, 105, Alignment.y);
            if (hollow)
            {
                tPTube -= new Cylinder(40 / 2 - 1, 106, Alignment.y);
            }
            tPTube = new Translate(tPTube, -(tPTube.XSize / 2 + centerXSize / 2));
            return tPTube;
        }

        static CsgObject RightTube(bool hollow = true)
        {
            CsgObject tPTube = LeftTube(hollow);
            tPTube = new Translate(tPTube, (tPTube.XSize + centerXSize));
            return tPTube;
        }

        static void Main()
        {
            Matrix4X4 rotateAboutZ = Matrix4X4.CreateRotationZ(MathHelper.DegreesToRadians(2));

            CsgObject tPBinoculars = TPBinoculars();

            OpenSCadOutput.Save(tPBinoculars, "TPBinoculars.scad");
        }
    }
}
