using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace DynCode
{
    public class CodeHelper
    {
        public static object HelperFunction(string classCode, string mainClass, object[] requiredAssemblies)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(classCode);
            var assemblyName = Path.GetRandomFileName();

            var references = new List<MetadataReference>();

            // Add basic references from currently loaded assemblies
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                    {
                        references.Add(MetadataReference.CreateFromFile(assembly.Location));
                    }
                }
                catch { }
            }

            // Add extra assemblies if provided
            foreach (var extraAsm in requiredAssemblies)
            {
                if (extraAsm is string path && File.Exists(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }

            var compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            using (var ms = new MemoryStream())
            {
                EmitResult result = compilation.Emit(ms);

                if (!result.Success)
                {
                    // Convert Roslyn diagnostics to a format similar to CompilerErrorCollection if needed
                    // For now, let's just return the diagnostics as a collection
                    return result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error).ToList();
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    Assembly assembly = Assembly.Load(ms.ToArray());
                    return assembly.CreateInstance(mainClass);
                }
            }
        }
    }
}