using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace Net3dBoolDemo2
{

    public class TDemoWindow : TGameWindow
    {

        public Net3dBool.Solid mesh;

        public override void CreateMesh()
        {

            var line1 = new Net3dBool.Line(
                new Net3dBool.Face(
                    new Net3dBool.Vertex(new Net3dBool.Point3d(0,0,0), new Net3dBool.Color3f(10,20,30)),
                    new Net3dBool.Vertex(new Net3dBool.Point3d(0, 0, 0), new Net3dBool.Color3f(10, 20, 30)),
                    new Net3dBool.Vertex(new Net3dBool.Point3d(0, 0, 0), new Net3dBool.Color3f(10, 20, 30))
                    ),
                new Net3dBool.Face(
                    new Net3dBool.Vertex(new Net3dBool.Point3d(0, 0, 0), new Net3dBool.Color3f(10, 20, 30)),
                    new Net3dBool.Vertex(new Net3dBool.Point3d(0, 0, 0), new Net3dBool.Color3f(10, 20, 30)),
                    new Net3dBool.Vertex(new Net3dBool.Point3d(0, 0, 0), new Net3dBool.Color3f(10, 20, 30))
                    ));

              var box = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_BOX_VERTICES, Net3dBool.DefaultCoordinates.DEFAULT_BOX_COORDINATES, getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_BOX_VERTICES.Length, Color.Red));
            var sphere = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_SPHERE_VERTICES, Net3dBool.DefaultCoordinates.DEFAULT_SPHERE_COORDINATES, getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_SPHERE_VERTICES.Length, Color.Red));
            sphere.Scale(0.68, 0.68, 0.68);

            var cylinder1 = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES, Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_COORDINATES, getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES.Length, Color.Green));
            cylinder1.Scale(0.38, 1, 0.38);

            var cylinder2 = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES, Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_COORDINATES, getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES.Length, Color.Green));
            cylinder2.Scale(0.38, 1, 0.38);
            cylinder2.Rotate(Math.PI / 2, 0, 0);

            var cylinder3 = new Net3dBool.Solid(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES, Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_COORDINATES, getColorArray(Net3dBool.DefaultCoordinates.DEFAULT_CYLINDER_VERTICES.Length, Color.Green));
            cylinder3.Scale(0.38, 1, 0.38);
            cylinder3.Rotate(Math.PI / 2, 0, 0);
            cylinder3.Rotate(0, Math.PI / 2, 0);

             var modeller = new Net3dBool.BooleanModeller(box, sphere);
          //  var modeller = line1;
            mesh = modeller.getDifference();


            //    var modeller = new Net3dBool.BooleanModeller(box, sphere);
            mesh = modeller.getIntersection();

            modeller = new Net3dBool.BooleanModeller(mesh, cylinder1);
           mesh = modeller.getDifference();

            modeller = new Net3dBool.BooleanModeller(mesh, cylinder2);
            mesh = modeller.getDifference();

            modeller = new Net3dBool.BooleanModeller(mesh, cylinder3);
            mesh = modeller.getDifference();
        }

        public override void RenderMesh()
        {
            GL.Begin(PrimitiveType.Triangles);

            var verts = mesh.GetVertices();
            int[] ind = mesh.GetIndices();
            Net3dBool.Color3f[] colors = mesh.GetColors();

            for (var i = 0; i < ind.Length; i = i + 3)
            {
                GL.Normal3(new Vector3(1, 1, 1));
                var p = verts[ind[i]];
                var c = colors[ind[i]];
                GL.Color3(c.r, c.g, c.b);
                GL.Color3(Color.Red);
                GL.Vertex3(new Vector3d(p.x, p.y, p.z));

                p = verts[ind[i + 1]];
                c = colors[ind[i + 1]];
                GL.Color3(c.r, c.g, c.b);
                GL.Color3(Color.Blue);
                GL.Vertex3(new Vector3d(p.x, p.y, p.z));

                p = verts[ind[i + 2]];
                c = colors[ind[i + 2]];
                GL.Color3(c.r, c.g, c.b);
                GL.Color3(Color.Green);
                GL.Vertex3(new Vector3d(p.x, p.y, p.z));
            }

            GL.End();
        }

    }
}

