using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;

namespace DynCode
{
    public class CodeHelper
    {
        /// <summary>
        /// Security whitelist of assembly names permitted to be referenced during script compilation.
        /// Unwhitelisted assemblies (such as System.Diagnostics.Process, System.Net.Http, System.Security.Cryptography)
        /// are strictly omitted from Roslyn metadata references so untrusted scripts cannot compile malicious calls.
        /// </summary>
        public static readonly HashSet<string> AllowedAssemblyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            // Core .NET primitive runtime & types
            "System.Private.CoreLib",
            "System.Runtime",
            "netstandard",
            "mscorlib",
            "System.Collections",
            "System.Collections.Concurrent",
            "System.Collections.NonGeneric",
            "System.Collections.Specialized",
            "System.Linq",
            "System.Linq.Expressions",
            "System.Linq.Queryable",
            "System.Memory",
            "System.Numerics.Vectors",
            "System.ObjectModel",
            "System.ComponentModel",
            "System.ComponentModel.Primitives",
            "System.Console",
            "System.Threading",
            "System.Threading.Tasks",

            // OpenSharpCAD / MatterHackers CAD & geometry assemblies
            "VectorMath",
            "MatterHackers.VectorMath",
            "Csg",
            "MatterHackers.Csg",
            "Agg",
            "MatterHackers.Agg",
            "PolygonMesh",
            "MatterHackers.PolygonMesh",
            "RenderOpenGl",
            "MatterHackers.RenderOpenGl",
            "DataConverters2D",
            "MatterHackers.DataConverters2D",
            "Tesselate",
            "MatterHackers.Tesselate",
            "Localizations",
            "MatterHackers.Localizations",
            "agg_clipper_library",
            "clipper_library",
            "geometry3Sharp",
            "Triangle",
            "MatterHackers.Agg.ImageProcessing",
            "Typography.One",
            "GLFW.NETStandard",
            "MatterHackers.GlfwProvider"
        };

        public static bool IsAllowedAssembly(string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName)) return false;
            return AllowedAssemblyNames.Contains(assemblyName);
        }

        private static bool IsForbiddenNamespaceOrType(string identifier)
        {
            if (string.IsNullOrWhiteSpace(identifier)) return false;

            // Blocked namespaces / root prefixes
            if (identifier.StartsWith("System.IO", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("System.Diagnostics", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("System.Net", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("System.Reflection", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("System.Runtime.InteropServices", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("Microsoft.Win32", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Block direct static calls on dangerous System types without namespace prefix
            if (identifier.StartsWith("File.", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("Directory.", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("Process.", StringComparison.OrdinalIgnoreCase) ||
                identifier.StartsWith("Environment.Exit", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private static List<Diagnostic> ValidateSyntaxSecurity(SyntaxTree syntaxTree)
        {
            var diagnostics = new List<Diagnostic>();
            var root = syntaxTree.GetRoot();

            // Check for unsafe code
            foreach (var token in root.DescendantTokens())
            {
                if (token.IsKind(SyntaxKind.UnsafeKeyword))
                {
                    diagnostics.Add(CreateSecurityDiagnostic("OSCAD001", "Unsafe code is strictly disallowed in CAD scripts.", token.GetLocation()));
                }
            }

            // Check for forbidden using directives
            foreach (var u in root.DescendantNodes().OfType<UsingDirectiveSyntax>())
            {
                string usingName = u.Name?.ToString() ?? "";
                if (IsForbiddenNamespaceOrType(usingName))
                {
                    diagnostics.Add(CreateSecurityDiagnostic("OSCAD002", $"Using directive '{usingName}' is not permitted in CAD scripts.", u.GetLocation()));
                }
            }

            // Check for member access / qualified names accessing forbidden namespaces
            foreach (var node in root.DescendantNodes())
            {
                if (node is MemberAccessExpressionSyntax memberAccess)
                {
                    string fullExpr = memberAccess.ToString();
                    if (IsForbiddenNamespaceOrType(fullExpr))
                    {
                        diagnostics.Add(CreateSecurityDiagnostic("OSCAD003", $"Access to '{fullExpr}' is blocked by security policy.", memberAccess.GetLocation()));
                    }
                }
                else if (node is QualifiedNameSyntax qualName)
                {
                    string fullExpr = qualName.ToString();
                    if (IsForbiddenNamespaceOrType(fullExpr))
                    {
                        diagnostics.Add(CreateSecurityDiagnostic("OSCAD003", $"Access to '{fullExpr}' is blocked by security policy.", qualName.GetLocation()));
                    }
                }
            }

            return diagnostics;
        }

        private static Diagnostic CreateSecurityDiagnostic(string id, string message, Location location)
        {
            var descriptor = new DiagnosticDescriptor(
                id,
                "Security Policy Violation",
                message,
                "Security",
                DiagnosticSeverity.Error,
                isEnabledByDefault: true);
            return Diagnostic.Create(descriptor, location);
        }

        public static object HelperFunction(string classCode, string mainClass, object[] requiredAssemblies)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(classCode);

            // Pre-compilation AST security check
            var securityErrors = ValidateSyntaxSecurity(syntaxTree);
            if (securityErrors.Count > 0)
            {
                return securityErrors;
            }

            var assemblyName = Path.GetRandomFileName();

            var references = new List<MetadataReference>();
            var addedLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Add basic references from currently loaded assemblies, filtered strictly by whitelist
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                try
                {
                    if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location))
                    {
                        var name = assembly.GetName().Name;
                        if (name != null && IsAllowedAssembly(name) && addedLocations.Add(assembly.Location))
                        {
                            references.Add(MetadataReference.CreateFromFile(assembly.Location));
                        }
                    }
                }
                catch { }
            }

            // Add extra assemblies if provided and whitelisted
            if (requiredAssemblies != null)
            {
                foreach (var extraAsm in requiredAssemblies)
                {
                    if (extraAsm is string path && File.Exists(path))
                    {
                        try
                        {
                            var asmName = AssemblyName.GetAssemblyName(path).Name;
                            if (asmName != null && IsAllowedAssembly(asmName) && addedLocations.Add(path))
                            {
                                references.Add(MetadataReference.CreateFromFile(path));
                            }
                        }
                        catch { }
                    }
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