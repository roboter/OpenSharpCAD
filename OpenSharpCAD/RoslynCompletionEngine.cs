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
        public void Draw(CsgObject objectToProcess) { }
        public void Draw(CsgObject objectToProcess, Color color) { }
        public void Draw(I2DPrimitive primitive2D) { }
        public void Draw(I2DPrimitive primitive2D, Color color) { }
        public void Draw(MatterHackers.PolygonMesh.Mesh mesh) { }
        public void Draw(MatterHackers.PolygonMesh.Mesh mesh, Color color) { }
        public CsgObject Extrude(I2DPrimitive shape, double height, bool centered = false, Alignment alignment = Alignment.z) => default;
        public CsgObject Revolve(I2DPrimitive shape, double angleDegrees = 360, int angleSteps = 30, double axisOffset = 0, bool revolveAroundZ = true) => default;
        public Color RGB(int r, int g, int b) => default;
        public Color RGBA(int r, int g, int b, int a) => default;
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

            // Also load any referenced assembly in CurrentDomain
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                AddAssembly(asm);
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
