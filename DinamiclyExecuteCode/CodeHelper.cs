using Microsoft.CSharp;
using System.CodeDom.Compiler;
using System.Collections.Generic;

namespace DynCode
{
    public class CodeHelper
    {
        public static object HelperFunction(string classCode, string mainClass, object[] requiredAssemblies)
        {
            CSharpCodeProvider provider = new CSharpCodeProvider(new Dictionary<string, string> { { "CompilerVersion", "v4.0" } });

            CompilerParameters parameters = new CompilerParameters
            {
                GenerateExecutable = false,       // Create a dll
                GenerateInMemory = true,          // Create it in memory
                WarningLevel = 3,                 // Default warning level
                CompilerOptions = "/optimize",    // Optimize code
                TreatWarningsAsErrors = false     // Better be false to avoid break in warnings
            };

            //----------------
            // Add basic referenced assemblies
            parameters.ReferencedAssemblies.Add("system.dll");
            parameters.ReferencedAssemblies.Add("System.Windows.Forms.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("AGG.dll");

            parameters.ReferencedAssemblies.Add("MatterHackers.Agg.UI.dll");
            parameters.ReferencedAssemblies.Add("MatterHackers.PolygonMesh.dll");
            parameters.ReferencedAssemblies.Add("MatterHackers.VectorMath.dll");
            parameters.ReferencedAssemblies.Add("MatterHackers.Csg.dll");
            parameters.ReferencedAssemblies.Add("MatterHackers.RenderOpenGl.dll");
            //using MatterHackers.PolygonMesh;
            //using MatterHackers.VectorMath;
            //using MatterHackers.Csg.Operations;
            //using MatterHackers.Csg.Solids;
            //using MatterHackers.RenderOpenGl;
            //using MatterHackers.Agg;

            //----------------
            // Add all extra assemblies required
            foreach (var extraAsm in requiredAssemblies)
            {
                parameters.ReferencedAssemblies.Add(extraAsm as string);
            }

            //--------------------
            // Try to compile the code received
            CompilerResults results = provider.CompileAssemblyFromSource(parameters, classCode);

            //--------------------
            // If the compilation returned error, then return the CompilerErrorCollection class with the errors to the caller
            if (results.Errors.Count != 0)
            {
                return results.Errors;
            }

            //--------------------
            // Return the created class instance to caller
            return results.CompiledAssembly.CreateInstance(mainClass);
        }
    }
}