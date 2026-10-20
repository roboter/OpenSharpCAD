using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Completion;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Text;

namespace CSharpCAD
{
    public class RoslynCompletionItem
    {
        public string DisplayText { get; set; }
        public string InsertionText { get; set; }
        public string Kind { get; set; } // Method, Property, Class, Keyword, Variable, etc.
        public string InlineDescription { get; set; }
        public CompletionItem RawItem { get; set; }
    }

    public class RoslynCompletionEngine
    {
        private readonly AdhocWorkspace _workspace;
        private readonly ProjectId _projectId;
        private readonly DocumentId _documentId;
        private readonly List<MetadataReference> _references;

        // Prefix and suffix to wrap the user's script in the same context as CompilerService
        public const string ScriptPrefix =
@"using System;
using System.Collections.Generic;
using MatterHackers.VectorMath;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.RenderOpenGl;
using MatterHackers.RenderOpenGl.OpenGl;
using MatterHackers.Agg;
using Circle = MatterHackers.Csg.Solids.Circle;
using Path = MatterHackers.Csg.Solids.Path;

namespace Test
{
    public class RenderTest
    {
        /// <summary>Renders a 3D CSG solid object into the 3D viewport with default color.</summary>
        public void Draw(CsgObject objectToProcess) { }
        /// <summary>Renders a 3D CSG solid object into the 3D viewport with a custom color.</summary>
        public void Draw(CsgObject objectToProcess, Color color) { }
        /// <summary>Renders a 2D primitive shape (Point, Line, Circle, Rectangle, Path) directly into the 3D viewport.</summary>
        public void Draw(I2DPrimitive primitive2D) { }
        /// <summary>Renders a 2D primitive shape with custom color into the 3D viewport.</summary>
        public void Draw(I2DPrimitive primitive2D, Color color) { }
        /// <summary>Renders a raw 3D polygon mesh into the 3D viewport.</summary>
        public void Draw(MatterHackers.PolygonMesh.Mesh mesh) { }
        /// <summary>Renders a raw 3D polygon mesh with custom color into the 3D viewport.</summary>
        public void Draw(MatterHackers.PolygonMesh.Mesh mesh, Color color) { }

        /// <summary>Extrudes a closed 2D primitive shape linearly along an axis into a 3D solid.</summary>
        /// <param name=""shape"">Closed 2D primitive (Circle, Rectangle, Path, Polygon).</param>
        /// <param name=""height"">Extrusion height/length.</param>
        /// <param name=""centered"">Whether to center the extruded solid along the extrusion axis.</param>
        /// <param name=""alignment"">Direction axis of extrusion (default: Alignment.z).</param>
        public CsgObject Extrude(I2DPrimitive shape, double height, bool centered = false, Alignment alignment = Alignment.z) => default;

        /// <summary>Extrudes a 2D primitive linearly with twist rotation and progressive scaling along the extrusion height.</summary>
        public CsgObject Extrude(I2DPrimitive shape, double height, double twistDegrees, int slices = 30, double scale = 1.0, bool centered = false, Alignment alignment = Alignment.z) => default;

        /// <summary>Twists and extrudes a 2D shape along height by a specified total rotation in degrees.</summary>
        public CsgObject TwistExtrude(I2DPrimitive shape, double height, double twistDegrees = 360, int slices = 30, double scale = 1.0, bool centered = false, Alignment alignment = Alignment.z) => default;

        /// <summary>Revolves a 2D profile curve around an axis to produce a 3D rotational solid of revolution.</summary>
        public CsgObject Revolve(I2DPrimitive shape, double angleDegrees = 360, int angleSteps = 30, double axisOffset = 0, bool revolveAroundZ = true) => default;

        /// <summary>Creates a smooth 3D loft transition between a bottom 2D shape and a top 2D shape over a specified height.</summary>
        public CsgObject Loft(I2DPrimitive bottom, I2DPrimitive top, double height, double twistDegrees = 0, bool centered = false, Alignment alignment = Alignment.z) => default;

        /// <summary>Lofts multiple cross-sections arranged along the Z axis into a continuous 3D solid.</summary>
        public CsgObject Loft(params LoftSection[] sections) => default;

        /// <summary>Lofts multiple cross-sections arranged along the Z axis with overall twist rotation.</summary>
        public CsgObject Loft(double twistDegrees, params LoftSection[] sections) => default;

