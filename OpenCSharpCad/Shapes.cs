using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Solids;
using MatterHackers.RenderOpenGl;
using MatterHackers.Agg;

namespace OpenCSharpCad
{
    public static class CadShapes
    {
        public static void Render()
        {
            MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();
            mesh.Vertices.Add(new Vector3(1, 0, 1));  // V0
            mesh.Vertices.Add(new Vector3(1, 0, -1)); // V1
            mesh.Vertices.Add(new Vector3(-1, 0, -1)); // V2
            mesh.Vertices.Add(new Vector3(-1, 0, 1)); // V3
            mesh.Vertices.Add(new Vector3(0, 1, 0)); // V4

            mesh.Faces.Add(0, 1, 2, mesh.Vertices);
            mesh.Faces.Add(2, 3, 0, mesh.Vertices);
            mesh.Faces.Add(3, 0, 4, mesh.Vertices);
            mesh.Faces.Add(0, 1, 4, mesh.Vertices);
            mesh.Faces.Add(1, 2, 4, mesh.Vertices);
            mesh.Faces.Add(2, 3, 4, mesh.Vertices);

            // RenderMeshToGl.Render(mesh, new Color(.3, .8, 7));
        }

        public static void Box(this Union rootUnion, double sizeX, double sizeY, double sizeZ)
        {
            rootUnion.Add(new Box(sizeX, sizeY, sizeZ));
        }

        //public 
    }
}
