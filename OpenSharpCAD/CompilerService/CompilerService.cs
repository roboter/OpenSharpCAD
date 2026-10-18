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
            sb.AppendLine("using MatterHackers.VectorMath; ");
            sb.AppendLine("using MatterHackers.Csg.Operations; ");
            sb.AppendLine("using MatterHackers.Csg.Transform; ");
            sb.AppendLine("using MatterHackers.Csg;");
            sb.AppendLine("using MatterHackers.Csg.Solids; ");
            sb.AppendLine("using MatterHackers.RenderOpenGl; ");
            sb.AppendLine("using MatterHackers.RenderOpenGl.OpenGl; ");
            sb.AppendLine("using MatterHackers.Agg;");

            sb.AppendLine("namespace Test");
            sb.AppendLine("{");
            sb.AppendLine("      public class RenderTest");
            sb.AppendLine("      {");
            sb.AppendLine("            private readonly List<System.ValueTuple<MatterHackers.PolygonMesh.Mesh, Color>> _cachedMeshes = new();");
            sb.AppendLine("            private bool _isBuilt = false;");
            sb.AppendLine("            public string LastError { get; set; }");
            sb.AppendLine();
            sb.AppendLine("            public void Build()");
            sb.AppendLine("            {");
            sb.AppendLine("                _cachedMeshes.Clear();");
            sb.AppendLine("                LastError = null;");
            sb.AppendLine("                try {");
            sb.AppendLine("#line 1 \"script\"");
            sb.AppendLine(scriptSource);
            sb.AppendLine("                } catch (Exception ex) {");
            sb.AppendLine("                    LastError = ex.Message;");
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
