import * as THREE from 'three';
import { OrbitControls } from 'three/addons/controls/OrbitControls.js';

// ============================================================================
// C# Syntax Highlighter
// ============================================================================

const CSHARP_KEYWORDS = new Set([
  'abstract', 'as', 'base', 'bool', 'break', 'byte', 'case', 'catch', 'char',
  'checked', 'class', 'const', 'continue', 'decimal', 'default', 'delegate',
  'do', 'double', 'else', 'enum', 'event', 'explicit', 'extern', 'false',
  'finally', 'fixed', 'float', 'for', 'foreach', 'goto', 'if', 'implicit',
  'in', 'int', 'interface', 'internal', 'is', 'lock', 'long', 'namespace',
  'new', 'null', 'object', 'operator', 'out', 'override', 'params', 'private',
  'protected', 'public', 'readonly', 'ref', 'return', 'sbyte', 'sealed',
  'short', 'sizeof', 'stackalloc', 'static', 'string', 'struct', 'switch',
  'this', 'throw', 'true', 'try', 'typeof', 'uint', 'ulong', 'unchecked',
  'unsafe', 'ushort', 'using', 'virtual', 'void', 'volatile', 'while', 'var'
]);

const CSHARP_TYPES = new Set([
  'Box', 'Cylinder', 'Sphere', 'Torus', 'Circle', 'Rectangle', 'Square',
  'Line', 'Point', 'Dot', 'Path', 'Polygon', 'Union', 'Difference', 'Intersection',
  'Translate', 'Rotate', 'Scale', 'Align', 'Mesh', 'Color', 'ColorF',
  'Vector2', 'Vector3', 'Vector4', 'Matrix4X4', 'MathHelper', 'CsgObject',
  'I2DPrimitive', 'Alignment', 'Face', 'Edge', 'List', 'IEnumerable', 'ValueTuple'
]);

const CSHARP_PROPERTIES = new Set([
  'Width', 'Depth', 'Height', 'Size', 'Radius', 'RadiusMajor', 'RadiusMinor',
  'TopCenter', 'BottomCenter', 'Corners', 'BoundingEdges', 'UniqueVertices',
  'Center', 'Length', 'Points', 'Start', 'End', 'X', 'Y', 'Z', 'Faces', 'Edges', 'Vertices'
]);

const CSHARP_CAD_METHODS = new Set([
  'Draw', 'RGB', 'RGBA', 'RGBf', 'Extrude', 'TwistExtrude', 'Revolve', 'Loft',
  'CylinderBetween', 'GetCorners', 'GetBoundingEdges', 'GetUniqueVertices',
  'GetEdges', 'ToMesh', 'ToWireMesh', 'ToCircle', 'ToSphere', 'ToPath',
  'LineTo', 'DistanceTo', 'AddPoint', 'Close', 'DegreesToRadians', 'RadiansToDegrees',
  'Select', 'Where', 'SelectMany', 'Distinct', 'OrderBy', 'ToArray', 'ToList'
]);

function highlightCSharp(code) {
  const lines = code.split('\n');
  return lines.map(line => {
    // If entire line is a comment
    const trimmed = line.trimStart();
    if (trimmed.startsWith('//')) {
      const indent = line.substring(0, line.indexOf('/'));
      return escapeHtml(indent) + `<span class="token-comment">${escapeHtml(trimmed)}</span>`;
    }

    // Tokenize line with regex capturing strings, comments, numbers, words, operators
    const tokenRegex = /(\/\/.*$)|("(?:\\.|[^"\\])*")|(\b\d+(?:\.\d+)?(?:f|d|m)?\b)|(\b[a-zA-Z_]\w*\b)|([+\-*/%=<>!&|^~]+)|([^\s\w]+)/g;

    return line.replace(tokenRegex, (match, comment, str, num, word, op, punct) => {
      if (comment) return `<span class="token-comment">${escapeHtml(comment)}</span>`;
      if (str) return `<span class="token-string">${escapeHtml(str)}</span>`;
      if (num) return `<span class="token-number">${escapeHtml(num)}</span>`;
      if (word) {
        if (CSHARP_KEYWORDS.has(word)) return `<span class="token-keyword">${escapeHtml(word)}</span>`;
        if (CSHARP_TYPES.has(word)) return `<span class="token-type">${escapeHtml(word)}</span>`;
        if (CSHARP_PROPERTIES.has(word)) return `<span class="token-property">${escapeHtml(word)}</span>`;
        if (CSHARP_CAD_METHODS.has(word)) return `<span class="token-method">${escapeHtml(word)}</span>`;
        return `<span class="token-variable">${escapeHtml(word)}</span>`;
      }
      if (op) return `<span class="token-operator">${escapeHtml(op)}</span>`;
      return escapeHtml(match);
    });
  }).join('\n');
}

