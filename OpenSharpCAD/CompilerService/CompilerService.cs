using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace CSharpCAD
{
    public class CompilerService : ICompilerService
    {
        public object Compile(string scriptSource, out List<Diagnostic> errors)
        {
            errors = [];
            StringBuilder sb = new();

            //-----------------
            // Create the class code wrapper
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using System.Linq;");
            sb.AppendLine("using MatterHackers.VectorMath; ");
            sb.AppendLine("using MatterHackers.Csg.Operations; ");
            sb.AppendLine("using MatterHackers.Csg.Transform; ");
            sb.AppendLine("using MatterHackers.Csg;");
            sb.AppendLine("using MatterHackers.Csg.Solids; ");
            sb.AppendLine("using MatterHackers.RenderOpenGl; ");
            sb.AppendLine("using MatterHackers.RenderOpenGl.OpenGl; ");
            sb.AppendLine("using MatterHackers.Agg;");
            sb.AppendLine("using Circle = MatterHackers.Csg.Solids.Circle;");
            sb.AppendLine("using Path = MatterHackers.Csg.Solids.Path;");
            sb.AppendLine("using Point = MatterHackers.Csg.Solids.Point;");
            sb.AppendLine("using Line = MatterHackers.Csg.Solids.Line;");
            sb.AppendLine("using Point2D = MatterHackers.Csg.Solids.Point2D;");
            sb.AppendLine("using Point3D = MatterHackers.Csg.Solids.Point3D;");
            sb.AppendLine("using Line2D = MatterHackers.Csg.Solids.Line2D;");

            sb.AppendLine("namespace Test");
            sb.AppendLine("{");
            sb.AppendLine("      public class RenderTest");
            sb.AppendLine("      {");
            sb.AppendLine("            private readonly List<System.ValueTuple<MatterHackers.PolygonMesh.Mesh, Color>> _cachedMeshes = new();");
            sb.AppendLine("            private bool _isBuilt = false;");
            sb.AppendLine("            public string LastError { get; set; }");
            sb.AppendLine("            public int LastErrorLine { get; set; } = -1;");
            sb.AppendLine();
            sb.AppendLine("            public void Build()");
            sb.AppendLine("            {");
            sb.AppendLine("                _cachedMeshes.Clear();");
            sb.AppendLine("                LastError = null;");
            sb.AppendLine("                LastErrorLine = -1;");
            sb.AppendLine("                try {");
            sb.AppendLine("#line 1 \"script\"");
            sb.AppendLine(scriptSource);
            sb.AppendLine("                } catch (Exception ex) {");
            sb.AppendLine("                    LastError = ex.Message;");
            sb.AppendLine("                    try {");
            sb.AppendLine("                        string st = ex.StackTrace ?? \"\";");
            sb.AppendLine("                        int idx = st.IndexOf(\"script:line \");");
            sb.AppendLine("                        if (idx < 0) idx = st.IndexOf(\":line \");");
            sb.AppendLine("                        if (idx >= 0) {");
            sb.AppendLine("                            int start = st.IndexOfAny(new[] { '0','1','2','3','4','5','6','7','8','9' }, idx);");
            sb.AppendLine("                            if (start >= 0) {");
            sb.AppendLine("                                int end = start;");
            sb.AppendLine("                                while (end < st.Length && char.IsDigit(st[end])) end++;");
            sb.AppendLine("                                if (int.TryParse(st.Substring(start, end - start), out int l)) { LastErrorLine = l - 1; }");
            sb.AppendLine("                            }");
            sb.AppendLine("                        }");
            sb.AppendLine("                    } catch {}");
            sb.AppendLine("                    System.Console.WriteLine(\"Error in script execution: \" + ex.Message);");
            sb.AppendLine("                }");
            sb.AppendLine("                _isBuilt = true;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Render()");
            sb.AppendLine("            {");
            sb.AppendLine("                if (!_isBuilt) { Build(); }");
            sb.AppendLine("                for (int i = 0; i < _cachedMeshes.Count; i++)");
            sb.AppendLine("                {");
            sb.AppendLine("                    var item = _cachedMeshes[i];");
            sb.AppendLine("                    if (item.Item1 != null)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        GLHelper.Render(item.Item1, item.Item2);");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public MatterHackers.VectorMath.AxisAlignedBoundingBox GetBounds()");
            sb.AppendLine("            {");
            sb.AppendLine("                if (!_isBuilt) { Build(); }");
            sb.AppendLine("                MatterHackers.VectorMath.AxisAlignedBoundingBox total = null;");
            sb.AppendLine("                for (int i = 0; i < _cachedMeshes.Count; i++)");
            sb.AppendLine("                {");
            sb.AppendLine("                    var m = _cachedMeshes[i].Item1;");
            sb.AppendLine("                    if (m != null)");
            sb.AppendLine("                    {");
            sb.AppendLine("                        var b = m.GetAxisAlignedBoundingBox();");
            sb.AppendLine("                        if (b != null && b.MinXYZ.X <= b.MaxXYZ.X)");
            sb.AppendLine("                        {");
            sb.AppendLine("                            if (total == null) total = new MatterHackers.VectorMath.AxisAlignedBoundingBox(b.MinXYZ, b.MaxXYZ);");
            sb.AppendLine("                            else total.ExpandToInclude(b);");
            sb.AppendLine("                        }");
            sb.AppendLine("                    }");
            sb.AppendLine("                }");
            sb.AppendLine("                return total;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Draw(CsgObject objectToProcess)");
            sb.AppendLine("            {");
            sb.AppendLine("                Draw(objectToProcess, new Color(150, 150, 150, 255));");
            sb.AppendLine("            }");
            sb.AppendLine();
            // Draw with explicit color
            sb.AppendLine("            public void Draw(CsgObject objectToProcess, Color color)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (objectToProcess == null) return;");
            sb.AppendLine("                var mesh = MatterHackers.RenderOpenGl.CsgToMesh.Convert(objectToProcess);");
            sb.AppendLine("                if (mesh != null) { _cachedMeshes.Add(new System.ValueTuple<MatterHackers.PolygonMesh.Mesh, Color>(mesh, color)); }");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Draw(I2DPrimitive primitive2D)");
            sb.AppendLine("            {");
            sb.AppendLine("                Draw(primitive2D, new Color(150, 150, 150, 255));");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Draw(I2DPrimitive primitive2D, Color color)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (primitive2D == null) return;");
            sb.AppendLine("                var mesh = primitive2D.IsClosed ? primitive2D.ToMesh() : primitive2D.ToWireMesh(0.5);");
            sb.AppendLine("                if (mesh != null) { _cachedMeshes.Add(new System.ValueTuple<MatterHackers.PolygonMesh.Mesh, Color>(mesh, color)); }");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Draw(MatterHackers.PolygonMesh.Mesh mesh)");
            sb.AppendLine("            {");
            sb.AppendLine("                Draw(mesh, new Color(150, 150, 150, 255));");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Draw(MatterHackers.PolygonMesh.Mesh mesh, Color color)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (mesh != null) { _cachedMeshes.Add(new System.ValueTuple<MatterHackers.PolygonMesh.Mesh, Color>(mesh, color)); }");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            // Draw collections of CSG objects (LINQ friendly)");
            sb.AppendLine("            public void Draw(System.Collections.Generic.IEnumerable<CsgObject> objects)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (objects == null) return;");
            sb.AppendLine("                foreach (var obj in objects) Draw(obj);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Draw(System.Collections.Generic.IEnumerable<CsgObject> objects, Color color)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (objects == null) return;");
            sb.AppendLine("                foreach (var obj in objects) Draw(obj, color);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            // Draw collections of 2D/3D primitives (LINQ friendly)");
            sb.AppendLine("            public void Draw(System.Collections.Generic.IEnumerable<I2DPrimitive> primitives)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (primitives == null) return;");
            sb.AppendLine("                foreach (var p in primitives) Draw(p);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            public void Draw(System.Collections.Generic.IEnumerable<I2DPrimitive> primitives, Color color)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (primitives == null) return;");
            sb.AppendLine("                foreach (var p in primitives) Draw(p, color);");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            // Standalone 2D Extrude/Revolve/Loft/TwistExtrude functions");
            sb.AppendLine("            public CsgObject Extrude(I2DPrimitive shape, double height, bool centered = false, Alignment alignment = Alignment.z) => shape?.Extrude(height, centered, alignment);");
            sb.AppendLine("            public CsgObject Extrude(I2DPrimitive shape, double height, double twistDegrees, int slices = 30, double scale = 1.0, bool centered = false, Alignment alignment = Alignment.z) => shape?.Extrude(height, twistDegrees, slices, scale, centered, alignment);");
            sb.AppendLine("            public CsgObject TwistExtrude(I2DPrimitive shape, double height, double twistDegrees = 360, int slices = 30, double scale = 1.0, bool centered = false, Alignment alignment = Alignment.z) => shape?.TwistExtrude(height, twistDegrees, slices, scale, centered, alignment);");
            sb.AppendLine("            public CsgObject Revolve(I2DPrimitive shape, double angleDegrees = 360, int angleSteps = 30, double axisOffset = 0, bool revolveAroundZ = true) => shape?.Revolve(angleDegrees, angleSteps, axisOffset, revolveAroundZ);");
            sb.AppendLine("            public CsgObject Loft(I2DPrimitive bottom, I2DPrimitive top, double height, double twistDegrees = 0, bool centered = false, Alignment alignment = Alignment.z) => new Loft(bottom, top, height, twistDegrees: twistDegrees, centered: centered, alignment: alignment);");
            sb.AppendLine("            public CsgObject Loft(params LoftSection[] sections) => new Loft(sections);");
            sb.AppendLine("            public CsgObject Loft(double twistDegrees, params LoftSection[] sections) => new Loft(twistDegrees, sections);");
            sb.AppendLine("            public CsgObject CylinderBetween(Vector3 start, Vector3 end, double radius, int sides = 16) => Cylinder.CreateBetween(start, end, radius, sides);");
            sb.AppendLine("            public CsgObject CylinderBetween(Point start, Point end, double radius, int sides = 16) => Cylinder.CreateBetween(start, end, radius, sides);");
            sb.AppendLine("            public CsgObject CylinderBetween(Line line, double radius, int sides = 16) => Cylinder.CreateBetween(line, radius, sides);");

            // Convenience color constructors available in scripts
            sb.AppendLine("            // RGB(0-255, 0-255, 0-255)");
            sb.AppendLine("            public Color RGB(int r, int g, int b) => new Color(r, g, b, 255);");
            sb.AppendLine("            // RGBA(0-255, 0-255, 0-255, 0-255)");
            sb.AppendLine("            public Color RGBA(int r, int g, int b, int a) => new Color(r, g, b, a);");
            sb.AppendLine("            // RGB using 0.0-1.0 floats");
            sb.AppendLine("            public Color RGBf(double r, double g, double b) => new Color(r, g, b);");
            sb.AppendLine("      }");
            sb.AppendLine("}");

            string classCode = sb.ToString();

            try
            {
                // Pass the class code, the namespace of the class and the list of extra assemblies needed
                var classRef = DynCode.CodeHelper.HelperFunction(classCode, "Test.RenderTest", new object[]
                {
                    typeof(System.Console).Assembly.Location,
                    typeof(MatterHackers.Csg.Transform.Translate).Assembly.Location,
                    typeof(MatterHackers.Csg.CsgObject).Assembly.Location,
                    typeof(MatterHackers.VectorMath.Vector3).Assembly.Location,
                    typeof(MatterHackers.Agg.Graphics2D).Assembly.Location,
                    typeof(MatterHackers.RenderOpenGl.GLHelper).Assembly.Location,
                    typeof(MatterHackers.PolygonMesh.Mesh).Assembly.Location
                });

                if (classRef is List<Diagnostic> diagnostics)
                {
                    errors = diagnostics;
                    return null;
                }

                return classRef;
            }
            catch (Exception ex)
            {
                // Create a basic diagnostic if something else fails
                Console.WriteLine(ex.Message);
                return null;
            }
        }
    }
}
