using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Solids;
using MatterHackers.RenderOpenGl;
using MatterHackers.Agg;

namespace CSharpCAD
{
    public static class CadShapes
    {
        public static void Render()
        {
            MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();
            mesh.Vertices.Add(new Vector3Float(1, 0, 1));  // V0
            mesh.Vertices.Add(new Vector3Float(1, 0, -1)); // V1
            mesh.Vertices.Add(new Vector3Float(-1, 0, -1)); // V2
            mesh.Vertices.Add(new Vector3Float(-1, 0, 1)); // V3
            mesh.Vertices.Add(new Vector3Float(0, 1, 0)); // V4

            mesh.Faces.Add(0, 1, 2, mesh.Vertices);
            mesh.Faces.Add(0, 2, 3, mesh.Vertices);

            mesh.Faces.Add(3, 0, 4, mesh.Vertices);
            mesh.Faces.Add(0, 1, 4, mesh.Vertices);
            mesh.Faces.Add(1, 2, 4, mesh.Vertices);
            mesh.Faces.Add(2, 3, 4, mesh.Vertices);

            GLHelper.Render(mesh, new Color(0.3, 0.8, 0.7));
        }

        public static void Box(this Union rootUnion, double sizeX, double sizeY, double sizeZ)
        {
            rootUnion.Add(new Box(sizeX, sizeY, sizeZ));
        }
    }
}
