using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Solids;
using MatterHackers.RenderOpenGl;
using MatterHackers.Agg;

namespace OpenSharpCAD
{
    public static class CadShapes
    {
        public static void Render()
        {
            MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();
            var v0 = mesh.CreateVertex(new Vector3(1, 0, 1));  // V0
            var v1 = mesh.CreateVertex(new Vector3(1, 0, -1)); // V1
            var v2 = mesh.CreateVertex(new Vector3(-1, 0, -1)); // V2
            var v3 = mesh.CreateVertex(new Vector3(-1, 0, 1)); // V3
            var v4 = mesh.CreateVertex(new Vector3(0, 1, 0)); // V4

            mesh.CreateFace(new Vertex[] { v0, v1, v2, v3 });
            mesh.CreateFace(new Vertex[] { v3, v0, v4 });
            mesh.CreateFace(new Vertex[] { v0, v1, v4 });
            mesh.CreateFace(new Vertex[] { v1, v2, v4 });
            mesh.CreateFace(new Vertex[] { v2, v3, v4 });

            RenderMeshToGl.Render(mesh, new RGBA_Floats(.3, .8, 7));
        }

        public static void Box(this Union rootUnion, double sizeX, double sizeY, double sizeZ)
        {
            rootUnion.Add(new Box(sizeX, sizeY, sizeZ));
        }

        //public 
    }
}
