using MatterHackers.Agg;
using MatterHackers.VectorMath;
using MatterHackers.PolygonMesh;
using MatterHackers.Agg.VertexSource;

namespace OpenCSharpCad
{
    public static class OrthographicZProjection
    {
        public static void DrawTo(Graphics2D graphics2D, MatterHackers.PolygonMesh.Mesh meshToDraw, Vector2 offset, double scale)
        {
            foreach (MatterHackers.PolygonMesh.Face face in meshToDraw.Faces)
            {
                PathStorage polygonProjected = new PathStorage();
                bool first = true;
                foreach (FaceEdge faceEdge in face.FaceEdges())
                {
                    Vector2 position = new Vector2(faceEdge.firstVertex.Position.x, faceEdge.firstVertex.Position.y);
                    position += offset;
                    position *= scale;
                    if (first)
                    {
                        polygonProjected.MoveTo(position.x, position.y);
                        first = false;
                    }
                    else
                    {
                        polygonProjected.LineTo(position.x, position.y);
                    }
                }
                graphics2D.Render(polygonProjected, RGBA_Bytes.Blue);
            }
        }
    }
}
