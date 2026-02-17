using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace CSharpCAD
{
    public class CompilerService : ICompilerService
    {
        public object Compile(string scriptSource, out List<string> errors)
        {
            errors = [];
            StringBuilder sb = new();

            //-----------------
            // Create the class code wrapper
            sb.AppendLine("using System;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using MatterHackers.VectorMath; ");
            sb.AppendLine("using MatterHackers.Csg.Operations; ");
            sb.AppendLine("using MatterHackers.Csg;");
            sb.AppendLine("using MatterHackers.Csg.Solids; ");
            sb.AppendLine("using MatterHackers.RenderOpenGl; ");
            sb.AppendLine("using MatterHackers.RenderOpenGl.OpenGl; ");
            sb.AppendLine("using MatterHackers.Agg;");

            sb.AppendLine("namespace Test");
            sb.AppendLine("{");
            sb.AppendLine("      public class RenderTest");
            sb.AppendLine("      {");
            sb.AppendLine("            public void Render()");
            sb.AppendLine("            {");
            sb.AppendLine("                try {");
            sb.AppendLine(scriptSource);
            sb.AppendLine("                } catch (Exception ex) { System.Console.WriteLine(\"Error in Render(): \" + ex.Message); }");
            sb.AppendLine("            }");

            sb.AppendLine("            public void Draw(CsgObject objectToProcess)");
            sb.AppendLine("            {");
            sb.AppendLine("                if (objectToProcess == null) { System.Console.WriteLine(\"Draw: objectToProcess is null\"); return; }");
            sb.AppendLine("                var mesh = MatterHackers.RenderOpenGl.CsgToMesh.Convert(objectToProcess);");
            sb.AppendLine("                if (mesh == null) { System.Console.WriteLine(\"Draw: mesh is null\"); return; }");
            sb.AppendLine("                if (GL.Instance == null) { System.Console.WriteLine(\"Draw: GL.Instance is NULL!\"); }");
            sb.AppendLine("                GLHelper.Render(mesh, new Color(150, 150, 150, 255));");
            sb.AppendLine("            }");
            sb.AppendLine("      }");
            sb.AppendLine("}");

            string classCode = sb.ToString();

            try
            {
                // Pass the class code, the namespace of the class and the list of extra assemblies needed
                var classRef = DynCode.CodeHelper.HelperFunction(classCode, "Test.RenderTest", new object[]
                {
                    typeof(System.Console).Assembly.Location,
                    typeof(MatterHackers.Csg.CsgObject).Assembly.Location,
                    typeof(MatterHackers.VectorMath.Vector3).Assembly.Location,
                    typeof(MatterHackers.Agg.Graphics2D).Assembly.Location,
                    typeof(MatterHackers.RenderOpenGl.GLHelper).Assembly.Location,
                    typeof(MatterHackers.PolygonMesh.Mesh).Assembly.Location
                });

                if (classRef is List<Diagnostic> diagnostics)
                {
                    foreach (var error in diagnostics)
                    {
                        errors.Add(error.ToString());
                    }
                    return null;
                }

                return classRef;
            }
            catch (Exception ex)
            {
                errors.Add(ex.Message);
                return null;
            }
        }
    }
}
