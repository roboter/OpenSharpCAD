using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using MatterHackers.Agg;
using MatterHackers.Csg;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.PolygonMesh;
using MatterHackers.VectorMath;
using Path = MatterHackers.Csg.Solids.Path;
using Circle = MatterHackers.Csg.Solids.Circle;
using Rectangle = MatterHackers.Csg.Solids.Rectangle;

namespace CSharpCAD
{
    /// <summary>
    /// Information model representing an automatically documented CAD type or function.
    /// </summary>
    public class ApiDocItem
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public string FullTypeName { get; set; }
        public string Summary { get; set; }
        public List<string> Constructors { get; set; } = new();
        public List<string> Properties { get; set; } = new();
        public List<string> Methods { get; set; } = new();
        public string ExampleSnippet { get; set; }
        public Func<List<(Mesh mesh, Color color)>> BuildMeshes { get; set; }
    }

    /// <summary>
    /// Automatic Documentation Service that inspects CAD types, shapes, primitives,
    /// transformations, and script global functions using Reflection and XML doc comments.
    /// </summary>
    public static class AutoDocumentationService
    {
        private static readonly Dictionary<string, XDocument> _xmlDocCache = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Retrieves the list of all documented CAD types and functions categorized and enriched with Reflection data.
        /// </summary>
        public static List<ApiDocItem> GetDocumentedItems()
        {
            var list = new List<ApiDocItem>();

            // 1. 3D Solids
            list.Add(DocumentType(typeof(Box), "3D Solid",
                "// Cube of 20x20x20 centered at origin\nvar cube = new Box(20, 20, 20, createCentered: true);\nDraw(cube, RGB(65, 105, 225));",
                () => new List<(Mesh, Color)> { (new Box(20, 20, 20, createCentered: true).ToMesh(), new Color(65, 105, 225)) }));

            list.Add(DocumentType(typeof(Cylinder), "3D Solid",
                "// Cylinder of radius 10 and height 30 centered\nvar cyl = new Cylinder(radius: 10, height: 30, sides: 36);\nDraw(cyl, RGB(46, 204, 113));",
                () => new List<(Mesh, Color)> { (new Cylinder(10, 30, 36).ToMesh(), new Color(46, 204, 113)) }));

            list.Add(DocumentType(typeof(Sphere), "3D Solid",
                "// Sphere of radius 15\nvar ball = new Sphere(radius: 15);\nDraw(ball, RGB(231, 76, 60));",
                () => new List<(Mesh, Color)> { (new Sphere(15).ToMesh(), new Color(231, 76, 60)) }));

            list.Add(DocumentType(typeof(Torus), "3D Solid",
                "// Torus with outer ring radius 20, tube inner radius 5\nvar ring = new Torus(innerRadius: 5, outerRadius: 20);\nDraw(ring, RGB(241, 196, 15));",
                () => new List<(Mesh, Color)> { (new Torus(5, 20).ToMesh(), new Color(241, 196, 15)) }));

            // 2. 2D Primitives
            list.Add(DocumentType(typeof(Circle), "2D Primitive",
                "// Circle of radius 15\nvar c = new Circle(radius: 15);\nDraw(c, RGB(80, 250, 123));",
                () => new List<(Mesh, Color)> { (new Circle(15).ToMesh(), new Color(80, 250, 123)) }));

            list.Add(DocumentType(typeof(Rectangle), "2D Primitive",
                "// Rectangle 30 wide x 16 tall with 3mm rounded corners\nvar rect = new Rectangle(30, 16, cornerRadius: 3, centered: true);\nDraw(rect, RGB(139, 233, 253));",
                () => new List<(Mesh, Color)> { (new Rectangle(30, 16, 3, true).ToMesh(), new Color(139, 233, 253)) }));

            list.Add(DocumentType(typeof(Square), "2D Primitive",
                "// Square with side 24 and rounded corners\nvar sq = new Square(size: 24, cornerRadius: 3, centered: true);\nDraw(sq, RGB(189, 147, 249));",
                () => new List<(Mesh, Color)> { (new Square(24, 3, true).ToMesh(), new Color(189, 147, 249)) }));

            list.Add(DocumentType(typeof(Line), "2D Primitive",
                "// 3D Line between two coordinate points\nvar line = new Line(new Point(0, 0, 0), new Point(25, 20, 15));\nDraw(line, RGB(255, 121, 198));",
                () => new List<(Mesh, Color)> { (new Line(new Point(-15, -15, 0), new Point(15, 15, 20)).ToWireMesh(1.0), new Color(255, 121, 198)) }));

            list.Add(DocumentType(typeof(Point), "2D Primitive",
                "// 1. Create a 3D coordinate point (X, Y, Z)\nvar pt = new Point(10, 15, 20);\n\n// 2. Position a solid at the point coordinates (or call pt.ToSphere(3)):\nvar sphere = new Translate(new Sphere(3), pt);\n\n// 3. Connect the point to origin with a 3D line:\nvar line = pt.LineTo(new Point(0, 0, 0));\n\nDraw(sphere, RGB(255, 85, 85));\nDraw(line, RGB(80, 200, 255));",
                () =>
                {
                    var pt = new Point(10, 15, 20);
                    var sphere = new Translate(new Sphere(3), pt);
                    var line = pt.LineTo(new Point(0, 0, 0));
                    return new List<(Mesh, Color)>
                    {
                        (sphere.ToMesh(), new Color(255, 85, 85)),
                        (line.ToWireMesh(1.0), new Color(80, 200, 255))
                    };
                }));

            list.Add(DocumentType(typeof(Path), "2D Primitive",
                "// Polyline / polygon path formed by chained points\nvar path = new Path();\npath.AddPoint(0, 0);\npath.AddPoint(20, 0);\npath.AddPoint(10, 18);\npath.Close();\nDraw(path, RGB(241, 250, 140));",
                () =>
                {
                    var p = new Path();
                    p.AddPoint(-10, -10);
                    p.AddPoint(10, -10);
                    p.AddPoint(0, 12);
                    p.Close();
                    return new List<(Mesh, Color)> { (p.ToMesh(), new Color(241, 250, 140)) };
                }));

            // 3. Transformations
            list.Add(DocumentType(typeof(Translate), "Transformation",
                "// Translate solid 15 units along X and 10 along Z\nvar b = new Box(16, 16, 16);\nvar shifted = new Translate(b, x: 15, y: 0, z: 10);\nDraw(shifted, RGB(80, 200, 255));",
                () => new List<(Mesh, Color)> { (new Translate(new Box(16, 16, 16, createCentered: true), 10, 0, 5).ToMesh(), new Color(80, 200, 255)) }));

            list.Add(DocumentType(typeof(Rotate), "Transformation",
                "// Rotate solid around axes (in radians)\nvar b = new Box(12, 12, 28, createCentered: true);\nvar tilted = new Rotate(b, x: MathHelper.DegreesToRadians(45), z: MathHelper.DegreesToRadians(30));\nDraw(tilted, RGB(255, 184, 108));",
                () => new List<(Mesh, Color)> { (new Rotate(new Box(12, 12, 28, createCentered: true), x: MathHelper.DegreesToRadians(45), z: MathHelper.DegreesToRadians(30)).ToMesh(), new Color(255, 184, 108)) }));

            list.Add(DocumentType(typeof(Scale), "Transformation",
                "// Non-uniform scaling of a solid along axes\nvar sph = new Sphere(12);\nvar stretched = new Scale(sph, x: 2.0, y: 0.8, z: 1.0);\nDraw(stretched, RGB(189, 147, 249));",
                () => new List<(Mesh, Color)> { (new Scale(new Sphere(12), 1.8, 0.8, 1.0).ToMesh(), new Color(189, 147, 249)) }));

            // 4. CSG Boolean Operations
            list.Add(DocumentType(typeof(Union), "CSG Boolean",
                "// Union joins multiple solids into one contiguous shape (also: a + b)\nvar joined = new Union(\n    new Box(24, 10, 10, createCentered: true),\n    new Box(10, 24, 10, createCentered: true)\n);\nDraw(joined, RGB(80, 250, 123));",
                () =>
                {
                    var u = new Union(
                        new Box(24, 10, 10, createCentered: true),
                        new Box(10, 24, 10, createCentered: true)
                    );
                    return new List<(Mesh, Color)> { (u.ToMesh(), new Color(80, 250, 123)) };
                }));

            list.Add(DocumentType(typeof(Difference), "CSG Boolean",
                "// Difference carves holes out of solids (also: a - b)\nvar box = new Box(22, 22, 22, createCentered: true);\nvar hole = new Cylinder(7, 30);\nvar hollow = new Difference(box, hole);\nDraw(hollow, RGB(255, 85, 85));",
                () =>
                {
                    var diff = new Difference(
                        new Box(22, 22, 22, createCentered: true),
                        new Cylinder(7, 30)
                    );
                    return new List<(Mesh, Color)> { (diff.ToMesh(), new Color(255, 85, 85)) };
                }));

            list.Add(DocumentType(typeof(Intersection), "CSG Boolean",
                "// Intersection keeps only the shared volume of solids (also: a * b)\nvar box = new Box(22, 22, 22, createCentered: true);\nvar sphere = new Sphere(14);\nvar common = new Intersection(box, sphere);\nDraw(common, RGB(241, 250, 140));",
                () =>
                {
                    var inter = new Intersection(
                        new Box(22, 22, 22, createCentered: true),
                        new Sphere(14)
                    );
                    return new List<(Mesh, Color)> { (inter.ToMesh(), new Color(241, 250, 140)) };
                }));

            // 5. Script Global Functions
            list.AddRange(DocumentScriptGlobals());

            return list;
        }

        /// <summary>
        /// Inspects a given type with Reflection and compiles its documentation record.
        /// </summary>
        public static ApiDocItem DocumentType(Type type, string category, string exampleSnippet = null, Func<List<(Mesh, Color)>> meshBuilder = null)
        {
            if (type == null) return null;

            string summary = GetXmlDocSummary(type) ?? $"Representation of {type.Name} in OpenSharpCAD.";

            var item = new ApiDocItem
            {
                Name = type.Name,
                Category = category,
                FullTypeName = type.FullName,
                Summary = summary,
                ExampleSnippet = exampleSnippet ?? $"// Example usage of {type.Name}\nvar obj = new {type.Name}();",
                BuildMeshes = meshBuilder
            };

            // Inspect Constructors
            var ctors = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
            foreach (var ctor in ctors)
            {
                string sig = FormatConstructorSignature(ctor);
                string ctorDoc = GetXmlDocSummary(ctor);
                if (!string.IsNullOrWhiteSpace(ctorDoc))
                {
                    sig += $"  // {ctorDoc}";
                }
                item.Constructors.Add(sig);
            }

            // Inspect Public Properties
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            foreach (var prop in props)
            {
                string propType = FormatTypeName(prop.PropertyType);
                string access = (prop.CanRead ? "get; " : "") + (prop.CanWrite ? "set; " : "");
                string propDoc = GetXmlDocSummary(prop);
                string line = $"{propType} {prop.Name} {{ {access}}}";
                if (!string.IsNullOrWhiteSpace(propDoc))
                {
                    line += $"  // {propDoc}";
                }
                item.Properties.Add(line);
            }

            // Inspect Public Methods (relevant CAD methods, omitting standard Object overrides)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.DeclaringType != typeof(object))
                .ToList();

            foreach (var m in methods)
            {
                string sig = FormatMethodSignature(m);
                string mDoc = GetXmlDocSummary(m);
                if (!string.IsNullOrWhiteSpace(mDoc))
                {
                    sig += $"  // {mDoc}";
                }
                item.Methods.Add(sig);
            }

            return item;
        }

        private static List<ApiDocItem> DocumentScriptGlobals()
        {
            var list = new List<ApiDocItem>();

            list.Add(new ApiDocItem
            {
                Name = "Draw",
                Category = "Script Global",
                Summary = "Renders a 3D solid, 2D primitive, or mesh into the 3D viewport with an optional custom color.",
                Constructors = new List<string>
                {
                    "void Draw(CsgObject objectToProcess)",
                    "void Draw(CsgObject objectToProcess, Color color)",
                    "void Draw(I2DPrimitive primitive2D)",
                    "void Draw(I2DPrimitive primitive2D, Color color)",
                    "void Draw(Mesh mesh)",
                    "void Draw(Mesh mesh, Color color)"
                },
                Properties = new List<string>(),
                Methods = new List<string>(),
                ExampleSnippet = "Draw(new Box(20, 20, 20, createCentered: true), RGB(80, 250, 123));",
                BuildMeshes = () => new List<(Mesh, Color)> { (new Box(20, 20, 20, createCentered: true).ToMesh(), new Color(80, 250, 123)) }
            });

            list.Add(new ApiDocItem
            {
                Name = "Extrude",
                Category = "Script Global",
                Summary = "Extrudes a closed 2D shape (Circle, Rectangle, Path) linearly along an axis into a 3D solid, with optional twist and scale.",
                Constructors = new List<string>
                {
                    "CsgObject Extrude(I2DPrimitive shape, double height, bool centered = false, Alignment alignment = Alignment.z)",
                    "CsgObject Extrude(I2DPrimitive shape, double height, double twistDegrees, int slices = 30, double scale = 1.0, bool centered = false, Alignment alignment = Alignment.z)"
                },
                Properties = new List<string>(),
                Methods = new List<string>(),
                ExampleSnippet = "var c = new Circle(12);\nvar pillar = Extrude(c, height: 35, centered: true);\nDraw(pillar, RGB(46, 204, 113));",
                BuildMeshes = () => new List<(Mesh, Color)> { (new Circle(12).Extrude(35, centered: true).ToMesh(), new Color(46, 204, 113)) }
            });

            list.Add(new ApiDocItem
            {
                Name = "TwistExtrude",
                Category = "Script Global",
                Summary = "Extrudes a closed 2D primitive while twisting around its centroid into a helical spiral 3D solid.",
                Constructors = new List<string>
                {
                    "CsgObject TwistExtrude(I2DPrimitive shape, double height, double twistDegrees = 360, int slices = 30, double scale = 1.0, bool centered = false, Alignment alignment = Alignment.z)"
                },
                Properties = new List<string>(),
                Methods = new List<string>(),
                ExampleSnippet = "var sq = new Square(16, centered: true);\nvar spiral = TwistExtrude(sq, height: 40, twistDegrees: 180, slices: 40);\nDraw(spiral, RGB(255, 121, 198));",
                BuildMeshes = () => new List<(Mesh, Color)> { (new Square(16, centered: true).TwistExtrude(40, twistDegrees: 180, slices: 40).ToMesh(), new Color(255, 121, 198)) }
            });

            list.Add(new ApiDocItem
            {
                Name = "Revolve",
                Category = "Script Global",
                Summary = "Revolves a 2D profile curve around an axis to produce a 3D rotational solid of revolution (e.g. vases, bowls, lathe parts).",
                Constructors = new List<string>
                {
                    "CsgObject Revolve(I2DPrimitive shape, double angleDegrees = 360, int angleSteps = 30, double axisOffset = 0, bool revolveAroundZ = true)"
                },
                Properties = new List<string>(),
                Methods = new List<string>(),
                ExampleSnippet = "var c = new Circle(x: 18, y: 0, radius: 6);\nvar donut = Revolve(c, angleDegrees: 360, angleSteps: 36);\nDraw(donut, RGB(241, 196, 15));",
                BuildMeshes = () => new List<(Mesh, Color)> { (new Circle(18, 0, 6).Revolve(360, 36).ToMesh(), new Color(241, 196, 15)) }
            });

            list.Add(new ApiDocItem
            {
                Name = "Loft",
                Category = "Script Global",
                Summary = "Creates a smooth 3D skin transition between 2D profile sections across height.",
                Constructors = new List<string>
                {
                    "CsgObject Loft(I2DPrimitive bottom, I2DPrimitive top, double height, double twistDegrees = 0, bool centered = false, Alignment alignment = Alignment.z)"
                },
                Properties = new List<string>(),
                Methods = new List<string>(),
                ExampleSnippet = "var bot = new Circle(16);\nvar top = new Square(16, centered: true);\nvar blended = Loft(bot, top, height: 35, twistDegrees: 45);\nDraw(blended, RGB(80, 200, 255));",
                BuildMeshes = () => new List<(Mesh, Color)> { (new Circle(16).Loft(new Square(16, centered: true), 35, twistDegrees: 45).ToMesh(), new Color(80, 200, 255)) }
            });

            list.Add(new ApiDocItem
            {
                Name = "RGB / RGBA / RGBf",
                Category = "Script Global",
                Summary = "Color construction helpers for styling solids and meshes in the 3D viewport.",
                Constructors = new List<string>
                {
                    "Color RGB(int r, int g, int b)            // 0 - 255",
                    "Color RGBA(int r, int g, int b, int a)     // 0 - 255",
                    "Color RGBf(double r, double g, double b)   // 0.0 - 1.0"
                },
                Properties = new List<string>(),
                Methods = new List<string>(),
                ExampleSnippet = "var col1 = RGB(255, 100, 50);\nvar col2 = RGBA(80, 250, 123, 200);\nvar col3 = RGBf(0.2, 0.8, 1.0);",
                BuildMeshes = () => new List<(Mesh, Color)> { (new Sphere(12).ToMesh(), new Color(255, 100, 50)) }
            });

            return list;
        }

        /// <summary>
        /// Generates a comprehensive GitHub Markdown documentation string covering all CAD types.
        /// </summary>
        public static string GenerateMarkdown()
        {
            var sb = new StringBuilder();
            sb.AppendLine("# OpenSharpCAD API Documentation");
            sb.AppendLine();
            sb.AppendLine("Comprehensive programmatic reference for all 3D Shapes, 2D Primitives, Transformations, CSG Boolean Operations, and Script Globals.");
            sb.AppendLine();
            sb.AppendLine("> Generated automatically by OpenSharpCAD `AutoDocumentationService`.");
            sb.AppendLine();

            var items = GetDocumentedItems();
            var categories = items.GroupBy(i => i.Category);

            // Table of Contents
            sb.AppendLine("## Table of Contents");
            foreach (var group in categories)
            {
                sb.AppendLine($"- **{group.Key}**");
                foreach (var item in group)
                {
                    string anchor = item.Name.ToLowerInvariant().Replace(" ", "-").Replace("/", "").Replace("(", "").Replace(")", "");
                    sb.AppendLine($"  - [{item.Name}](#{anchor})");
                }
            }
            sb.AppendLine();

            // Sections
            foreach (var group in categories)
            {
                sb.AppendLine($"---");
                sb.AppendLine($"## {group.Key}");
                sb.AppendLine();

                foreach (var item in group)
                {
                    sb.AppendLine($"### {item.Name}");
                    sb.AppendLine();
                    sb.AppendLine(item.Summary);
                    sb.AppendLine();

                    if (item.Constructors.Count > 0)
                    {
                        sb.AppendLine("#### Constructors / Signatures");
                        sb.AppendLine("```csharp");
                        foreach (var ctor in item.Constructors)
                        {
                            sb.AppendLine(ctor);
                        }
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }

                    if (item.Properties.Count > 0)
                    {
                        sb.AppendLine("#### Properties");
                        sb.AppendLine("```csharp");
                        foreach (var prop in item.Properties)
                        {
                            sb.AppendLine(prop);
                        }
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }

                    if (item.Methods.Count > 0)
                    {
                        sb.AppendLine("#### Methods");
                        sb.AppendLine("```csharp");
                        foreach (var m in item.Methods)
                        {
                            sb.AppendLine(m);
                        }
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }

                    if (!string.IsNullOrWhiteSpace(item.ExampleSnippet))
                    {
                        sb.AppendLine("#### Example Code");
                        sb.AppendLine("```csharp");
                        sb.AppendLine(item.ExampleSnippet);
                        sb.AppendLine("```");
                        sb.AppendLine();
                    }
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Writes the generated Markdown API documentation to the specified file path.
        /// </summary>
        public static void ExportMarkdownFile(string filePath)
        {
            string md = GenerateMarkdown();
            string dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, md, Encoding.UTF8);
        }

        /// <summary>
        /// Generates a comprehensive HTML API Reference page with dark theme styling,
        /// interactive sidebar navigation, live search filtering, and copyable code snippets.
        /// </summary>
        public static string GenerateHtml()
        {
            var items = GetDocumentedItems();
            var categories = items.GroupBy(i => i.Category).ToList();

            var sb = new StringBuilder();
            sb.AppendLine("<!DOCTYPE html>");
            sb.AppendLine("<html lang=\"en\">");
            sb.AppendLine("<head>");
            sb.AppendLine("  <meta charset=\"UTF-8\">");
            sb.AppendLine("  <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
            sb.AppendLine("  <title>OpenSharpCAD — API Reference</title>");
            sb.AppendLine("  <meta name=\"description\" content=\"Comprehensive API Reference documentation for OpenSharpCAD: 3D Solids, 2D Primitives, Transformations, CSG Booleans, and Script Globals.\">");
            sb.AppendLine("  <link rel=\"icon\" href=\"data:image/svg+xml,<svg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 100 100'><text y='.9em' font-size='90'>💎</text></svg>\">");
            sb.AppendLine("  <link rel=\"preconnect\" href=\"https://fonts.googleapis.com\">");
            sb.AppendLine("  <link rel=\"preconnect\" href=\"https://fonts.gstatic.com\" crossorigin>");
            sb.AppendLine("  <link href=\"https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&family=JetBrains+Mono:wght@400;500;600;700&display=swap\" rel=\"stylesheet\">");
            sb.AppendLine("  <link rel=\"stylesheet\" href=\"styles.css\">");
            sb.AppendLine("</head>");
            sb.AppendLine("<body>");

            // Top Header
            sb.AppendLine("  <header class=\"app-header\">");
            sb.AppendLine("    <div class=\"header-top\">");
            sb.AppendLine("      <a href=\"index.html\" class=\"brand\">");
            sb.AppendLine("        <div class=\"brand-logo\">#</div>");
            sb.AppendLine("        <span class=\"brand-title\">OpenSharpCAD</span>");
            sb.AppendLine("        <span class=\"brand-badge\">API Reference</span>");
            sb.AppendLine("      </a>");
            sb.AppendLine("      <div class=\"header-actions\">");
            sb.AppendLine("        <a href=\"index.html\" class=\"nav-link\">");
            sb.AppendLine("          <span>🎮 Interactive 3D Examples</span>");
            sb.AppendLine("        </a>");
            sb.AppendLine("        <a href=\"api.html\" class=\"nav-link\" style=\"color: var(--accent-cyan); font-weight: 700;\">");
            sb.AppendLine("          <span>📖 API Reference</span>");
            sb.AppendLine("        </a>");
            sb.AppendLine("        <a href=\"https://github.com/roboter/OpenSharpCAD\" class=\"nav-link\" target=\"_blank\" rel=\"noopener\">");
            sb.AppendLine("          <span>🐙 GitHub</span>");
            sb.AppendLine("        </a>");
            sb.AppendLine("      </div>");
            sb.AppendLine("    </div>");
            sb.AppendLine("  </header>");

            // Hero section
            sb.AppendLine("  <section class=\"hero-section\" style=\"padding: 2rem 1.5rem 1rem;\">");
            sb.AppendLine("    <h1 class=\"hero-title\">OpenSharpCAD API Reference</h1>");
            sb.AppendLine("    <p class=\"hero-subtitle\">Complete programmatic reference for all 3D Solids, 2D Primitives, Transformations, CSG Boolean Operations, and Script Globals.</p>");
            sb.AppendLine("  </section>");

            // Two-column layout: Sidebar + Main Content
            sb.AppendLine("  <div class=\"api-layout\">");

            // Sidebar
            sb.AppendLine("    <aside class=\"api-sidebar\">");
            sb.AppendLine("      <div class=\"api-sidebar-search\">");
            sb.AppendLine("        <input type=\"text\" id=\"api-filter-input\" class=\"api-sidebar-input\" placeholder=\"Filter API classes...\">");
            sb.AppendLine("      </div>");
            sb.AppendLine("      <nav id=\"api-nav\">");
            foreach (var group in categories)
            {
                sb.AppendLine("        <div class=\"api-nav-group\">");
                sb.AppendLine($"          <div class=\"api-nav-group-title\">{EscapeHtml(group.Key)}</div>");
                foreach (var item in group)
                {
                    string anchor = GetHtmlAnchor(item.Name);
                    sb.AppendLine($"          <a href=\"#{anchor}\" class=\"api-nav-link\" data-name=\"{EscapeHtml(item.Name.ToLowerInvariant())}\">");
                    sb.AppendLine($"            <span>{EscapeHtml(item.Name)}</span>");
                    sb.AppendLine("          </a>");
                }
                sb.AppendLine("        </div>");
            }
            sb.AppendLine("      </nav>");
            sb.AppendLine("    </aside>");

            // Main Content Column
            sb.AppendLine("    <main class=\"api-main-content\" id=\"api-main-content\">");
            foreach (var item in items)
            {
                string anchor = GetHtmlAnchor(item.Name);
                string badgeColor = item.Category switch
                {
                    "3D Solid" => "var(--accent-cyan)",
                    "2D Primitive" => "var(--accent-green)",
                    "Transformation" => "var(--accent-purple)",
                    "CSG Boolean" => "var(--accent-pink)",
                    "Script Global" => "var(--accent-yellow)",
                    _ => "var(--accent-cyan)"
                };

                sb.AppendLine($"      <article class=\"api-entry-card\" id=\"{anchor}\" data-name=\"{EscapeHtml(item.Name.ToLowerInvariant())}\">");
                sb.AppendLine("        <header class=\"api-entry-header\">");
                sb.AppendLine("          <div class=\"api-entry-title-row\">");
                sb.AppendLine($"            <h2 class=\"api-entry-title\">{EscapeHtml(item.Name)}</h2>");
                sb.AppendLine($"            <span class=\"card-badge\" style=\"background: rgba(139, 233, 253, 0.15); color: {badgeColor}; border: 1px solid rgba(139, 233, 253, 0.3);\">{EscapeHtml(item.Category)}</span>");
                if (!string.IsNullOrEmpty(item.FullTypeName))
                {
                    sb.AppendLine($"            <span class=\"api-entry-namespace\">{EscapeHtml(item.FullTypeName)}</span>");
                }
                sb.AppendLine("          </div>");
                sb.AppendLine($"          <p class=\"api-entry-summary\">{EscapeHtml(item.Summary)}</p>");
                sb.AppendLine("        </header>");

                sb.AppendLine("        <div class=\"api-entry-body\">");

                // Constructors
                if (item.Constructors.Count > 0)
                {
                    sb.AppendLine("          <section>");
                    sb.AppendLine("            <h3 class=\"api-section-heading\">Constructors & Signatures</h3>");
                    sb.AppendLine("            <div class=\"api-code-block\">");
                    foreach (var ctor in item.Constructors)
                    {
                        sb.AppendLine($"              <div class=\"api-code-item\">{EscapeHtml(ctor)}</div>");
                    }
                    sb.AppendLine("            </div>");
                    sb.AppendLine("          </section>");
                }

                // Properties
                if (item.Properties.Count > 0)
                {
                    sb.AppendLine("          <section>");
                    sb.AppendLine("            <h3 class=\"api-section-heading\">Properties</h3>");
                    sb.AppendLine("            <div class=\"api-code-block\">");
                    foreach (var prop in item.Properties)
                    {
                        sb.AppendLine($"              <div class=\"api-code-item\">{EscapeHtml(prop)}</div>");
                    }
                    sb.AppendLine("            </div>");
                    sb.AppendLine("          </section>");
                }

                // Methods
                if (item.Methods.Count > 0)
                {
                    sb.AppendLine("          <section>");
                    sb.AppendLine("            <h3 class=\"api-section-heading\">Methods</h3>");
                    sb.AppendLine("            <div class=\"api-code-block\">");
                    foreach (var m in item.Methods)
                    {
                        sb.AppendLine($"              <div class=\"api-code-item\">{EscapeHtml(m)}</div>");
                    }
                    sb.AppendLine("            </div>");
                    sb.AppendLine("          </section>");
                }

                // Example
                if (!string.IsNullOrWhiteSpace(item.ExampleSnippet))
                {
                    sb.AppendLine("          <section>");
                    sb.AppendLine("            <div style=\"display: flex; justify-content: space-between; align-items: center; margin-bottom: 0.5rem;\">");
                    sb.AppendLine("              <h3 class=\"api-section-heading\" style=\"margin-bottom: 0;\">Example Usage</h3>");
                    sb.AppendLine($"              <button class=\"copy-btn\" data-code=\"{System.Uri.EscapeDataString(item.ExampleSnippet)}\">");
                    sb.AppendLine("                <span>Copy</span>");
                    sb.AppendLine("              </button>");
                    sb.AppendLine("            </div>");
                    sb.AppendLine("            <div class=\"api-code-block\" style=\"color: var(--accent-yellow);\">");
                    sb.AppendLine($"              <pre><code>{EscapeHtml(item.ExampleSnippet)}</code></pre>");
                    sb.AppendLine("            </div>");
                    sb.AppendLine("          </section>");
                }

                sb.AppendLine("        </div>");
                sb.AppendLine("      </article>");
            }
            sb.AppendLine("    </main>");
            sb.AppendLine("  </div>");

            // Footer
            sb.AppendLine("  <footer class=\"app-footer\">");
            sb.AppendLine("    <div class=\"footer-links\">");
            sb.AppendLine("      <a href=\"index.html\">Interactive 3D Examples</a>");
            sb.AppendLine("      <a href=\"https://github.com/roboter/OpenSharpCAD\">Repository</a>");
            sb.AppendLine("      <a href=\"#top\">Back to top ↑</a>");
            sb.AppendLine("    </div>");
            sb.AppendLine("    <p>© OpenSharpCAD. Built with Three.js and C# CAD engine.</p>");
            sb.AppendLine("  </footer>");

            // Client script for filter and copy
            sb.AppendLine("  <script>");
            sb.AppendLine("    const filterInput = document.getElementById('api-filter-input');");
            sb.AppendLine("    const navLinks = document.querySelectorAll('.api-nav-link');");
            sb.AppendLine("    const cards = document.querySelectorAll('.api-entry-card');");
            sb.AppendLine("    if (filterInput) {");
            sb.AppendLine("      filterInput.addEventListener('input', (e) => {");
            sb.AppendLine("        const q = e.target.value.toLowerCase().trim();");
            sb.AppendLine("        cards.forEach(c => {");
            sb.AppendLine("          const match = !q || c.dataset.name.includes(q) || c.textContent.toLowerCase().includes(q);");
            sb.AppendLine("          c.style.display = match ? 'flex' : 'none';");
            sb.AppendLine("        });");
            sb.AppendLine("        navLinks.forEach(l => {");
            sb.AppendLine("          const match = !q || l.dataset.name.includes(q);");
            sb.AppendLine("          l.style.display = match ? 'flex' : 'none';");
            sb.AppendLine("        });");
            sb.AppendLine("      });");
            sb.AppendLine("    }");
            sb.AppendLine("    document.querySelectorAll('.copy-btn').forEach(btn => {");
            sb.AppendLine("      btn.addEventListener('click', async () => {");
            sb.AppendLine("        const code = decodeURIComponent(btn.dataset.code);");
            sb.AppendLine("        try {");
            sb.AppendLine("          await navigator.clipboard.writeText(code);");
            sb.AppendLine("          const prev = btn.innerHTML;");
            sb.AppendLine("          btn.innerHTML = '<span>Copied!</span>';");
            sb.AppendLine("          setTimeout(() => btn.innerHTML = prev, 1500);");
            sb.AppendLine("        } catch (err) {}");
            sb.AppendLine("      });");
            sb.AppendLine("    });");
            sb.AppendLine("  </script>");

            sb.AppendLine("</body>");
            sb.AppendLine("</html>");

            return sb.ToString();
        }

        /// <summary>
        /// Writes the generated HTML API Reference documentation to the specified file path.
        /// </summary>
        public static void ExportHtmlFile(string filePath)
        {
            string html = GenerateHtml();
            string dir = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(filePath, html, Encoding.UTF8);
        }

        private static string GetHtmlAnchor(string name)
        {
            return (name ?? "").ToLowerInvariant().Replace(" ", "-").Replace("/", "").Replace("(", "").Replace(")", "").Replace("&", "");
        }

        private static string EscapeHtml(string str)
        {
            if (string.IsNullOrEmpty(str)) return "";
            return str.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }

        #region Reflection & XML Formatting Helpers

        private static string FormatConstructorSignature(ConstructorInfo ctor)
        {
            var pars = ctor.GetParameters();
            string parList = string.Join(", ", pars.Select(FormatParameter));
            return $"new {ctor.DeclaringType?.Name}({parList})";
        }

        private static string FormatMethodSignature(MethodInfo m)
        {
            string ret = FormatTypeName(m.ReturnType);
            var pars = m.GetParameters();
            string parList = string.Join(", ", pars.Select(FormatParameter));
            return $"{ret} {m.Name}({parList})";
        }

        private static string FormatParameter(ParameterInfo p)
        {
            string typeName = FormatTypeName(p.ParameterType);
            string paramStr = $"{typeName} {p.Name}";
            if (p.HasDefaultValue)
            {
                string defVal = p.DefaultValue == null ? "null" :
                    p.DefaultValue is bool b ? (b ? "true" : "false") :
                    p.DefaultValue is string s ? $"\"{s}\"" :
                    p.DefaultValue.ToString();
                paramStr += $" = {defVal}";
            }
            return paramStr;
        }

        private static string FormatTypeName(Type t)
        {
            if (t == typeof(void)) return "void";
            if (t == typeof(int)) return "int";
            if (t == typeof(double)) return "double";
            if (t == typeof(float)) return "float";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(string)) return "string";

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                return $"{FormatTypeName(t.GetGenericArguments()[0])}?";
            }

            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
            {
                return $"List<{FormatTypeName(t.GetGenericArguments()[0])}>";
            }

            if (t.IsArray)
            {
                return $"{FormatTypeName(t.GetElementType())}[]";
            }

            return t.Name;
        }

        private static string GetXmlDocSummary(MemberInfo member)
        {
            if (member == null) return null;

            var xml = GetXmlDocForAssembly(member.DeclaringType?.Assembly ?? (member as Type)?.Assembly);
            if (xml == null) return null;

            string memberId = GetXmlMemberId(member);
            if (string.IsNullOrEmpty(memberId)) return null;

            var memberElem = xml.Descendants("member").FirstOrDefault(e => (string)e.Attribute("name") == memberId);
            if (memberElem != null)
            {
                var summary = memberElem.Element("summary");
                if (summary != null)
                {
                    return CleanDocText(summary.Value);
                }
            }

            return null;
        }

        private static string GetXmlMemberId(MemberInfo member)
        {
            if (member is Type t)
            {
                return $"T:{t.FullName}";
            }
            if (member is ConstructorInfo ctor)
            {
                var pars = ctor.GetParameters();
                string parStr = pars.Length > 0 ? $"({string.Join(",", pars.Select(p => p.ParameterType.FullName))})" : "";
                return $"M:{ctor.DeclaringType?.FullName}.#ctor{parStr}";
            }
            if (member is MethodInfo m)
            {
                var pars = m.GetParameters();
                string parStr = pars.Length > 0 ? $"({string.Join(",", pars.Select(p => p.ParameterType.FullName))})" : "";
                return $"M:{m.DeclaringType?.FullName}.{m.Name}{parStr}";
            }
            if (member is PropertyInfo p)
            {
                return $"P:{p.DeclaringType?.FullName}.{p.Name}";
            }
            return null;
        }

        private static XDocument GetXmlDocForAssembly(Assembly asm)
        {
            if (asm == null || string.IsNullOrEmpty(asm.Location)) return null;

            if (_xmlDocCache.TryGetValue(asm.Location, out var cached))
            {
                return cached;
            }

            string xmlPath = System.IO.Path.ChangeExtension(asm.Location, ".xml");
            if (File.Exists(xmlPath))
            {
                try
                {
                    var doc = XDocument.Load(xmlPath);
                    _xmlDocCache[asm.Location] = doc;
                    return doc;
                }
                catch { }
            }

            _xmlDocCache[asm.Location] = null;
            return null;
        }

        private static string CleanDocText(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "";
            var lines = raw.Split('\n')
                .Select(l => l.Trim())
                .Where(l => l.Length > 0);
            return string.Join(" ", lines);
        }

        #endregion
    }
}