        /// <summary>Creates a Color from integer RGB values (0 - 255).</summary>
        public Color RGB(int r, int g, int b) => default;

        /// <summary>Creates a Color from integer RGBA values (0 - 255).</summary>
        public Color RGBA(int r, int g, int b, int a) => default;

        /// <summary>Creates a Color from floating-point RGB values (0.0 - 1.0).</summary>
        public Color RGBf(double r, double g, double b) => default;

        public void Render()
        {
";

        public const string ScriptSuffix =
@"
        }
    }
}
";

        public RoslynCompletionEngine()
        {
            var featuresAssembly = typeof(CompletionService).Assembly;
            var csharpFeaturesAssembly = Assembly.Load("Microsoft.CodeAnalysis.CSharp.Features");

            var assemblies = MefHostServices.DefaultAssemblies
                .Add(featuresAssembly)
                .Add(csharpFeaturesAssembly);

            var host = MefHostServices.Create(assemblies);
            _workspace = new AdhocWorkspace(host);

            _references = BuildMetadataReferences();

            _projectId = ProjectId.CreateNewId("CSharpCADScriptProject");
            _documentId = DocumentId.CreateNewId(_projectId, "Script.cs");

            var projectInfo = ProjectInfo.Create(
                _projectId,
                VersionStamp.Create(),
                "CSharpCADScriptProject",
                "CSharpCADScriptProject",
                LanguageNames.CSharp,
                compilationOptions: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .WithMetadataReferences(_references);

            _workspace.AddProject(projectInfo);
            _workspace.AddDocument(DocumentInfo.Create(_documentId, "Script.cs"));
        }

        private List<MetadataReference> BuildMetadataReferences()
        {
            var refs = new List<MetadataReference>();
            var seenLocations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void AddAssembly(Assembly asm)
            {
                if (asm == null || asm.IsDynamic || string.IsNullOrWhiteSpace(asm.Location))
                    return;

                if (seenLocations.Add(asm.Location) && File.Exists(asm.Location))
                {
                    try
                    {
                        refs.Add(MetadataReference.CreateFromFile(asm.Location));
                    }
                    catch
                    {
                        // Ignore unreadable assemblies
                    }
                }
            }

            // Core domain assemblies
            AddAssembly(typeof(object).Assembly); // System.Private.CoreLib
            AddAssembly(typeof(Console).Assembly);
            AddAssembly(typeof(List<>).Assembly);
            AddAssembly(typeof(Enumerable).Assembly);
            AddAssembly(typeof(MatterHackers.Csg.CsgObject).Assembly);
            AddAssembly(typeof(MatterHackers.Csg.Solids.Box).Assembly);
            AddAssembly(typeof(MatterHackers.Csg.Transform.Translate).Assembly);
            AddAssembly(typeof(MatterHackers.VectorMath.Vector3).Assembly);
            AddAssembly(typeof(MatterHackers.Agg.Graphics2D).Assembly);
            AddAssembly(typeof(MatterHackers.RenderOpenGl.GLHelper).Assembly);
            AddAssembly(typeof(MatterHackers.PolygonMesh.Mesh).Assembly);

            // Also load any referenced assembly in CurrentDomain (filtered by security whitelist)
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.IsDynamic || string.IsNullOrEmpty(asm.Location)) continue;
                var name = asm.GetName().Name;
                if (name != null && DynCode.CodeHelper.IsAllowedAssembly(name))
                {
                    AddAssembly(asm);
                }
            }

            return refs;
        }

        public async Task<List<RoslynCompletionItem>> GetCompletionsAsync(
            string userScript, 
            int cursorPosition, 
            CancellationToken cancellationToken = default)
        {
            userScript ??= string.Empty;
            cursorPosition = Math.Clamp(cursorPosition, 0, userScript.Length);

            string fullCode = ScriptPrefix + userScript + ScriptSuffix;
            int wrappedCaret = ScriptPrefix.Length + cursorPosition;

            var sourceText = SourceText.From(fullCode);
            var doc = _workspace.CurrentSolution.GetDocument(_documentId);
            if (doc == null) return new List<RoslynCompletionItem>();

            var updatedDoc = doc.WithText(sourceText);
            _workspace.TryApplyChanges(updatedDoc.Project.Solution);

            var completionService = CompletionService.GetService(updatedDoc);
            if (completionService == null) return new List<RoslynCompletionItem>();

            var completions = await completionService.GetCompletionsAsync(
                updatedDoc,
                wrappedCaret,
                cancellationToken: cancellationToken).ConfigureAwait(false);

            if (completions == null || completions.ItemsList.Count == 0)
                return new List<RoslynCompletionItem>();

            var results = new List<RoslynCompletionItem>();
            foreach (var item in completions.ItemsList)
            {
                string kind = GetItemKind(item);
                results.Add(new RoslynCompletionItem
                {
                    DisplayText = item.DisplayText,
                    InsertionText = item.FilterText ?? item.DisplayText,
                    Kind = kind,
                    InlineDescription = item.InlineDescription,
                    RawItem = item
                });
            }

            return results;
        }