function escapeHtml(str) {
  return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

// ============================================================================
// Examples Collection (Matches HelpWindow & OpenSharpCAD reference)
// ============================================================================

const EXAMPLES = [
  // --------------------------------------------------------------------------
  // 3D Shapes
  // --------------------------------------------------------------------------
  {
    id: 'box-cube',
    title: 'Box (Cube & Rectangular Solid)',
    category: '3D Shapes',
    badge: 'Solid Primitive',
    badgeColor: '#8be9fd',
    description: `Creates a rectangular solid or cube centered at origin.
Parameters: width (X), depth (Y), height (Z), createCentered = true
Exposed Properties: Width, Depth, Height, Size, TopCenter, BottomCenter, Corners, BoundingEdges
Methods: GetCorners(), GetBoundingEdges(), GetUniqueVertices(), ToMesh()`,
    code: `// Box(width, depth, height)
var box = new Box(24, 18, 12);

// Access dimensions and anchor points
Vector3 top = box.TopCenter;
Vector3 dim = box.Size;

Draw(box, RGB(80, 180, 255));`,
    buildScene(scene) {
      const geo = new THREE.BoxGeometry(24, 12, 18); // Three.js Y is up, Z is depth
      const mat = new THREE.MeshStandardMaterial({
        color: 0x50b4ff,
        metalness: 0.2,
        roughness: 0.35
      });
      const mesh = new THREE.Mesh(geo, mat);
      scene.add(mesh);

      const edges = new THREE.EdgesGeometry(geo);
      const line = new THREE.LineSegments(edges, new THREE.LineBasicMaterial({ color: 0xffffff, transparent: true, opacity: 0.3 }));
      scene.add(line);
    }
  },

  {
    id: 'box-corners',
    title: 'Box Corners & Points (Anchor Joints)',
    category: '3D Shapes',
    badge: 'Points & Anchoring',
    badgeColor: '#8be9fd',
    description: `Extract all 8 corners of a cube/box to spawn corner joint spheres or anchor secondary parts parametrically.
Methods: cube.Corners, cube.GetCorners(), cube.GetUniqueVertices()
Anchor Points: cube.TopCenter, cube.BottomCenter, cube.LeftCenter, cube.RightCenter`,
    code: `// 1. Create a base cube
var cube = new Box(24, 24, 24);

// 2. Spawn corner node spheres at all 8 corner points of the cube
CsgObject joints = null;
foreach (var pt in cube.GetUniqueVertices())
{
    var node = new Sphere(radius: 3.5).Translate(pt);
    joints = (joints == null) ? node : joints + node;
}

// 3. Mount a cylinder tower on TopCenter
var tower = new Cylinder(radius: 6, height: 16).Translate(cube.TopCenter);

Draw(cube, RGB(60, 120, 220));
Draw(joints, Color.Orange);
Draw(tower, Color.Green);`,
    buildScene(scene) {
      const cubeGeo = new THREE.BoxGeometry(24, 24, 24);
      const cubeMat = new THREE.MeshStandardMaterial({ color: 0x3c78dc, roughness: 0.4 });
      scene.add(new THREE.Mesh(cubeGeo, cubeMat));

      const sphereGeo = new THREE.SphereGeometry(3.5, 24, 24);
      const sphereMat = new THREE.MeshStandardMaterial({ color: 0xffa500, roughness: 0.3 });
      for (let x of [-12, 12]) {
        for (let y of [-12, 12]) {
          for (let z of [-12, 12]) {
            const s = new THREE.Mesh(sphereGeo, sphereMat);
            s.position.set(x, y, z);
            scene.add(s);
          }
        }
      }

      const towerGeo = new THREE.CylinderGeometry(6, 6, 16, 32);
      const towerMat = new THREE.MeshStandardMaterial({ color: 0x50fa7b, roughness: 0.3 });
      const tower = new THREE.Mesh(towerGeo, towerMat);
      tower.position.set(0, 12 + 8, 0);
      scene.add(tower);
    }
  },

  {
    id: 'box-edges',
    title: 'Box Edges to Struts (Wireframe & Cage)',
    category: '3D Shapes',
    badge: 'Edges to Objects',
    badgeColor: '#8be9fd',
    description: `Extract all 12 bounding edges from a box as (Start, End) coordinate pairs and bridge them with cylinders to create wireframes or structural cages.
Methods: cube.BoundingEdges, cube.GetBoundingEdges(), cube.GetEdges()
Helper: CylinderBetween(start, end, radius) or Cylinder.CreateBetween(start, end, radius, sides)`,
    code: `// Extract all 12 bounding edges of a cube to construct a wireframe strut cage
var cube = new Box(28, 28, 28);
CsgObject cage = null;

foreach (var (start, end) in cube.GetBoundingEdges())
{
    var strut = CylinderBetween(start, end, radius: 1.2);
    cage = (cage == null) ? strut : cage + strut;
}

// Add a glowing core inside
var core = new Sphere(12);

Draw(cage, Color.Yellow);
Draw(core, RGB(255, 80, 120));`,
    buildScene(scene) {
      const coreGeo = new THREE.SphereGeometry(12, 32, 32);
      const coreMat = new THREE.MeshStandardMaterial({ color: 0xff5078, roughness: 0.2, metalness: 0.1 });
      scene.add(new THREE.Mesh(coreGeo, coreMat));

      const strutMat = new THREE.MeshStandardMaterial({ color: 0xf1fa8c, roughness: 0.3, metalness: 0.5 });
      const r = 1.2;
      const s = 28;
      const h = s / 2;

      const edges = [
        [[-h, -h, -h], [ h, -h, -h]], [[-h,  h, -h], [ h,  h, -h]], [[-h, -h,  h], [ h, -h,  h]], [[-h,  h,  h], [ h,  h,  h]],
        [[-h, -h, -h], [-h,  h, -h]], [[ h, -h, -h], [ h,  h, -h]], [[-h, -h,  h], [-h,  h,  h]], [[ h, -h,  h], [ h,  h,  h]],
        [[-h, -h, -h], [-h, -h,  h]], [[ h, -h, -h], [ h, -h,  h]], [[-h,  h, -h], [-h,  h,  h]], [[ h,  h, -h], [ h,  h,  h]],
      ];

      for (const [p1, p2] of edges) {
        const v1 = new THREE.Vector3(...p1);
        const v2 = new THREE.Vector3(...p2);
        const dir = new THREE.Vector3().subVectors(v2, v1);
        const len = dir.length();
        const strutGeo = new THREE.CylinderGeometry(r, r, len, 16);
        const strut = new THREE.Mesh(strutGeo, strutMat);
        strut.position.addVectors(v1, v2).multiplyScalar(0.5);
        strut.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir.clone().normalize());
        scene.add(strut);
      }
    }
  },

  {
    id: 'linq-filtering',
    title: 'LINQ Edge & Point Filtering',
    category: '3D Shapes',
    badge: 'LINQ & Filtering',
    badgeColor: '#8be9fd',
    description: `Query and filter 3D geometry points and edges using standard C# LINQ (.Where, .Select, .SelectMany, .Distinct, .OrderBy).
Filter edges to target specific faces (e.g. right side X > 0, top Z > 0), extract endpoints, and attach spheres or struts.`,
    code: `var cube = new Box(28, 28, 28);

// LINQ: filter only right-side points (X > 0) from edges
var rightPoints = cube.GetBoundingEdges()
    .SelectMany(e => new[] { e.Start, e.End })
    .Where(p => p.X > 0)
    .Distinct();

foreach (var pt in rightPoints)
{
    Draw(new Sphere(radius: 4.2).Translate(pt), Color.Red);
}

// LINQ: filter only right-side edges and bridge with struts
var rightEdges = cube.GetBoundingEdges()
    .Where(e => e.Start.X > 0 && e.End.X > 0);

foreach (var (start, end) in rightEdges)
{
    Draw(CylinderBetween(start, end, radius: 1.5), Color.Yellow);
}

Draw(cube, RGB(60, 80, 130));`,
    buildScene(scene) {
      const cubeGeo = new THREE.BoxGeometry(28, 28, 28);
      const cubeMat = new THREE.MeshStandardMaterial({ color: 0x3c5082, roughness: 0.5, transparent: true, opacity: 0.85 });
      scene.add(new THREE.Mesh(cubeGeo, cubeMat));

      const sphereGeo = new THREE.SphereGeometry(4.2, 20, 20);
      const sphereMat = new THREE.MeshStandardMaterial({ color: 0xff5555, roughness: 0.3 });
      for (let y of [-14, 14]) {
        for (let z of [-14, 14]) {
          const s = new THREE.Mesh(sphereGeo, sphereMat);
          s.position.set(14, y, z);
          scene.add(s);
        }
      }

      const strutMat = new THREE.MeshStandardMaterial({ color: 0xf1fa8c, roughness: 0.3 });
      const rightEdges = [
        [[14, -14, -14], [14, 14, -14]],
        [[14,  14, -14], [14, 14,  14]],
        [[14,  14,  14], [14, -14, 14]],
        [[14, -14,  14], [14, -14, -14]]
      ];
      for (const [p1, p2] of rightEdges) {
        const v1 = new THREE.Vector3(...p1);
        const v2 = new THREE.Vector3(...p2);
        const dir = new THREE.Vector3().subVectors(v2, v1);
        const strut = new THREE.Mesh(new THREE.CylinderGeometry(1.5, 1.5, dir.length(), 16), strutMat);
        strut.position.addVectors(v1, v2).multiplyScalar(0.5);
        strut.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir.normalize());
        scene.add(strut);
      }
    }
  },

  {
    id: 'cylinder',
    title: 'Cylinder',
    category: '3D Shapes',
    badge: 'Solid Primitive',
    badgeColor: '#8be9fd',
    description: `Creates a cylindrical solid centered along the Z axis (or X/Y via Alignment).
Parameters: radius, height, sides = 30, alignment = Alignment.z
Exposed Properties: Radius, Radius1, Radius2, Height, Alignment, Center, TopCenter, BottomCenter, Min, Max
Helper Method: CylinderBetween(startPoint, endPoint, radius, sides = 16)`,
    code: `// Cylinder(radius, height, sides = 30)
var cyl = new Cylinder(radius: 10, height: 28, sides: 36);
Draw(cyl, Color.Orange);`,
    buildScene(scene) {
      const geo = new THREE.CylinderGeometry(10, 10, 28, 36);
      const mat = new THREE.MeshStandardMaterial({ color: 0xffa500, roughness: 0.35, metalness: 0.2 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'cone',
    title: 'Cone / Tapered Cylinder',
    category: '3D Shapes',
    badge: 'Solid Primitive',
    badgeColor: '#8be9fd',
    description: `Creates a cone or tapered cylinder with different top and bottom radii.
Parameters: radius1 (bottom), radius2 (top, 0 for sharp cone), height, sides = 30, alignment
Exposed Properties: Radius1, Radius2, Height, Alignment, TopCenter, BottomCenter, Center`,
    code: `// Cylinder(radius1, radius2, height, sides)
var cone = new Cylinder(radius1: 14, radius2: 0, height: 25, sides: 36);
Draw(cone, Color.Yellow);`,
    buildScene(scene) {
      const geo = new THREE.CylinderGeometry(0, 14, 25, 36);
      const mat = new THREE.MeshStandardMaterial({ color: 0xf1fa8c, roughness: 0.35 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'sphere',
    title: 'Sphere',
    category: '3D Shapes',
    badge: 'Solid Primitive',
    badgeColor: '#8be9fd',
    description: `Creates a UV sphere centered at (0, 0, 0).
Parameters: radius, name = ""
Exposed Properties: Radius, Center, Min, Max, Bounds
Fluent Chaining: new Sphere(5).Translate(cube.TopCenter)`,
    code: `// Sphere(radius)
var sphere = new Sphere(radius: 12);
Draw(sphere, Color.Cyan);`,
    buildScene(scene) {
      const geo = new THREE.SphereGeometry(12, 36, 36);
      const mat = new THREE.MeshStandardMaterial({ color: 0x8be9fd, roughness: 0.25, metalness: 0.1 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'torus',
    title: 'Torus (Donut Ring)',
    category: '3D Shapes',
    badge: 'Solid Primitive',
    badgeColor: '#8be9fd',
    description: `Creates a 3D donut ring solid with customizable tube thickness and outer ring radius.
Parameters: innerRadius (tube radius), outerRadius (ring radius), alignment = Alignment.z
Exposed Properties: InnerRadius, OuterRadius, Alignment, Bounds, Center`,
    code: `// Torus(innerRadius: tubeRadius, outerRadius: ringRadius)
var ring = new Torus(innerRadius: 5, outerRadius: 20);
Draw(ring, RGB(255, 120, 200));`,
    buildScene(scene) {
      const geo = new THREE.TorusGeometry(20, 5, 24, 48);
      const mat = new THREE.MeshStandardMaterial({ color: 0xff78c8, roughness: 0.3, metalness: 0.2 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  // --------------------------------------------------------------------------
  // 2D Primitives
  // --------------------------------------------------------------------------
  {
    id: 'circle-2d',
    title: 'Circle (2D Profile)',
    category: '2D Primitives',
    badge: '2D Shape',
    badgeColor: '#50fa7b',
    description: `Represents a 2D planar circular boundary with radius and center.
Can be rendered directly with Draw() or extruded into 3D.
Methods: Extrude(height), TwistExtrude(height, twistDegrees), Revolve(angle), Loft(topShape, height)`,
    code: `// 2D Circle of radius 15
var c = new Circle(radius: 15);
Draw(c, RGB(80, 250, 123));`,
    buildScene(scene) {
      const geo = new THREE.CylinderGeometry(15, 15, 0.4, 48);
      const mat = new THREE.MeshStandardMaterial({ color: 0x50fa7b, roughness: 0.3 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'rectangle-2d',
    title: 'Rectangle (with Rounded Corners)',
    category: '2D Primitives',
    badge: '2D Shape',
    badgeColor: '#50fa7b',
    description: `Represents a 2D rectangle with optional corner fillet radius.
Parameters: width, height, cornerRadius = 0, centered = true
Properties: Width, Height, CornerRadius, Center, IsClosed`,
    code: `// Rectangle 30 wide x 16 tall with 3mm rounded corners
var rect = new Rectangle(30, 16, cornerRadius: 3, centered: true);
Draw(rect, RGB(139, 233, 253));`,
    buildScene(scene) {
      const shape = new THREE.Shape();
      const w = 30, h = 16, r = 3;
      const x = -w/2, y = -h/2;
      shape.moveTo(x + r, y);
      shape.lineTo(x + w - r, y);
      shape.quadraticCurveTo(x + w, y, x + w, y + r);
      shape.lineTo(x + w, y + h - r);
      shape.quadraticCurveTo(x + w, y + h, x + w - r, y + h);
      shape.lineTo(x + r, y + h);
      shape.quadraticCurveTo(x, y + h, x, y + h - r);
      shape.lineTo(x, y + r);
      shape.quadraticCurveTo(x, y, x + r, y);

      const geo = new THREE.ExtrudeGeometry(shape, { depth: 0.5, bevelEnabled: false });
      geo.center();
      const mat = new THREE.MeshStandardMaterial({ color: 0x8be9fd, roughness: 0.3 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'point-coordinate',
    title: 'Point (3D Coordinates & Anchoring)',
    category: '2D Primitives',
    badge: '2D / 3D Primitive',
    badgeColor: '#50fa7b',
    description: `Represents a 2D or 3D coordinate point (X, Y, Z). Fully compatible with Vector2, Vector3, Line, and Translate.
Use points to position solids, anchor objects, or connect coordinates with 3D lines.`,
    code: `// 1. Create a 3D coordinate point (X, Y, Z)
var pt = new Point(10, 15, 20);

// 2. Position a solid at the point coordinates (or call pt.ToSphere(3)):
var sphere = new Translate(new Sphere(3), pt);

// 3. Connect the point to origin with a 3D line:
var line = pt.LineTo(new Point(0, 0, 0));

Draw(sphere, RGB(255, 85, 85));
Draw(line, RGB(80, 200, 255));`,
    buildScene(scene) {
      const p1 = new THREE.Vector3(10, 20, 15);
      const p0 = new THREE.Vector3(0, 0, 0);

      const sphereGeo = new THREE.SphereGeometry(3, 24, 24);
      const sphereMat = new THREE.MeshStandardMaterial({ color: 0xff5555, roughness: 0.3 });
      const s = new THREE.Mesh(sphereGeo, sphereMat);
      s.position.copy(p1);
      scene.add(s);

      const dir = new THREE.Vector3().subVectors(p1, p0);
      const lineGeo = new THREE.CylinderGeometry(0.5, 0.5, dir.length(), 16);
      const lineMat = new THREE.MeshStandardMaterial({ color: 0x50c8ff, roughness: 0.3 });
      const line = new THREE.Mesh(lineGeo, lineMat);
      line.position.addVectors(p0, p1).multiplyScalar(0.5);
      line.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), dir.normalize());
      scene.add(line);
    }
  },

  // --------------------------------------------------------------------------
  // 2D => 3D Operations
  // --------------------------------------------------------------------------
  {
    id: 'extrude',
    title: 'Linear Extrude (2D => 3D)',
    category: '2D => 3D',
    badge: 'Solid Generator',
    badgeColor: '#bd93f9',
    description: `Extrudes any closed 2D shape (Circle, Rectangle, Square, or closed Path) linearly along an axis.
Parameters: height, centered = false, alignment = Alignment.z
Global method: Extrude(shape, height) or fluent shape.Extrude(height)`,
    code: `// 1. Define closed 2D profile
var c = new Circle(radius: 12);

// 2. Extrude linearly along Z into a solid column
var column = Extrude(c, height: 35, centered: true);

Draw(column, RGB(46, 204, 113));`,
    buildScene(scene) {
      const geo = new THREE.CylinderGeometry(12, 12, 35, 36);
      const mat = new THREE.MeshStandardMaterial({ color: 0x2ecc71, roughness: 0.35, metalness: 0.1 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'twist-extrude',
    title: 'Twist Extrude (Helical Screw & Spiral)',
    category: '2D => 3D',
    badge: 'Helical Solid',
    badgeColor: '#bd93f9',
    description: `Extrudes a 2D shape while twisting around its centroid by specified degrees.
Creates drills, helical gears, screws, turbine impellers, and spiral pillars.
Parameters: height, twistDegrees = 360, slices = 30, scale = 1.0, centered = false`,
    code: `// Create a square and twist-extrude 180 degrees
var sq = new Square(size: 16, centered: true);
var spiral = TwistExtrude(sq, height: 40, twistDegrees: 180, slices: 40);

Draw(spiral, RGB(255, 121, 198));`,
    buildScene(scene) {
      const shape = new THREE.Shape();
      const s = 8;
      shape.moveTo(-s, -s);
      shape.lineTo(s, -s);
      shape.lineTo(s, s);
      shape.lineTo(-s, s);
      shape.closePath();

      const extrudeSettings = {
        steps: 40,
        depth: 40,
        bevelEnabled: false
      };

      const geo = new THREE.ExtrudeGeometry(shape, extrudeSettings);
      geo.center();

      // Apply twist to vertex positions along Y
      const pos = geo.attributes.position;
      const angleTotal = Math.PI; // 180 deg
      for (let i = 0; i < pos.count; i++) {
        const y = pos.getY(i);
        const factor = (y + 20) / 40; // 0 to 1
        const theta = factor * angleTotal;
        const x = pos.getX(i);
        const z = pos.getZ(i);
        pos.setX(i, x * Math.cos(theta) - z * Math.sin(theta));
        pos.setZ(i, x * Math.sin(theta) + z * Math.cos(theta));
      }
      geo.computeVertexNormals();

      const mat = new THREE.MeshStandardMaterial({ color: 0xff79c6, roughness: 0.35, metalness: 0.15 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'revolve',
    title: 'Revolve (Lathe / Rotational Solid)',
    category: '2D => 3D',
    badge: 'Rotational Solid',
    badgeColor: '#bd93f9',
    description: `Rotates a 2D curve around an axis to produce rotational solids of revolution (vases, pulleys, bowls, rims, torus-like rings).
Parameters: angleDegrees = 360, angleSteps = 30, axisOffset = 0, revolveAroundZ = true`,
    code: `// Offset circle revolved into a torus-like donut solid
var c = new Circle(x: 18, y: 0, radius: 6);
var donut = Revolve(c, angleDegrees: 360, angleSteps: 36);

Draw(donut, RGB(241, 196, 15));`,
    buildScene(scene) {
      const geo = new THREE.TorusGeometry(18, 6, 24, 48);
      const mat = new THREE.MeshStandardMaterial({ color: 0xf1c40f, roughness: 0.3, metalness: 0.2 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  {
    id: 'loft',
    title: 'Loft (Profile to Profile Transition)',
    category: '2D => 3D',
    badge: 'Smooth Transition',
    badgeColor: '#bd93f9',
    description: `Generates a smooth aerodynamic 3D skin transition between two different 2D cross-sections across height (e.g., Circle at bottom to Square at top).
Parameters: bottom, top, height, twistDegrees = 0, centered = false`,
    code: `var bot = new Circle(16);
var top = new Square(16, centered: true);

// Smooth transition from circular base to square top
var blended = Loft(bot, top, height: 35, twistDegrees: 45);

Draw(blended, RGB(80, 200, 255));`,
    buildScene(scene) {
      // Create loft geometry using parametric surface
      const slices = 30;
      const height = 35;
      const r = 16;
      const s = 16 / 2;
      const geo = new THREE.CylinderGeometry(s * 1.2, r, height, slices, 10);
      const mat = new THREE.MeshStandardMaterial({ color: 0x50c8ff, roughness: 0.35, metalness: 0.1 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  // --------------------------------------------------------------------------
  // CSG Booleans
  // --------------------------------------------------------------------------
  {
    id: 'boolean-union',
    title: 'CSG Union (Join Solids / + Operator)',
    category: 'CSG Booleans',
    badge: 'Boolean Join',
    badgeColor: '#ff79c6',
    description: `Fuses multiple overlapping solids into a single continuous solid geometry without internal walls.
Syntax: new Union(a, b) or operator: a + b`,
    code: `// Join two intersecting boxes into a cross solid
var cross = new Union(
    new Box(28, 10, 10, createCentered: true),
    new Box(10, 28, 10, createCentered: true)
);

Draw(cross, RGB(80, 250, 123));`,
    buildScene(scene) {
      const b1 = new THREE.Mesh(new THREE.BoxGeometry(28, 10, 10), new THREE.MeshStandardMaterial({ color: 0x50fa7b, roughness: 0.35 }));
      const b2 = new THREE.Mesh(new THREE.BoxGeometry(10, 10, 28), new THREE.MeshStandardMaterial({ color: 0x50fa7b, roughness: 0.35 }));
      const group = new THREE.Group();
      group.add(b1);
      group.add(b2);
      scene.add(group);
    }
  },

  {
    id: 'boolean-difference',
    title: 'CSG Difference (Cutout / - Operator)',
    category: 'CSG Booleans',
    badge: 'Boolean Cutout',
    badgeColor: '#ff79c6',
    description: `Subtracts cutting solids from a primary solid to carve holes, notches, slots, or cavities.
Syntax: new Difference(primary, cutter) or operator: primary - cutter`,
    code: `// Carve a cylindrical through-hole out of a solid block
var block = new Box(24, 24, 24, createCentered: true);
var hole  = new Cylinder(7, 35);

var boredBlock = block - hole;
Draw(boredBlock, RGB(255, 85, 85));`,
    buildScene(scene) {
      // Simulate difference using box with hollow center
      const outerGeo = new THREE.BoxGeometry(24, 24, 24);
      const outerMat = new THREE.MeshStandardMaterial({ color: 0xff5555, roughness: 0.4, side: THREE.DoubleSide });
      const group = new THREE.Group();

      // 4 wall segments around central bore
      const wallMat = new THREE.MeshStandardMaterial({ color: 0xff5555, roughness: 0.4 });
      const w1 = new THREE.Mesh(new THREE.BoxGeometry(24, 24, 5), wallMat); w1.position.z = 9.5;
      const w2 = new THREE.Mesh(new THREE.BoxGeometry(24, 24, 5), wallMat); w2.position.z = -9.5;
      const w3 = new THREE.Mesh(new THREE.BoxGeometry(5, 24, 14), wallMat); w3.position.x = 9.5;
      const w4 = new THREE.Mesh(new THREE.BoxGeometry(5, 24, 14), wallMat); w4.position.x = -9.5;

      const innerCyl = new THREE.Mesh(new THREE.CylinderGeometry(7, 7, 24, 32, 1, true), new THREE.MeshStandardMaterial({ color: 0xcc4444, roughness: 0.3, side: THREE.BackSide }));

      group.add(w1, w2, w3, w4, innerCyl);
      scene.add(group);
    }
  },

  {
    id: 'boolean-intersection',
    title: 'CSG Intersection (Shared Volume / * Operator)',
    category: 'CSG Booleans',
    badge: 'Boolean Overlap',
    badgeColor: '#ff79c6',
    description: `Keeps only the overlapping shared volume common to all solids.
Syntax: new Intersection(a, b) or operator: a * b`,
    code: `// Intersection of a cube and a sphere produces a smooth-edged cube
var cube = new Box(24, 24, 24, createCentered: true);
var ball = new Sphere(15.5);

var roundedCube = cube * ball;
Draw(roundedCube, RGB(241, 250, 140));`,
    buildScene(scene) {
      const geo = new THREE.SphereGeometry(15, 36, 36);
      // Flatten vertices exceeding cube bounds
      const pos = geo.attributes.position;
      const limit = 12;
      for (let i = 0; i < pos.count; i++) {
        pos.setX(i, Math.max(-limit, Math.min(limit, pos.getX(i))));
        pos.setY(i, Math.max(-limit, Math.min(limit, pos.getY(i))));
        pos.setZ(i, Math.max(-limit, Math.min(limit, pos.getZ(i))));
      }
      geo.computeVertexNormals();

      const mat = new THREE.MeshStandardMaterial({ color: 0xf1fa8c, roughness: 0.35 });
      scene.add(new THREE.Mesh(geo, mat));
    }
  },

  // --------------------------------------------------------------------------
  // Complex Assemblies
  // --------------------------------------------------------------------------
  {
    id: 'flange-hub',
    title: 'Flange Hub with Circular Bolt Array',
    category: 'Complex Assemblies',
    badge: 'Mechanical Assembly',
    badgeColor: '#ffb86c',
    description: `Complete mechanical assembly: base mounting disk, center collar hub, central through-bore shaft hole, and a circular array of 6 counter-bored bolt holes created with a procedural loop.`,
    code: `// 1. Base Disk & Center Hub
var baseDisk = new Cylinder(radius: 32, height: 7, sides: 32);
var hub      = new Cylinder(radius: 17, height: 20, sides: 28);

// 2. Center Through-Bore
var centerBore = new Cylinder(radius: 9, height: 32, sides: 24);

// 3. Circular array of 6 bolt holes
CsgObject boltHoles = null;
for (int i = 0; i < 6; i++) {
    double angle = i * MathHelper.Tau / 6;
    var hole = new Translate(new Cylinder(2.8, 16, 16), Math.Cos(angle) * 24, Math.Sin(angle) * 24, 0);
    boltHoles = (boltHoles == null) ? hole : boltHoles + hole;
}

// 4. CSG combine and draw
var flange = (baseDisk + hub) - (centerBore + boltHoles);
Draw(flange, RGB(80, 180, 255));`,
    buildScene(scene) {
      const group = new THREE.Group();
      const mat = new THREE.MeshStandardMaterial({ color: 0x50b4ff, roughness: 0.35, metalness: 0.3 });

      // Base disk
      const diskGeo = new THREE.CylinderGeometry(32, 32, 7, 36);
      const disk = new THREE.Mesh(diskGeo, mat);

      // Hub
      const hubGeo = new THREE.CylinderGeometry(17, 17, 13, 32);
      const hub = new THREE.Mesh(hubGeo, mat);
      hub.position.y = 10;

      group.add(disk, hub);

      // Add dark bolt hole indicators
      const holeMat = new THREE.MeshStandardMaterial({ color: 0x14151d, roughness: 0.8 });
      const boreIndicator = new THREE.Mesh(new THREE.CylinderGeometry(9, 9, 28, 24), holeMat);
      boreIndicator.position.y = 5;
      group.add(boreIndicator);

      for (let i = 0; i < 6; i++) {
        const theta = (i * Math.PI * 2) / 6;
        const bh = new THREE.Mesh(new THREE.CylinderGeometry(2.8, 2.8, 8, 16), holeMat);
        bh.position.set(Math.cos(theta) * 24, 0.5, Math.sin(theta) * 24);
        group.add(bh);
      }

      scene.add(group);
    }
  },

  {
    id: 'perforated-sphere',
    title: 'Perforated Cage Sphere & Suspended Gem',
    category: 'Complex Assemblies',
    badge: 'CSG Sculpting',
    badgeColor: '#ffb86c',
    description: `Constructs a hollow spherical shell perforated by orthogonal boring cylinders along X, Y, and Z axes, revealing an inner diamond gem suspended at the core.`,
    code: `// 1. Outer shell sphere minus concentric inner hollow cavity
var outerSphere = new Sphere(radius: 20);
var innerHollow = new Sphere(radius: 16.5);

// 2. Orthogonal perforation boring cylinders (X, Y, Z axes)
var boreX = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.x);
var boreY = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.y);
var boreZ = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.z);

// 3. Subtract to sculpt perforated cage shell
var cage = outerSphere - innerHollow - boreX - boreY - boreZ;

// 4. Inner suspended diamond gem
var gem = new Rotate(new Box(11, 11, 11),
    x: MathHelper.DegreesToRadians(45),
    y: MathHelper.DegreesToRadians(45));

Draw(cage, RGB(60, 140, 240));
Draw(gem, RGB(255, 75, 130));`,
    buildScene(scene) {
      // Wireframe / perforated sphere shell representation
      const cageGeo = new THREE.SphereGeometry(20, 24, 24);
      const cageMat = new THREE.MeshStandardMaterial({
        color: 0x3c8cf0,
        roughness: 0.3,
        metalness: 0.2,
        wireframe: true
      });
      const cage = new THREE.Mesh(cageGeo, cageMat);
      scene.add(cage);

      // Inner diamond gem
      const gemGeo = new THREE.OctahedronGeometry(9, 0);
      const gemMat = new THREE.MeshStandardMaterial({
        color: 0xff4b82,
        roughness: 0.15,
        metalness: 0.3,
        emissive: 0x551122
      });
      const gem = new THREE.Mesh(gemGeo, gemMat);
      scene.add(gem);
    }
  },

  {
    id: 'spiral-staircase',
    title: 'Spiral Helical Staircase & Balusters',
    category: 'Complex Assemblies',
    badge: 'Procedural Assembly',
    badgeColor: '#ffb86c',
    description: `Procedural winding staircase with a central newel post column, 16 cantilevering spiral steps, and outer vertical baluster spindles generated algorithmically.`,
    code: `// 1. Central newel post column
var pillar = new Cylinder(radius: 4, height: 60, sides: 28);

// 2. Procedural winding steps and baluster spindles
CsgObject steps = null;
CsgObject balusters = null;
int stepCount = 16;

for (int i = 0; i < stepCount; i++)
{
    double z = (i - 8) * 3.5;
    double angle = i * MathHelper.Tau / 12;

    var tread = new Translate(
        new Rotate(new Box(20, 6, 2.5), z: angle),
        Math.Cos(angle) * 10, Math.Sin(angle) * 10, z);
    steps = (steps == null) ? tread : steps + tread;

    double outerX = Math.Cos(angle) * 19;
    double outerY = Math.Sin(angle) * 19;
    var baluster = new Cylinder(radius: 0.9, height: 12, sides: 12)
        .Translate(outerX, outerY, z + 6);
    balusters = (balusters == null) ? baluster : balusters + baluster;
}

Draw(pillar, RGB(180, 185, 200));
Draw(steps, RGB(255, 155, 60));
Draw(balusters, RGB(80, 190, 255));`,
    buildScene(scene) {
      const group = new THREE.Group();

      // Central pillar
      const pillarGeo = new THREE.CylinderGeometry(4, 4, 60, 24);
      const pillarMat = new THREE.MeshStandardMaterial({ color: 0xb4b9c8, roughness: 0.4 });
      group.add(new THREE.Mesh(pillarGeo, pillarMat));

      const stepMat = new THREE.MeshStandardMaterial({ color: 0xff9b3c, roughness: 0.35 });
      const balusterMat = new THREE.MeshStandardMaterial({ color: 0x50beff, roughness: 0.3 });

      const stepCount = 16;
      for (let i = 0; i < stepCount; i++) {
        const y = (i - 8) * 3.5;
        const angle = (i * Math.PI * 2) / 12;

        const tread = new THREE.Mesh(new THREE.BoxGeometry(20, 2.5, 6), stepMat);
        tread.position.set(Math.cos(angle) * 10, y, Math.sin(angle) * 10);
        tread.rotation.y = -angle;
        group.add(tread);

        const bal = new THREE.Mesh(new THREE.CylinderGeometry(0.9, 0.9, 12, 12), balusterMat);
        bal.position.set(Math.cos(angle) * 19, y + 6, Math.sin(angle) * 19);
        group.add(bal);
      }

      scene.add(group);
    }
  }
];

// ============================================================================
// Application & Viewport Controller
// ============================================================================

class ViewportController {
  constructor(canvas, buildSceneCallback) {
    this.canvas = canvas;
    this.buildSceneCallback = buildSceneCallback;
    this.isVisible = false;
    this.isWireframe = false;
    this.autoRotate = false;

    this.initThree();
  }

  initThree() {
    const width = this.canvas.clientWidth || 480;
    const height = this.canvas.clientHeight || 380;

    // Renderer
    this.renderer = new THREE.WebGLRenderer({
      canvas: this.canvas,
      antialias: true,
      alpha: false
    });
    this.renderer.setSize(width, height, false);
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    this.renderer.setClearColor(0x14151d, 1.0);

    // Scene
    this.scene = new THREE.Scene();

    // Subtle coordinate ground grid
    const grid = new THREE.GridHelper(80, 20, 0x383a59, 0x222430);
    grid.position.y = -20;
    this.scene.add(grid);

    // Lighting
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.7);
    this.scene.add(ambientLight);

    const dirLight1 = new THREE.DirectionalLight(0xffffff, 0.9);
    dirLight1.position.set(50, 80, 60);
    this.scene.add(dirLight1);

    const dirLight2 = new THREE.DirectionalLight(0x8be9fd, 0.4);
    dirLight2.position.set(-50, -40, -40);
    this.scene.add(dirLight2);

    // Camera
    this.camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 1000);
    this.initialCameraPos = new THREE.Vector3(45, 35, 50);
    this.camera.position.copy(this.initialCameraPos);

    // Controls
    this.controls = new OrbitControls(this.camera, this.canvas);
    this.controls.enableDamping = true;
    this.controls.dampingFactor = 0.08;
    this.controls.target.set(0, 0, 0);

    // Build model
    this.modelGroup = new THREE.Group();
    this.scene.add(this.modelGroup);
    if (this.buildSceneCallback) {
      this.buildSceneCallback(this.modelGroup);
    }

    // Auto-fit bounds
    this.fitCameraToBounds();

    // Resize observer
    this.resizeObserver = new ResizeObserver(() => this.handleResize());
    this.resizeObserver.observe(this.canvas.parentElement);
  }

  fitCameraToBounds() {
    const box = new THREE.Box3().setFromObject(this.modelGroup);
    if (box.isEmpty()) return;

    const center = box.getCenter(new THREE.Vector3());
    const size = box.getSize(new THREE.Vector3());
    const maxDim = Math.max(size.x, size.y, size.z, 20);

    const fov = this.camera.fov * (Math.PI / 180);
    let cameraZ = Math.abs(maxDim / 2 / Math.tan(fov / 2)) * 1.6;
    cameraZ = Math.max(cameraZ, 30);

    this.initialCameraPos.set(cameraZ * 0.75, cameraZ * 0.6, cameraZ * 0.9);
    this.initialTarget = center.clone();

    this.resetCamera();
  }

  resetCamera() {
    this.camera.position.copy(this.initialCameraPos);
    this.controls.target.copy(this.initialTarget || new THREE.Vector3(0, 0, 0));
    this.controls.update();
  }

  toggleWireframe() {
    this.isWireframe = !this.isWireframe;
    this.modelGroup.traverse(child => {
      if (child.isMesh && child.material) {
        if (Array.isArray(child.material)) {
          child.material.forEach(m => m.wireframe = this.isWireframe);
        } else {
          child.material.wireframe = this.isWireframe;
        }
      }
    });
  }

  toggleAutoRotate() {
    this.autoRotate = !this.autoRotate;
    this.controls.autoRotate = this.autoRotate;
    this.controls.autoRotateSpeed = 2.0;
  }

  zoom(factor) {
    this.camera.position.sub(this.controls.target).multiplyScalar(factor).add(this.controls.target);
    this.controls.update();
  }

  handleResize() {
    const parent = this.canvas.parentElement;
    if (!parent) return;
    const width = parent.clientWidth;
    const height = parent.clientHeight;
    if (width === 0 || height === 0) return;

    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
    this.renderer.setSize(width, height, false);
  }

  render() {
    if (!this.isVisible) return;
    this.controls.update();
    this.renderer.render(this.scene, this.camera);
  }
}

// Global active viewports
const activeViewports = new Map();

function createCardElement(item) {
  const card = document.createElement('article');
  card.className = 'card-item';
  card.id = `example-${item.id}`;
  card.dataset.category = item.category;
  card.dataset.title = item.title.toLowerCase();
  card.dataset.code = item.code.toLowerCase();

  const highlightedCode = highlightCSharp(item.code);

  card.innerHTML = `
    <header class="card-header">
      <div class="card-header-left">
        <h3 class="card-title">${escapeHtml(item.title)}</h3>
        <span class="card-badge" style="background: ${item.badgeColor}22; color: ${item.badgeColor}; border: 1px solid ${item.badgeColor}66;">
          ${escapeHtml(item.badge)}
        </span>
      </div>
      <p class="card-desc">${escapeHtml(item.description)}</p>
    </header>
    <div class="card-body">
      <!-- 3D Viewport Column (Left) -->
      <div class="viewport-pane">
        <div class="viewport-hud">
          <div class="hud-group">
            <button class="hud-btn btn-home" title="Center camera view">🏠 Home</button>
            <button class="hud-btn btn-wireframe" title="Toggle wireframe mode">🌐 Wire</button>
            <button class="hud-btn btn-rotate" title="Toggle auto-rotation">🔄 Spin</button>
          </div>
          <div class="hud-group">
            <button class="hud-btn btn-zoom-in" title="Zoom in">+</button>
            <button class="hud-btn btn-zoom-out" title="Zoom out">-</button>
          </div>
        </div>
        <canvas class="three-canvas" id="canvas-${item.id}"></canvas>
        <div class="viewport-hint">🖱 Left-drag: Rotate • Right-drag: Pan • Wheel: Zoom</div>
      </div>

      <!-- Code Column (Right) -->
      <div class="code-pane">
        <div class="code-header">
          <span class="code-lang-label">C# Script</span>
          <button class="copy-btn" data-code="${encodeURIComponent(item.code)}">
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
              <rect x="9" y="9" width="13" height="13" rx="2" ry="2"></rect>
              <path d="M5 15H4a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2h9a2 2 0 0 1 2 2v1"></path>
            </svg>
            <span>Copy Code</span>
          </button>
        </div>
        <div class="code-container">
          <pre><code>${highlightCodePreservingLines(highlightedCode)}</code></pre>
        </div>
      </div>
    </div>
  `;

  return card;
}

function highlightCodePreservingLines(highlightedText) {
  return highlightedText;
}

function initApp() {
  const container = document.getElementById('examples-container');
  if (!container) return;

  // Render cards grouped or linear
  let currentCategory = '';
  EXAMPLES.forEach(item => {
    if (item.category !== currentCategory) {
      currentCategory = item.category;
      const divider = document.createElement('div');
      divider.className = 'section-divider';
      divider.innerHTML = `
        <span class="section-divider-title">${escapeHtml(currentCategory)}</span>
        <div class="section-divider-line"></div>
      `;
      container.appendChild(divider);
    }
    const cardEl = createCardElement(item);
    container.appendChild(cardEl);
  });

  // Init Three.js viewports using IntersectionObserver for top performance
  const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
      const card = entry.target;
      const vp = activeViewports.get(card.id);
      if (vp) {
        vp.isVisible = entry.isIntersecting;
        if (entry.isIntersecting) {
          vp.handleResize();
        }
      }
    });
  }, { threshold: 0.1 });

  EXAMPLES.forEach(item => {
    const cardId = `example-${item.id}`;
    const cardEl = document.getElementById(cardId);
    const canvas = document.getElementById(`canvas-${item.id}`);
    if (canvas && cardEl) {
      const vp = new ViewportController(canvas, item.buildScene);
      activeViewports.set(cardId, vp);
      observer.observe(cardEl);

      // Bind HUD Buttons
      const btnHome = cardEl.querySelector('.btn-home');
      const btnWire = cardEl.querySelector('.btn-wireframe');
      const btnRotate = cardEl.querySelector('.btn-rotate');
      const btnZoomIn = cardEl.querySelector('.btn-zoom-in');
      const btnZoomOut = cardEl.querySelector('.btn-zoom-out');

      if (btnHome) btnHome.addEventListener('click', () => vp.resetCamera());
      if (btnWire) btnWire.addEventListener('click', () => {
        vp.toggleWireframe();
        btnWire.style.borderColor = vp.isWireframe ? 'var(--accent-green)' : '';
      });
      if (btnRotate) btnRotate.addEventListener('click', () => {
        vp.toggleAutoRotate();
        btnRotate.style.borderColor = vp.autoRotate ? 'var(--accent-cyan)' : '';
      });
      if (btnZoomIn) btnZoomIn.addEventListener('click', () => vp.zoom(0.8));
      if (btnZoomOut) btnZoomOut.addEventListener('click', () => vp.zoom(1.25));
    }
  });

  // Global Animation Loop
  function animate() {
    requestAnimationFrame(animate);
    activeViewports.forEach(vp => {
      if (vp.isVisible) {
        vp.render();
      }
    });
  }
  animate();

  // Copy code buttons
  document.querySelectorAll('.copy-btn').forEach(btn => {
    btn.addEventListener('click', async () => {
      const code = decodeURIComponent(btn.dataset.code);
      try {
        await navigator.clipboard.writeText(code);
        const originalText = btn.innerHTML;
        btn.classList.add('copied');
        btn.innerHTML = `
          <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
            <polyline points="20 6 9 17 4 12"></polyline>
          </svg>
          <span>Copied!</span>
        `;
        setTimeout(() => {
          btn.classList.remove('copied');
          btn.innerHTML = originalText;
        }, 1500);
      } catch (err) {
        console.error('Clipboard copy failed:', err);
      }
    });
  });

  // Category filter tabs
  const tabButtons = document.querySelectorAll('.tab-btn');
  tabButtons.forEach(btn => {
    btn.addEventListener('click', () => {
      tabButtons.forEach(b => b.classList.remove('active'));
      btn.classList.add('active');
      const category = btn.dataset.category;
      filterExamples(category, document.getElementById('search-input')?.value || '');
    });
  });

  // Search input filter
  const searchInput = document.getElementById('search-input');
  if (searchInput) {
    searchInput.addEventListener('input', (e) => {
      const activeTab = document.querySelector('.tab-btn.active');
      const category = activeTab ? activeTab.dataset.category : 'All';
      filterExamples(category, e.target.value);
    });
  }
}

function filterExamples(category, query) {
  const q = query.toLowerCase().trim();
  const cards = document.querySelectorAll('.card-item');
  const dividers = document.querySelectorAll('.section-divider');

  let matchCount = 0;
  cards.forEach(card => {
    const cardCat = card.dataset.category;
    const matchesCat = (category === 'All' || cardCat === category);
    const matchesQuery = !q || card.dataset.title.includes(q) || card.dataset.code.includes(q);

    if (matchesCat && matchesQuery) {
      card.style.display = 'flex';
      matchCount++;
      const vp = activeViewports.get(card.id);
      if (vp) vp.handleResize();
    } else {
      card.style.display = 'none';
    }
  });

  // Show/hide dividers if filtering
  dividers.forEach(div => {
    div.style.display = (category === 'All' && !q) ? 'flex' : 'none';
  });

  const emptyState = document.getElementById('empty-state');
  if (emptyState) {
    emptyState.style.display = matchCount === 0 ? 'block' : 'none';
  }
}

// Start app once DOM is loaded
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initApp);
} else {
  initApp();
}
