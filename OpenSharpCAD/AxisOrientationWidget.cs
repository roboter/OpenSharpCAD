using System;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace CSharpCAD
{
    /// <summary>
    /// A 2D overlay widget that shows the current 3D orientation of the world view as three
    /// color-coded axis arrows (X=Red, Y=Green, Z=Blue) in the bottom-left corner of the viewport.
    /// It reads the WorldView.RotationMatrix every frame so it always matches the trackball state.
    /// The disc background is rendered using the agg Ellipse vertex source for smooth anti-aliasing.
    /// </summary>
    public class AxisOrientationWidget : GuiWidget
    {
        // ── size / layout ─────────────────────────────────────────────────────────
        private const double WidgetSize  = 150;  // overall square widget size in pixels
        private const double DiscRadius  = 70;   // radius of the background disc
        private const double ArmsRadius  = 52;   // length of each axis arm in pixels
        private const double ArrowHead   = 9;    // half-width of arrowhead
        private const double LineWidth   = 3.5;  // axis line stroke width
        private const double LabelPush   = 10;   // extra push past arrow tip for label
        private const double LabelSize   = 12;   // font point size for axis labels

        // ── colours ───────────────────────────────────────────────────────────────
        private static readonly Color ColX    = new Color(230,  55,  55, 240);
        private static readonly Color ColY    = new Color( 55, 210,  55, 240);
        private static readonly Color ColZ    = new Color( 60, 130, 230, 240);
        private static readonly Color ColBg   = new Color( 18,  20,  32, 200);
        private static readonly Color ColRim  = new Color(255, 255, 255,  55);
        private static readonly Color ColDot  = new Color(255, 255, 255, 200);

        // world-space unit axes
        private static readonly Vector3 AxisX = Vector3.UnitX;
        private static readonly Vector3 AxisY = Vector3.UnitY;
        private static readonly Vector3 AxisZ = Vector3.UnitZ;

        private readonly WorldView _world;

        public AxisOrientationWidget(WorldView world)
            : base(WidgetSize, WidgetSize)
        {
            _world      = world ?? throw new ArgumentNullException(nameof(world));
            HAnchor     = HAnchor.Left;
            VAnchor     = VAnchor.Bottom;
            Margin      = new BorderDouble(12, 12);
            Selectable  = false;   // don't eat mouse events
        }

        public override void OnDraw(Graphics2D g)
        {
            base.OnDraw(g);
            DrawGizmo(g);
        }

        // ─────────────────────────────────────────────────────────────────────────
        private void DrawGizmo(Graphics2D g)
        {
            double cx = WidgetSize / 2;
            double cy = WidgetSize / 2;

            // ── smooth filled background disc ─────────────────────────────────────
            // Ellipse vertex source + g.Render = anti-aliased filled shape
            var bgDisc = new Ellipse(cx, cy, DiscRadius - 1, DiscRadius - 1);
            g.Render(bgDisc, ColBg);

            // anti-aliased rim (rendered as a thin stroked circle)
            g.Circle(cx, cy, DiscRadius - 1, ColRim);

            // ── project the three world-space axes into screen space ──────────────
            var rot = _world.RotationMatrix;
            Vector3 sx = ProjectAxis(AxisX, rot);
            Vector3 sy = ProjectAxis(AxisY, rot);
            Vector3 sz = ProjectAxis(AxisZ, rot);

            // depth-sort: draw farthest (lowest Z) first so nearer axes appear on top
            var axes = new (Vector3 screen, Color col, string label)[]
            {
                (sx, ColX, "X"),
                (sy, ColY, "Y"),
                (sz, ColZ, "Z"),
            };
            SortByDepth(ref axes);

            foreach (var (screen, col, label) in axes)
            {
                double ex = cx + screen.X * ArmsRadius;
                double ey = cy + screen.Y * ArmsRadius;

                // axes going away from the viewer (Z < 0) get reduced alpha
                Color drawCol = screen.Z >= 0
                    ? col
                    : new Color(col.red, col.green, col.blue, (byte)110);

                DrawArrow(g, cx, cy, ex, ey, drawCol);
                DrawLabel(g, ex, ey, screen.X, screen.Y, label, drawCol);
            }

            // ── central dot ───────────────────────────────────────────────────────
            var dot = new Ellipse(cx, cy, 3.5, 3.5);
            g.Render(dot, ColDot);
        }

        /// <summary>
        /// Projects a world-space direction vector into the gizmo's 2D screen plane
        /// using only the rotational part of the current WorldView.
        /// Returns (screenX ∈ [-1,1], screenY ∈ [-1,1], viewDepth).
        /// </summary>
        private static Vector3 ProjectAxis(Vector3 dir, Matrix4X4 rot)
        {
            // Row-major multiply: v_view = v_world * rot (upper-left 3×3)
            double rx = dir.X * rot[0, 0] + dir.Y * rot[1, 0] + dir.Z * rot[2, 0];
            double ry = dir.X * rot[0, 1] + dir.Y * rot[1, 1] + dir.Z * rot[2, 1];
            double rz = dir.X * rot[0, 2] + dir.Y * rot[1, 2] + dir.Z * rot[2, 2];
            return new Vector3(rx, ry, rz);
        }

        // insertion sort (3 elements, trivial) – sort ascending by Z so we draw far → near
        private static void SortByDepth(
            ref (Vector3 screen, Color col, string label)[] arr)
        {
            for (int i = 1; i < arr.Length; i++)
            {
                var key = arr[i];
                int j   = i - 1;
                while (j >= 0 && arr[j].screen.Z > key.screen.Z)
                {
                    arr[j + 1] = arr[j];
                    j--;
                }
                arr[j + 1] = key;
            }
        }

        // ── drawing helpers ───────────────────────────────────────────────────────

        private static void DrawArrow(
            Graphics2D g,
            double x0, double y0,
            double x1, double y1,
            Color col)
        {
            // shaft
            g.Line(x0, y0, x1, y1, col, LineWidth);

            // arrowhead cap lines
            double dx  = x1 - x0;
            double dy  = y1 - y0;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len < 1e-4) return;
            double ux = dx / len;
            double uy = dy / len;
            double px = -uy;          // perpendicular
            double py =  ux;

            double hx = x1 - ux * ArrowHead * 1.1;
            double hy = y1 - uy * ArrowHead * 1.1;
            double hw = ArrowHead * 0.55;

            g.Line(x1, y1, hx + px * hw, hy + py * hw, col, LineWidth);
            g.Line(x1, y1, hx - px * hw, hy - py * hw, col, LineWidth);
        }

        private static void DrawLabel(
            Graphics2D g,
            double ex, double ey,
            double sx, double sy,
            string label,
            Color col)
        {
            // normalise direction so label is always a fixed distance past the tip
            double nl = Math.Sqrt(sx * sx + sy * sy);
            double nx = nl > 1e-4 ? sx / nl : 0;
            double ny = nl > 1e-4 ? sy / nl : 0;

            double lx = ex + nx * LabelPush;
            double ly = ey + ny * LabelPush;

            g.DrawString(
                label,
                lx, ly,
                LabelSize,
                justification: Justification.Center,
                baseline: Baseline.BoundsCenter,
                color: col);
        }
    }
}