        public async Task<string> GetDescriptionAsync(RoslynCompletionItem item, CancellationToken cancellationToken = default)
        {
            if (item?.RawItem == null) return null;
            var doc = _workspace.CurrentSolution.GetDocument(_documentId);
            if (doc == null) return null;

            var service = CompletionService.GetService(doc);
            if (service == null) return null;

            var desc = await service.GetDescriptionAsync(doc, item.RawItem, cancellationToken).ConfigureAwait(false);
            return desc?.Text;
        }

        private static readonly string[] KnownTypeNames = new[]
        {
            "Cylinder", "Box", "Sphere", "Torus", "Cone", "Polyhedron",
            "Circle", "Ring", "Path", "Arc", "Text", "Triangle",
            "Vector3", "Vector2", "Vector4", "Color", "Line",
            "Translate", "Rotate", "Scale", "Union", "Difference", "Intersection",
            "Mesh", "MeshViewerWidget"
        };

        private static readonly string[] KnownFunctionNames = new[]
        {
            "Draw", "Extrude", "TwistExtrude", "Revolve", "Loft", "RGB", "RGBA", "RGBf"
        };

        public static int LevenshteinDistance(string s, string t)
        {
            if (string.IsNullOrEmpty(s)) return t?.Length ?? 0;
            if (string.IsNullOrEmpty(t)) return s.Length;

            int n = s.Length;
            int m = t.Length;
            int[,] d = new int[n + 1, m + 1];

            for (int i = 0; i <= n; d[i, 0] = i++) ;
            for (int j = 0; j <= m; d[0, j] = j++) ;

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = char.ToLowerInvariant(s[i - 1]) == char.ToLowerInvariant(t[j - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }

        public string ResolveTargetName(string name, bool isConstructor)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;

            var pool = isConstructor ? KnownTypeNames : KnownTypeNames.Concat(KnownFunctionNames).ToArray();

            // 1. Exact case-insensitive match
            foreach (var candidate in pool)
            {
                if (string.Equals(candidate, name, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            // 2. Prefix match
            foreach (var candidate in pool)
            {
                if (candidate.StartsWith(name, StringComparison.OrdinalIgnoreCase))
                    return candidate;
            }

            // 3. Fuzzy match (distance <= 2)
            string best = null;
            int bestDist = 3;
            foreach (var candidate in pool)
            {
                int dist = LevenshteinDistance(name, candidate);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = candidate;
                }
            }

            return best ?? name;
        }

        public async Task<List<RoslynCompletionItem>> GetSignaturesAsync(
            string targetName,
            bool isConstructor,
            int argIndex,
            string userScript,
            int cursorPosition,
            CancellationToken cancellationToken = default)
        {
            string resolvedName = ResolveTargetName(targetName, isConstructor);
            var results = new List<RoslynCompletionItem>();

            Type targetType = null;
            if (isConstructor || KnownTypeNames.Contains(resolvedName, StringComparer.OrdinalIgnoreCase))
            {
                targetType = FindTypeInAssemblies(resolvedName);
            }

            if (targetType != null)
            {
                var ctors = targetType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
                foreach (var ctor in ctors)
                {
                    var parameters = ctor.GetParameters();
                    string sig = $"{targetType.Name}({string.Join(", ", parameters.Select(FormatParameter))})";
                    string insert = FormatInsertion(parameters);
                    string desc = FormatArgDesc(parameters, argIndex);

                    results.Add(new RoslynCompletionItem
                    {
                        DisplayText = sig,
                        InsertionText = insert,
                        Kind = "constructor",
                        InlineDescription = desc
                    });
                }
            }

            if (results.Count == 0)
            {
                var methods = FindMethodsInAssemblies(resolvedName);
                foreach (var method in methods)
                {
                    var parameters = method.GetParameters();
                    string sig = $"{method.Name}({string.Join(", ", parameters.Select(FormatParameter))})";
                    string insert = FormatInsertion(parameters);
                    string desc = FormatArgDesc(parameters, argIndex);

                    results.Add(new RoslynCompletionItem
                    {
                        DisplayText = sig,
                        InsertionText = insert,
                        Kind = "method",
                        InlineDescription = desc
                    });
                }
            }

            if (results.Count == 0)
            {
                try
                {
                    var doc = _workspace.CurrentSolution.GetDocument(_documentId);
                    if (doc != null)
                    {
                        string fullCode = ScriptPrefix + (userScript ?? "") + ScriptSuffix;
                        var updatedDoc = doc.WithText(SourceText.From(fullCode));
                        _workspace.TryApplyChanges(updatedDoc.Project.Solution);

                        var compilation = await updatedDoc.Project.GetCompilationAsync(cancellationToken).ConfigureAwait(false);
                        if (compilation != null)
                        {
                            var symbols = compilation.GetSymbolsWithName(resolvedName, SymbolFilter.All, cancellationToken);
                            foreach (var sym in symbols)
                            {
                                if (sym is INamedTypeSymbol nts)
                                {
                                    foreach (var ctor in nts.InstanceConstructors.Where(c => c.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public))
                                    {
                                        string sig = $"{nts.Name}({string.Join(", ", ctor.Parameters.Select(FormatRoslynParameter))})";
                                        string insert = FormatRoslynInsertion(ctor.Parameters);
                                        string desc = FormatRoslynArgDesc(ctor.Parameters, argIndex);

                                        results.Add(new RoslynCompletionItem
                                        {
                                            DisplayText = sig,
                                            InsertionText = insert,
                                            Kind = "constructor",
                                            InlineDescription = desc
                                        });
                                    }
                                }
                                else if (sym is IMethodSymbol ms)
                                {
                                    string sig = $"{ms.Name}({string.Join(", ", ms.Parameters.Select(FormatRoslynParameter))})";
                                    string insert = FormatRoslynInsertion(ms.Parameters);
                                    string desc = FormatRoslynArgDesc(ms.Parameters, argIndex);

                                    results.Add(new RoslynCompletionItem
                                    {
                                        DisplayText = sig,
                                        InsertionText = insert,
                                        Kind = "method",
                                        InlineDescription = desc
                                    });
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Ignore Roslyn errors
                }
            }

            return results;
        }

        private static Type FindTypeInAssemblies(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return null;

            var prioritizedAssemblies = new[]
            {
                typeof(MatterHackers.Csg.Solids.Cylinder).Assembly,
                typeof(MatterHackers.VectorMath.Vector3).Assembly,
                typeof(MatterHackers.Agg.Color).Assembly,
                typeof(MatterHackers.Csg.CsgObject).Assembly
            };

            foreach (var asm in prioritizedAssemblies)
            {
                try
                {
                    var match = asm.GetTypes().FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
                catch { }
            }

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    if (asm.IsDynamic) continue;
                    var name = asm.GetName().Name;
                    if (name == null || !DynCode.CodeHelper.IsAllowedAssembly(name)) continue;
                    var match = asm.GetTypes().FirstOrDefault(t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase));
                    if (match != null) return match;
                }
                catch { }
            }

            return null;
        }

        private static List<MethodInfo> FindMethodsInAssemblies(string methodName)
        {
            var list = new List<MethodInfo>();
            if (string.IsNullOrEmpty(methodName)) return list;

            var candidates = new[]
            {
                typeof(MatterHackers.Csg.CsgObject),
                typeof(MatterHackers.Csg.Solids.Circle),
                typeof(MatterHackers.Csg.Solids.TwistExtrude),
                typeof(MatterHackers.Csg.Solids.RotateExtrude),
                typeof(MatterHackers.Csg.Solids.LinearExtrude),
                typeof(MatterHackers.Csg.Solids.Loft)
            };

            foreach (var type in candidates)
            {
                try
                {
                    var matches = type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                        .Where(m => string.Equals(m.Name, methodName, StringComparison.OrdinalIgnoreCase));
                    list.AddRange(matches);
                }
                catch { }
            }

            return list;
        }

        private static string FormatParameter(ParameterInfo p)
        {
            string typeName = GetFriendlyTypeName(p.ParameterType);
            string name = p.Name ?? "arg";
            if (p.HasDefaultValue)
            {
                string defaultStr = FormatDefaultValue(p.DefaultValue, p.ParameterType);
                return $"{typeName} {name} = {defaultStr}";
            }
            if (p.GetCustomAttributes(typeof(ParamArrayAttribute), false).Length > 0)
            {
                return $"params {typeName} {name}";
            }
            return $"{typeName} {name}";
        }

        private static string FormatRoslynParameter(IParameterSymbol p)
        {
            string typeName = p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            string name = p.Name;
            if (p.HasExplicitDefaultValue)
            {
                string defaultStr = FormatRoslynDefaultValue(p.ExplicitDefaultValue, p.Type);
                return $"{typeName} {name} = {defaultStr}";
            }
            if (p.IsParams)
            {
                return $"params {typeName} {name}";
            }
            return $"{typeName} {name}";
        }

        private static string FormatInsertion(ParameterInfo[] parameters)
        {
            var required = parameters.Where(p => !p.HasDefaultValue).Select(p => p.Name).ToList();
            if (required.Count == 0 && parameters.Length > 0)
            {
                required = parameters.Take(2).Select(p => p.Name).ToList();
            }
            return (required.Count > 0 ? string.Join(", ", required) : "") + ")";
        }

        private static string FormatRoslynInsertion(ImmutableArray<IParameterSymbol> parameters)
        {
            var required = parameters.Where(p => !p.HasExplicitDefaultValue).Select(p => p.Name).ToList();
            if (required.Count == 0 && parameters.Length > 0)
            {
                required = parameters.Take(2).Select(p => p.Name).ToList();
            }
            return (required.Count > 0 ? string.Join(", ", required) : "") + ")";
        }

        private static string FormatArgDesc(ParameterInfo[] parameters, int argIndex)
        {
            if (argIndex >= 0 && argIndex < parameters.Length)
            {
                var p = parameters[argIndex];
                return $"param {argIndex + 1}: {GetFriendlyTypeName(p.ParameterType)} {p.Name}";
            }
            return $"{parameters.Length} parameter{(parameters.Length == 1 ? "" : "s")}";
        }

        private static string FormatRoslynArgDesc(ImmutableArray<IParameterSymbol> parameters, int argIndex)
        {
            if (argIndex >= 0 && argIndex < parameters.Length)
            {
                var p = parameters[argIndex];
                return $"param {argIndex + 1}: {p.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)} {p.Name}";
            }
            return $"{parameters.Length} parameter{(parameters.Length == 1 ? "" : "s")}";
        }

        private static string GetFriendlyTypeName(Type type)
        {
            if (type == typeof(double)) return "double";
            if (type == typeof(int)) return "int";
            if (type == typeof(float)) return "float";
            if (type == typeof(bool)) return "bool";
            if (type == typeof(string)) return "string";
            if (type == typeof(void)) return "void";
            if (type == typeof(object)) return "object";
            if (type.IsArray) return GetFriendlyTypeName(type.GetElementType()) + "[]";
            return type.Name;
        }

        private static string FormatDefaultValue(object value, Type type)
        {
            if (value == null) return "null";
            if (value is string s) return $"\"{s}\"";
            if (value is bool b) return b ? "true" : "false";
            if (type.IsEnum) return type.Name + "." + value;
            if (value is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
            return value.ToString();
        }

        private static string FormatRoslynDefaultValue(object value, ITypeSymbol type)
        {
            if (value == null) return "null";
            if (value is string s) return $"\"{s}\"";
            if (value is bool b) return b ? "true" : "false";
            if (type.TypeKind == TypeKind.Enum) return type.Name + "." + value;
            if (value is double d) return d.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (value is float f) return f.ToString(System.Globalization.CultureInfo.InvariantCulture) + "f";
            return value.ToString();
        }

        private static string GetItemKind(CompletionItem item)
        {
            var tags = item.Tags;
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Class)) return "class";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Structure)) return "struct";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Interface)) return "interface";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Enum)) return "enum";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Method) || tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.ExtensionMethod)) return "method";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Property)) return "property";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Field)) return "field";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Local)) return "local";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Parameter)) return "parameter";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Keyword)) return "keyword";
            if (tags.Contains(Microsoft.CodeAnalysis.Tags.WellKnownTags.Constant)) return "const";
            return "symbol";
        }
    }
}
