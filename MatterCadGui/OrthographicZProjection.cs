﻿/*
Copyright (c) 2012, Lars Brubaker
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met: 

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer. 
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution. 

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies, 
either expressed or implied, of the FreeBSD Project.
*/



using MatterHackers.Agg;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace MatterHackers.PolygonMesh.Processors
{
    public static class OrthographicZProjection
    {
        public static void DrawTo(Graphics2D graphics2D, Mesh meshToDraw, Vector2 offset, double scale)
        {
            foreach (Face face in meshToDraw.Faces)
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
