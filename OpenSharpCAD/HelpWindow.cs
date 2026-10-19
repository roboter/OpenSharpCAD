using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.Csg;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.PolygonMesh;
using MatterHackers.RenderOpenGl;
using MatterHackers.RenderOpenGl.OpenGl;
using MatterHackers.VectorMath;
using MatterHackers.VectorMath.TrackBall;
using Path = MatterHackers.Csg.Solids.Path;
using Circle = MatterHackers.Csg.Solids.Circle;
using Face = MatterHackers.Csg.Face;

namespace CSharpCAD
{
    /// <summary>
    /// Interactive Cheat Sheet and Reference Window for OpenSharpCAD.
    /// Features category tabs with formatted documentation, syntax-highlighted code boxes,
    /// and embedded 3D viewports below every section.
    /// </summary>
    public class HelpWindow : SystemWindow
    {
        private readonly Action<string, bool> _onInsertCode;
        private readonly Action _onClose;

        private FlowLayoutWidget _contentFlow;
        private ScrollableWidget _scrollableContent;
        private readonly List<StyledButton> _tabButtons = new();
        private readonly Dictionary<string, List<(Mesh mesh, Color color)>> _itemMeshCache = new();
        private int _currentTabIndex = 0;

        // Visual Palette (Dracula-inspired dark theme matching editor)
        private static readonly Color DialogBg           = new Color(34, 36, 48);
        private static readonly Color HeaderBg           = new Color(26, 27, 38);
        private static readonly Color TabBarBg           = new Color(28, 30, 42);
        private static readonly Color CardBg             = new Color(42, 45, 60);
        private static readonly Color CodeBoxBg          = new Color(20, 21, 29);
        private static readonly Color ActiveTabBg        = new Color(98, 114, 164);
        private static readonly Color ActiveTabHoverBg   = new Color(120, 138, 190);
        private static readonly Color InactiveTabBg      = new Color(40, 42, 54);
        private static readonly Color InactiveTabHoverBg = new Color(55, 58, 75);
        private static readonly Color AccentGreen        = new Color(80, 250, 123);
        private static readonly Color AccentGreenHover   = new Color(120, 255, 160);
        private static readonly Color AccentCyan         = new Color(80, 200, 255);
        private static readonly Color AccentPink         = new Color(255, 121, 198);
        private static readonly Color AccentYellow       = new Color(241, 250, 140);
        private static readonly Color AccentPurple       = new Color(189, 147, 249);
        private static readonly Color TextWhite          = new Color(248, 248, 242);
        private static readonly Color TextMuted          = new Color(180, 185, 205);

        /// <summary>
        /// Custom high-contrast clickable button widget with explicit text color and hover states.
        /// </summary>
        public class StyledButton : GuiWidget
        {
            private readonly TextWidget _labelWidget;
            private Color _normalBg;
            private Color _hoverBg;

            public StyledButton(string text, Color textColor, Color normalBg, Color hoverBg, double pointSize = 8.5, BorderDouble padding = default)
            {
                _normalBg = normalBg;
                _hoverBg = hoverBg;
                BackgroundColor = normalBg;
                Cursor = Cursors.Hand;
                Padding = (padding.Width == 0 && padding.Height == 0) ? new BorderDouble(8, 4) : padding;
                VAnchor = VAnchor.Center;

                _labelWidget = new TextWidget(text, pointSize: pointSize, textColor: textColor, bold: true, ellipsisIfClipped: false)
                {
                    VAnchor = VAnchor.Center,
                    HAnchor = HAnchor.Center,
                    AutoExpandBoundsToText = true,
                    Selectable = false,
                };
                AddChild(_labelWidget);
                SetBoundsToEncloseChildren();
            }

            public void SetText(string text)
            {
                _labelWidget.Text = text;
                SetBoundsToEncloseChildren();
                Invalidate();
            }

            public void SetColors(Color normalBg, Color hoverBg, Color? textColor = null)
            {
                _normalBg = normalBg;
                _hoverBg = hoverBg;
                BackgroundColor = normalBg;
                if (textColor.HasValue)
                {
                    _labelWidget.TextColor = textColor.Value;
                }
                Invalidate();
            }

            public override void OnMouseEnterBounds(MouseEventArgs mouseEvent)
            {
                BackgroundColor = _hoverBg;
                Invalidate();
                base.OnMouseEnterBounds(mouseEvent);
            }

            public override void OnMouseLeaveBounds(MouseEventArgs mouseEvent)
            {
                BackgroundColor = _normalBg;
                Invalidate();
                base.OnMouseLeaveBounds(mouseEvent);
            }
        }

        /// <summary>
        /// Live 3D OpenGL viewport embedded inside a section card, supporting trackball rotation, pan, zoom, and centering without auto-spinning.
        /// </summary>
        public class Card3DViewport : TrackballTumbleView
        {
            private readonly List<(Mesh mesh, Color color)> _meshes;
            private Vector3 _modelCenter = Vector3.Zero;
            private bool _centered = false;

            public Card3DViewport(WorldView world, List<(Mesh mesh, Color color)> meshes)
                : base(world, null)
            {
                _meshes = meshes ?? new();
                DrawContent = Render3DContent;
                TransformState = TrackBallTransformType.Translation;
            }

            public AxisAlignedBoundingBox GetBounds()
            {
                AxisAlignedBoundingBox total = null;
                for (int i = 0; i < _meshes.Count; i++)
                {
                    var m = _meshes[i].mesh;
                    if (m != null)
                    {
                        var b = m.GetAxisAlignedBoundingBox();
                        if (b != null && b.MinXYZ.X <= b.MaxXYZ.X)
                        {
                            if (total == null) total = new AxisAlignedBoundingBox(b.MinXYZ, b.MaxXYZ);
                            else total.ExpandToInclude(b);
                        }
                    }
                }
                return total;
            }

            public void CenterView()
            {
                RecalculateProjection();

                var bounds = GetBounds();
                _modelCenter = (bounds != null) ? bounds.Center : Vector3.Zero;
                Vector3 size = (bounds != null) ? bounds.Size : new Vector3(20, 20, 20);
                double maxDim = Math.Max(size.X, Math.Max(size.Y, size.Z));
                if (maxDim < 0.001 || double.IsNaN(maxDim)) maxDim = 20;

                ZeroVelocity();
                World.Reset();

                double vHeight = 2.0 * 7.0 * Math.Tan(MathHelper.DegreesToRadians(45.0) * 0.5);
                double w = Width > 0 ? Width : 480;
                double h = Height > 0 ? Height : 310;
                double aspect = w / h;
                double vWidth = vHeight * aspect;
                double targetExtent = Math.Min(vHeight, vWidth) * 0.65;
                double scale = targetExtent / maxDim;

                World.Scale = scale;
                World.Translate(-_modelCenter);
                World.RotationMatrix = Matrix4X4.CreateRotationX(MathHelper.DegreesToRadians(-60)) * Matrix4X4.CreateRotationZ(MathHelper.DegreesToRadians(30));
                _centered = true;
                Invalidate();
            }

            public void ResetCamera()
            {
                CenterView();
            }

            public void Zoom(double factor)
            {
                if (factor > 0 && !double.IsNaN(factor))
                {
                    World.Scale *= factor;
                    Invalidate();
                }
            }

            private void Render3DContent()
            {
                if (_meshes == null || _meshes.Count == 0) return;

                for (int i = 0; i < _meshes.Count; i++)
                {
                    var item = _meshes[i];
                    if (item.mesh != null)
                    {
                        GLHelper.Render(item.mesh, item.color);
                    }
                }
            }

            public new void RecalculateProjection()
            {
                double w = Width > 0 ? Width : 480;
                double h = Height > 0 ? Height : 310;

                double trackingRadius = Math.Max(10.0, Math.Min(w * 0.45, h * 0.45));
                TrackBallController.ScreenCenter = new Vector2(Math.Max(10.0, w / 2.0 - CenterOffsetX), Math.Max(10.0, h / 2.0));
                TrackBallController.TrackBallRadius = trackingRadius;

                var zNear = 0.1;
                var zFar = 100.0;
                GetNearFar?.Invoke(out zNear, out zFar);

                if (CenterOffsetX != 0)
                {
                    World.CalculatePerspectiveMatrixOffCenter(w, h, CenterOffsetX, zNear, zFar);
                }
                else
                {
                    World.CalculatePerspectiveMatrix(w, h, zNear, zFar);
                }
            }

            public override void OnBoundsChanged(EventArgs e)
            {
                base.OnBoundsChanged(e);
                if (Width > 10 && Height > 10)
                {
                    RecalculateProjection();
                    if (!_centered)
                    {
                        CenterView();
                    }
                }
            }

            public override void OnMouseDown(MouseEventArgs mouseEvent)
            {
                RecalculateProjection();

                if (mouseEvent.Button == MouseButtons.Left)
                {
                    if (Keyboard.IsKeyDown(Keys.ShiftKey))
                    {
                        TransformState = TrackBallTransformType.Rotation;
                    }
                    else
                    {
                        TransformState = TrackBallTransformType.Translation;
                    }
                }
                else if (mouseEvent.Button == MouseButtons.Right)
                {
                    TransformState = TrackBallTransformType.Rotation;
                }
                else if (mouseEvent.Button == MouseButtons.Middle)
                {
                    TransformState = TrackBallTransformType.Translation;
                }

                base.OnMouseDown(mouseEvent);

                if (MouseCaptured)
                {
                    Vector2 pos = new Vector2(mouseEvent.X, mouseEvent.Y);
                    TrackBallController.OnMouseDown(pos, Matrix4X4.Identity, TransformState);
                }
            }

            public override void OnMouseMove(MouseEventArgs mouseEvent)
            {
                if (Width <= 0 || Height <= 0) return;
                RecalculateProjection();

                base.OnMouseMove(mouseEvent);

                // Guard against matrix corruption
                if (!World.TranslationMatrix.IsValid() || !World.RotationMatrix.IsValid())
                {
                    CenterView();
                }
            }

            public override void OnDraw(Graphics2D graphics2D)
            {
                if (Width <= 0 || Height <= 0) return;

                RecalculateProjection();

                RectangleDouble clip = graphics2D.GetClippingRect();
                RectangleDouble screenRect = this.TransformToScreenSpace(LocalBounds);

                // If completely outside the visible scroll window, skip drawing entirely
                if (screenRect.Right <= clip.Left || screenRect.Left >= clip.Right ||
                    screenRect.Top <= clip.Bottom || screenRect.Bottom >= clip.Top)
                {
                    return;
                }

                // Dark background & subtle pedestal rings
                graphics2D.FillRectangle(LocalBounds, new Color(16, 18, 26));

                double cx = Width / 2;
                double cy = Math.Max(25, Height * 0.38);
                graphics2D.Circle(new Vector2(cx, cy), Math.Min(Width, Height) * 0.36, new Color(42, 48, 68, 80));
                graphics2D.Circle(new Vector2(cx, cy), Math.Min(Width, Height) * 0.22, new Color(52, 58, 82, 100));

                if (DrawContent != null)
                {
                    if (!World.TranslationMatrix.IsValid() || !World.RotationMatrix.IsValid())
                    {
                        CenterView();
                    }

                    double left = Math.Max(screenRect.Left, clip.Left);
                    double bottom = Math.Max(screenRect.Bottom, clip.Bottom);
                    double right = Math.Min(screenRect.Right, clip.Right);
                    double top = Math.Min(screenRect.Top, clip.Top);
                    if (right <= left || top <= bottom) return;
                    RectangleDouble visibleRect = new RectangleDouble(left, bottom, right, top);

                    double devScale = GuiWidget.DeviceScale;
                    RectangleDouble glRect = new RectangleDouble(
                        screenRect.Left * devScale,
                        screenRect.Bottom * devScale,
                        screenRect.Right * devScale,
                        screenRect.Top * devScale
                    );

                    RectangleDouble scissorRect = new RectangleDouble(
                        visibleRect.Left * devScale,
                        visibleRect.Bottom * devScale,
                        visibleRect.Right * devScale,
                        visibleRect.Top * devScale
                    );

                    GLHelper.SetGlContext(this.World, glRect, new LightingData()
                    {
                        AmbientLight = new float[] { 0.2f, 0.2f, 0.2f, 1.0f }
                    });

                    GL.Scissor((int)scissorRect.Left, (int)scissorRect.Bottom, (int)scissorRect.Width, (int)scissorRect.Height);
                    GL.Enable(EnableCap.ScissorTest);

                    DrawContent();

                    GLHelper.UnsetGlContext();
                }
            }
        }

        private record HelpItem(
            string Title,
            string Badge,
            Color BadgeColor,
            string Description,
            string Snippet,
            Func<List<(Mesh mesh, Color color)>> BuildMeshes = null,
            bool IsFullExample = false);

        private GuiWidget _dialogCard;

        public GuiWidget DialogCard => _dialogCard ?? this;

        public HelpWindow(Action<string, bool> onInsertCode = null, Action onClose = null)
            : base(1240, 760)
        {
            _onInsertCode = onInsertCode;
            _onClose      = onClose;

            Title = "OpenSharpCAD — Cheat Sheet & Reference (F1)";
            MinimumSize = new Vector2(850, 520);
            UseOpenGL = true;
            BackgroundColor = DialogBg;

            BuildUi();
        }

        public override void OnKeyDown(KeyEventArgs keyEvent)
        {
            if (keyEvent.KeyCode == Keys.F1)
            {
                _onClose?.Invoke();
                CloseOnIdle();
                keyEvent.Handled = true;
                return;
            }
            base.OnKeyDown(keyEvent);
        }

        public override void OnClosed(EventArgs e)
        {
            _onClose?.Invoke();
            base.OnClosed(e);
        }

        public void ToggleMaximize()
        {
            Maximized = !Maximized;
        }

        public void EnsureWithinParent()
        {
        }

        private void BuildUi()
        {
            _dialogCard = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                BackgroundColor = DialogBg,
            };
            AddChild(_dialogCard);

            // 1. Top Header Bar
            var header = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 34,
                BackgroundColor = HeaderBg,
                Padding = new BorderDouble(12, 3),
            };

            var titleText = new TextWidget("OpenSharpCAD — Interactive Cheat Sheet & Reference", pointSize: 10, textColor: TextWhite, bold: true, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                AutoExpandBoundsToText = true,
            };
            header.AddChild(titleText);

            header.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

            var hintText = new TextWidget("Press F1 to close", pointSize: 7.5, textColor: TextMuted, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 10, 0),
                AutoExpandBoundsToText = true,
            };
            header.AddChild(hintText);

            var closeButton = new StyledButton("X", TextWhite, new Color(68, 71, 90, 180), new Color(255, 85, 85), pointSize: 8, padding: new BorderDouble(8, 3))
            {
                VAnchor = VAnchor.Center,
            };
            closeButton.Click += (s, e) =>
            {
                _onClose?.Invoke();
                CloseOnIdle();
            };
            header.AddChild(closeButton);

            _dialogCard.AddChild(header);

            // 2. Navigation Category Tabs Bar
            var tabBar = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 30,
                BackgroundColor = TabBarBg,
                Padding = new BorderDouble(6, 2),
            };

            string[] tabNames = new[]
            {
                "3D Shapes",
                "2D Primitives",
                "2D => 3D",
                "Transforms",
                "Booleans",
                "Colors",
                "Math & Helpers",
                "Examples",
                "API Reference"
            };

            for (int i = 0; i < tabNames.Length; i++)
            {
                int tabIndex = i;
                bool isActive = (i == 0);
                var btn = new StyledButton(
                    tabNames[i],
                    textColor: isActive ? TextWhite : TextMuted,
                    normalBg: isActive ? ActiveTabBg : InactiveTabBg,
                    hoverBg: isActive ? ActiveTabHoverBg : InactiveTabHoverBg,
                    pointSize: 8,
                    padding: new BorderDouble(8, 3)
                )
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(2, 0),
                };
                btn.Click += (s, e) => SelectTab(tabIndex);
                _tabButtons.Add(btn);
                tabBar.AddChild(btn);
            }

            tabBar.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

            var exportDocBtn = new StyledButton("Export MD", TextWhite, new Color(50, 54, 72), new Color(98, 114, 164), pointSize: 7.5, padding: new BorderDouble(6, 3))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(4, 0, 0, 0),
            };
            exportDocBtn.Click += (s, e) =>
            {
                try
                {
                    AutoDocumentationService.ExportMarkdownFile("API_DOCUMENTATION.md");
                    exportDocBtn.SetText("Exported!");
                }
                catch
                {
                    exportDocBtn.SetText("Export Error");
                }
            };
            tabBar.AddChild(exportDocBtn);

            _dialogCard.AddChild(tabBar);

            // 3. Main Scrollable Content Body
            _scrollableContent = new ScrollableWidget(autoScroll: true)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                Padding = new BorderDouble(12),
            };
            _scrollableContent.ScrollArea.HAnchor = HAnchor.Stretch;

            _contentFlow = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                Width = 1180,
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
            };
            _scrollableContent.BoundsChanged += (s, e) =>
            {
                if (_scrollableContent.Width > 60)
                {
                    _contentFlow.Width = Math.Max(700, _scrollableContent.Width - 28);
                }
            };
            _scrollableContent.AddChild(_contentFlow);
            _dialogCard.AddChild(_scrollableContent);

            // Initialize on Tab 0 (3D Shapes)
            SelectTab(0);
        }

        private void SelectTab(int index)
        {
            _currentTabIndex = index;
            for (int i = 0; i < _tabButtons.Count; i++)
            {
                bool isActive = (i == index);
                _tabButtons[i].SetColors(
                    normalBg: isActive ? ActiveTabBg : InactiveTabBg,
                    hoverBg: isActive ? ActiveTabHoverBg : InactiveTabHoverBg,
                    textColor: isActive ? TextWhite : TextMuted
                );
            }

            _contentFlow.RemoveChildren();

            var items = GetItemsForTab(index);
            foreach (var item in items)
            {
                _contentFlow.AddChild(CreateItemCard(item));
            }

            _scrollableContent.TopLeftOffset = Vector2.Zero;
        }

        public static void CopyToClipboard(string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            try
            {
                Clipboard.Instance?.SetText(text);
            }
            catch { }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                try
                {
                    var psi = new ProcessStartInfo("pbcopy")
                    {
                        RedirectStandardInput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    using var p = Process.Start(psi);
                    if (p != null)
                    {
                        using (var sw = p.StandardInput)
                        {
                            sw.Write(text);
                        }
                        p.WaitForExit(500);
                    }
                }
                catch { }
            }
        }

        private GuiWidget CreateItemCard(HelpItem item)
        {
            var card = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
                BackgroundColor = CardBg,
                Margin = new BorderDouble(0, 4, 0, 10),
                Padding = new BorderDouble(12, 10),
            };

            // Top row: Title + Badge + Copy Button + Insert Button
            var topRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 26,
            };

            var titleLabel = new TextWidget(item.Title, pointSize: 10, textColor: TextWhite, bold: true, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                AutoExpandBoundsToText = true,
            };
            topRow.AddChild(titleLabel);

            var badge = new GuiWidget(0, 16)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(8, 0, 0, 0),
                Padding = new BorderDouble(6, 2),
                BackgroundColor = new Color(item.BadgeColor, 40),
            };
            badge.AddChild(new TextWidget(item.Badge, pointSize: 7.5, textColor: item.BadgeColor, bold: true, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                HAnchor = HAnchor.Center,
                AutoExpandBoundsToText = true,
            });
            badge.SetBoundsToEncloseChildren();
            topRow.AddChild(badge);

            topRow.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

            var copyBtn = new StyledButton("Copy Code", TextWhite, new Color(68, 71, 90), new Color(98, 114, 164), pointSize: 7.5, padding: new BorderDouble(8, 3))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 5, 0),
            };
            copyBtn.Click += (s, e) =>
            {
                CopyToClipboard(item.Snippet);
                copyBtn.SetText("Copied!");
                copyBtn.SetColors(AccentGreen, AccentGreenHover, new Color(20, 24, 32));
                var timer = new System.Threading.Timer(_ =>
                {
                    UiThread.RunOnIdle(() =>
                    {
                        copyBtn.SetText("Copy Code");
                        copyBtn.SetColors(new Color(68, 71, 90), new Color(98, 114, 164), TextWhite);
                    });
                }, null, 1500, System.Threading.Timeout.Infinite);
            };
            topRow.AddChild(copyBtn);

            string btnText = item.IsFullExample ? "Load Example" : "Insert Snippet";
            var insertBtn = new StyledButton(btnText, new Color(20, 24, 32), AccentGreen, AccentGreenHover, pointSize: 7.5, padding: new BorderDouble(8, 3))
            {
                VAnchor = VAnchor.Center,
            };
            insertBtn.Click += (s, e) =>
            {
                CopyToClipboard(item.Snippet);
                _onInsertCode?.Invoke(item.Snippet, item.IsFullExample);
            };
            topRow.AddChild(insertBtn);

            card.AddChild(topRow);

            // Description
            var descFlow = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
                Margin = new BorderDouble(0, 4, 0, 4),
            };

            var descLines = item.Description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var dLine in descLines)
            {
                if (string.IsNullOrWhiteSpace(dLine))
                {
                    descFlow.AddChild(new GuiWidget(1, 3));
                    continue;
                }
                var dText = new TextWidget(dLine, pointSize: 7.5, textColor: TextMuted, ellipsisIfClipped: false)
                {
                    AutoExpandBoundsToText = true,
                    Margin = new BorderDouble(0, 1),
                };
                descFlow.AddChild(dText);
            }
            card.AddChild(descFlow);

            // 3D Viewport on LEFT, Code Snippet on RIGHT
            var viewportContainer = CreateSection3DView(item);
            var codeWidget = new ReadOnlyCodeBoxWidget(item.Snippet, fontSize: 8.0, maxDisplayHeight: 336);

            if (viewportContainer != null)
            {
                var sideBySideRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Fit,
                    Margin = new BorderDouble(0, 4, 0, 0),
                };

                viewportContainer.Margin = new BorderDouble(0, 0, 10, 0);
                viewportContainer.VAnchor = VAnchor.Top;
                sideBySideRow.AddChild(viewportContainer);

                codeWidget.HAnchor = HAnchor.Stretch;
                codeWidget.VAnchor = VAnchor.Top;
                codeWidget.Height = 336;
                if (codeWidget.Editor != null)
                {
                    codeWidget.Editor.Height = 336;
                }
                codeWidget.Margin = new BorderDouble(0);
                sideBySideRow.AddChild(codeWidget);

                card.AddChild(sideBySideRow);
            }
            else
            {
                // Full width code snippet if no 3D preview
                codeWidget.HAnchor = HAnchor.Stretch;
                card.AddChild(codeWidget);
            }

            return card;
        }

        private GuiWidget CreateSection3DView(HelpItem item)
        {
            if (!_itemMeshCache.TryGetValue(item.Title, out var meshes))
            {
                if (item.BuildMeshes != null)
                {
                    try
                    {
                        meshes = item.BuildMeshes();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error building mesh for " + item.Title + ": " + ex.Message);
                        meshes = new();
                    }
                }
                else
                {
                    meshes = new();
                }
                _itemMeshCache[item.Title] = meshes;
            }

            if (meshes == null || meshes.Count == 0)
            {
                return null;
            }

            // Contained 3D viewport container (on LEFT)
            var container = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Absolute,
                VAnchor = VAnchor.Absolute,
                Width = 480,
                Height = 336,
                Margin = new BorderDouble(0),
                BackgroundColor = CodeBoxBg,
            };

            // HUD Controls Bar
            var hud = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 26,
                BackgroundColor = new Color(24, 26, 36),
                Padding = new BorderDouble(6, 2),
            };

            var world = new WorldView(480, 310);
            var viewport = new Card3DViewport(world, meshes)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
            };

            var homeBtn = new StyledButton("🏠 Home", TextWhite, new Color(68, 71, 90), new Color(98, 114, 164), pointSize: 7.5, padding: new BorderDouble(6, 3))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 4, 0),
            };
            homeBtn.Click += (s, e) => viewport.CenterView();
            hud.AddChild(homeBtn);

            var resetBtn = new StyledButton("🔄 Reset", TextWhite, new Color(50, 54, 72), new Color(70, 76, 100), pointSize: 7.5, padding: new BorderDouble(6, 3))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 4, 0),
            };
            resetBtn.Click += (s, e) => viewport.ResetCamera();
            hud.AddChild(resetBtn);

            var zoomInBtn = new StyledButton("+", TextWhite, new Color(50, 54, 72), new Color(70, 76, 100), pointSize: 8, padding: new BorderDouble(6, 3))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 3, 0),
            };
            zoomInBtn.Click += (s, e) => viewport.Zoom(1.2);
            hud.AddChild(zoomInBtn);

            var zoomOutBtn = new StyledButton("-", TextWhite, new Color(50, 54, 72), new Color(70, 76, 100), pointSize: 8, padding: new BorderDouble(6, 3))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 5, 0),
            };
            zoomOutBtn.Click += (s, e) => viewport.Zoom(1.0 / 1.2);
            hud.AddChild(zoomOutBtn);

            hud.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

            var hintLabel = new TextWidget("🖱 Left-drag: Pan • Right-drag: Rotate • Wheel: Zoom", pointSize: 7.5, textColor: TextMuted, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                AutoExpandBoundsToText = true,
            };
            hud.AddChild(hintLabel);

            container.AddChild(hud);
            container.AddChild(viewport);

            return container;
        }

        private List<HelpItem> GetItemsForTab(int tabIndex) => tabIndex switch
        {
            0 => Get3DShapeItems(),
            1 => Get2DPrimitiveItems(),
            2 => Get2Dto3DItems(),
            3 => GetTransformItems(),
            4 => GetBooleanItems(),
            5 => GetColorItems(),
            6 => GetMathItems(),
            7 => GetExampleItems(),
            8 => GetApiReferenceItems(),
            _ => new List<HelpItem>()
        };

        private List<HelpItem> GetApiReferenceItems()
        {
            var docItems = AutoDocumentationService.GetDocumentedItems();
            var list = new List<HelpItem>();
            foreach (var doc in docItems)
            {
                var sbDesc = new StringBuilder();
                sbDesc.AppendLine(doc.Summary);
                if (doc.Constructors.Count > 0)
                {
                    sbDesc.AppendLine();
                    sbDesc.AppendLine("Constructors & Signatures:");
                    foreach (var c in doc.Constructors) sbDesc.AppendLine(" • " + c);
                }
                if (doc.Properties.Count > 0)
                {
                    sbDesc.AppendLine();
                    sbDesc.AppendLine("Properties:");
                    foreach (var p in doc.Properties) sbDesc.AppendLine(" • " + p);
                }
                if (doc.Methods.Count > 0)
                {
                    sbDesc.AppendLine();
                    sbDesc.AppendLine("Methods:");
                    foreach (var m in doc.Methods) sbDesc.AppendLine(" • " + m);
                }

                Color badgeColor = doc.Category switch
                {
                    "3D Solid" => AccentCyan,
                    "2D Primitive" => AccentGreen,
                    "Transformation" => AccentPurple,
                    "CSG Boolean" => AccentPink,
                    "Script Global" => AccentYellow,
                    _ => AccentCyan
                };

                list.Add(new HelpItem(
                    doc.Name,
                    doc.Category,
                    badgeColor,
                    sbDesc.ToString().TrimEnd(),
                    doc.ExampleSnippet,
                    doc.BuildMeshes
                ));
            }
            return list;
        }

        private List<HelpItem> Get3DShapeItems() => new()
        {
            new HelpItem(
                "Box (Cube & Rectangular Solid)",
                "Solid Primitive",
                AccentCyan,
                "Creates a rectangular solid or cube centered at origin.\nParameters: width (X), depth (Y), height (Z), name = \"\", createCentered = true\nExposed Properties:\n• Width, Depth, Height, Size (Max - Min)\n• Center, Min, Max, Bounds\n• TopCenter, BottomCenter, LeftCenter, RightCenter, FrontCenter, BackCenter\n• Corners (8 Vector3 corners), BoundingEdges (12 segment pairs)\nMethods: GetCorners(), GetBoundingEdges(), GetUniqueVertices(), GetEdges(), ToMesh()",
                "// Box(width, depth, height)\nvar box = new Box(24, 18, 12);\n// Access dimensions and anchor points\nVector3 top = box.TopCenter;\nVector3 dim = box.Size;\nDraw(box, RGB(80, 180, 255));",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Box(24, 18, 12)), new Color(80, 180, 255)) }),

            new HelpItem(
                "Box Corners & Points (Anchor Joints)",
                "Points & Anchoring",
                AccentCyan,
                "Extract all 8 corners of a cube/box to spawn corner joint spheres or anchor secondary parts parametrically.\nMethods: cube.Corners, cube.GetCorners(), cube.GetUniqueVertices()\nAnchor Points: cube.TopCenter, cube.BottomCenter, cube.LeftCenter, cube.RightCenter",
                "// 1. Create a base cube\nvar cube = new Box(24, 24, 24);\n\n// 2. Spawn corner node spheres at all 8 corner points of the cube\nCsgObject joints = null;\nforeach (var pt in cube.GetUniqueVertices())\n{\n    var node = new Sphere(radius: 3.5).Translate(pt);\n    joints = (joints == null) ? node : joints + node;\n}\n\n// 3. Mount a cylinder tower on TopCenter\nvar tower = new Cylinder(radius: 6, height: 16).Translate(cube.TopCenter);\n\nDraw(cube, RGB(60, 120, 220));\nDraw(joints, Color.Orange);\nDraw(tower, Color.Green);",
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var cube = new Box(24, 24, 24);
                    list.Add((CsgToMesh.Convert(cube), new Color(60, 120, 220)));
                    CsgObject joints = null;
                    foreach (var pt in cube.GetUniqueVertices())
                    {
                        var node = new Sphere(3.5).Translate(pt);
                        joints = (joints == null) ? node : joints + node;
                    }
                    if (joints != null) list.Add((CsgToMesh.Convert(joints), Color.Orange));
                    var tower = new Cylinder(radius: 6, height: 16).Translate(cube.TopCenter);
                    list.Add((CsgToMesh.Convert(tower), Color.Green));
                    return list;
                }),

            new HelpItem(
                "Box Edges to Struts (Wireframe & Cage)",
                "Edges to Objects",
                AccentCyan,
                "Extract all 12 bounding edges from a box as (Start, End) coordinate pairs and bridge them with cylinders to create wireframes or structural cages.\nMethods: cube.BoundingEdges, cube.GetBoundingEdges(), cube.GetEdges()\nHelper: CylinderBetween(start, end, radius) or Cylinder.CreateBetween(start, end, radius, sides)",
                "// Extract all 12 bounding edges of a cube to construct a wireframe strut cage\nvar cube = new Box(28, 28, 28);\nCsgObject cage = null;\n\nforeach (var (start, end) in cube.GetBoundingEdges())\n{\n    var strut = CylinderBetween(start, end, radius: 1.2);\n    cage = (cage == null) ? strut : cage + strut;\n}\n\n// Add a glowing core inside\nvar core = new Sphere(12);\n\nDraw(cage, Color.Yellow);\nDraw(core, RGB(255, 80, 120));",
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var cube = new Box(28, 28, 28);
                    CsgObject cage = null;
                    foreach (var (start, end) in cube.GetBoundingEdges())
                    {
                        var strut = Cylinder.CreateBetween(start, end, 1.2);
                        cage = (cage == null) ? strut : cage + strut;
                    }
                    var core = new Sphere(12);
                    if (cage != null) list.Add((CsgToMesh.Convert(cage), Color.Yellow));
                    list.Add((CsgToMesh.Convert(core), new Color(255, 80, 120)));
                    return list;
                }),

            new HelpItem(
                "LINQ Edge & Point Filtering",
                "LINQ & Filtering",
                AccentCyan,
                "Query and filter 3D geometry points and edges using standard C# LINQ (.Where, .Select, .SelectMany, .Distinct, .OrderBy).\nFilter edges to target specific faces (e.g. right side X > 0, top Z > 0), extract endpoints, and attach spheres or struts.",
                "var cube = new Box(28, 28, 28);\n\n// LINQ: filter only right-side points (X > 0) from edges\nvar rightPoints = cube.GetBoundingEdges()\n    .SelectMany(e => new[] { e.Start, e.End })\n    .Where(p => p.X > 0)\n    .Distinct();\n\nforeach (var pt in rightPoints)\n{\n    Draw(new Sphere(radius: 4.2).Translate(pt), Color.Red);\n}\n\n// LINQ: filter only right-side edges and bridge with struts\nvar rightEdges = cube.GetBoundingEdges()\n    .Where(e => e.Start.X > 0 && e.End.X > 0);\n\nforeach (var (start, end) in rightEdges)\n{\n    Draw(CylinderBetween(start, end, radius: 1.5), Color.Yellow);\n}\n\nDraw(cube, RGB(60, 80, 130));",
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var cube = new Box(28, 28, 28);
                    list.Add((CsgToMesh.Convert(cube), new Color(60, 80, 130)));
                    var rightPoints = cube.GetBoundingEdges().SelectMany(e => new[] { e.Start, e.End }).Where(p => p.X > 0).Distinct();
                    foreach (var pt in rightPoints)
                    {
                        list.Add((CsgToMesh.Convert(new Sphere(4.2).Translate(pt)), Color.Red));
                    }
                    var rightEdges = cube.GetBoundingEdges().Where(e => e.Start.X > 0 && e.End.X > 0);
                    foreach (var (start, end) in rightEdges)
                    {
                        list.Add((CsgToMesh.Convert(Cylinder.CreateBetween(start, end, 1.5)), new Color(245, 215, 75)));
                    }
                    return list;
                }),

            new HelpItem(
                "CsgObject Universal Inspection & Points",
                "Universal API",
                AccentCyan,
                "All 3D shapes (Box, Cylinder, Sphere, Torus, Extrude, and any CSG boolean result) expose universal inspection properties and point/edge accessors:\n• Center, Min, Max, Size, XSize, YSize, ZSize\n• TopCenter, BottomCenter, LeftCenter, RightCenter, FrontCenter, BackCenter\n• GetCorners() -> Vector3[8], GetBoundingEdges() -> (Vector3 Start, Vector3 End)[12]\n• GetVertices() -> List<Vector3>, GetUniqueVertices(tolerance) -> List<Vector3>\n• GetEdges(tolerance) -> List<(Vector3 Start, Vector3 End)>\n• Fluent transforms: .Translate(x,y,z), .RotateDegrees(x,y,z), .Scale(s)",
                "var baseShape = new Box(30, 20, 15);\n\n// Inspect geometry\nVector3 center = baseShape.Center;\nVector3 top    = baseShape.TopCenter;\nVector3 dims   = baseShape.Size;\n\n// Mount a cylinder exactly flush on top of the base shape\nvar turret = new Cylinder(radius: 5, height: 12)\n    .Translate(baseShape.TopCenter);\n\nDraw(baseShape, RGB(80, 140, 240));\nDraw(turret, RGB(255, 180, 60));",
                BuildMeshes: () =>
                {
                    var baseShape = new Box(30, 20, 15);
                    var turret = new Cylinder(radius: 5, height: 12).Translate(baseShape.TopCenter);
                    return new() {
                        (CsgToMesh.Convert(baseShape), new Color(80, 140, 240)),
                        (CsgToMesh.Convert(turret), new Color(255, 180, 60))
                    };
                }),

            new HelpItem(
                "Cylinder",
                "Solid Primitive",
                AccentCyan,
                "Creates a cylindrical solid centered along the Z axis (or X/Y via Alignment).\nParameters: radius, height, sides = 30, alignment = Alignment.z\nExposed Properties: Radius, Radius1, Radius2, Height, Alignment, Center, TopCenter, BottomCenter, Min, Max\nHelper Method: CylinderBetween(startPoint, endPoint, radius, sides = 16)",
                "// Cylinder(radius, height, sides = 30)\nvar cyl = new Cylinder(radius: 10, height: 28, sides: 36);\nDraw(cyl, Color.Orange);",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Cylinder(10, 28, 36)), Color.Orange) }),

            new HelpItem(
                "Cone / Tapered Cylinder",
                "Solid Primitive",
                AccentCyan,
                "Creates a cone or tapered cylinder with different top and bottom radii.\nParameters: radius1 (bottom), radius2 (top, 0 for sharp cone), height, sides = 30, alignment\nExposed Properties: Radius1, Radius2, Height, Alignment, TopCenter, BottomCenter, Center",
                "// Cylinder(radius1, radius2, height, sides)\nvar cone = new Cylinder(radius1: 14, radius2: 0, height: 25, sides: 36);\nDraw(cone, Color.Yellow);",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Cylinder(14, 0, 25, 36)), Color.Yellow) }),

            new HelpItem(
                "Sphere",
                "Solid Primitive",
                AccentCyan,
                "Creates a UV sphere centered at (0, 0, 0).\nParameters: radius, name = \"\"\nExposed Properties: Radius, Center, Min, Max, Bounds\nFluent Chaining: new Sphere(5).Translate(cube.TopCenter)",
                "// Sphere(radius)\nvar sphere = new Sphere(radius: 12);\nDraw(sphere, Color.Cyan);",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Sphere(12)), Color.Cyan) }),

            new HelpItem(
                "Torus (Donut)",
                "Solid Primitive",
                AccentCyan,
                "Creates a torus ring centered at origin with inner and outer radii.\nParameters: innerRadius, outerRadius, alignment = Alignment.z\nExposed Properties: InnerRadius, OuterRadius, Alignment, Center, Min, Max, Bounds",
                "// Torus(innerRadius, outerRadius)\nvar donut = new Torus(innerRadius: 10, outerRadius: 22);\nDraw(donut, Color.Violet);",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Torus(10, 22)), Color.Violet) }),

            new HelpItem(
                "Fluent Transform Chaining",
                "Method Chaining",
                AccentCyan,
                "Call transformation methods directly on ANY 3D solid without wrapping in new Translate(...):\n• shape.Translate(x, y, z) or shape.Translate(Vector3)\n• shape.Rotate(rx, ry, rz) (angles in radians)\n• shape.RotateDegrees(degX, degY, degZ) (angles in degrees)\n• shape.Scale(uniformScale) or shape.Scale(sx, sy, sz)",
                "var shape = new Box(12, 12, 12)\n    .RotateDegrees(0, 0, 45)\n    .Translate(10, 0, 5)\n    .Scale(1.2);\nDraw(shape, Color.Cyan);",
                BuildMeshes: () =>
                {
                    var shape = new Box(12, 12, 12).RotateDegrees(0, 0, 45).Translate(10, 0, 5).Scale(1.2);
                    return new() { (CsgToMesh.Convert(shape), Color.Cyan) };
                })
        };

        private List<HelpItem> Get2DPrimitiveItems() => new()
        {
            new HelpItem(
                "Point & Point3D (Coordinates)",
                "2D / 3D Primitive",
                AccentGreen,
                "Represents a 2D or 3D coordinate point (X, Y, Z).\nSeamlessly compatible with Vector2, Vector3, tuples (x, y) / (x, y, z), Dot, and Line.\nTwo points can create a Line: new Line(p1, p2) or p1.LineTo(p2).\nMethods: LineTo, DistanceTo, ToCircle(r), ToSphere(r)",
                "// 1. Create 2 points (2D or 3D)\nvar p1 = new Point(0, 0, -20);\nvar p2 = new Point(0, 0, 20);\n\n// 2. Connect into a line\nvar line = new Line(p1, p2);\n\n// 3. Extract line.Height property directly into Cylinder!\nvar cyl = new Cylinder(radius: 8, height: line.Height);\n\nDraw(cyl, RGB(80, 180, 255));\nDraw(p1, Color.Red);\nDraw(p2, Color.Green);",
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var p1 = new Point(0, 0, -20);
                    var p2 = new Point(0, 0, 20);
                    var line = new Line(p1, p2);
                    var cyl = new Cylinder(radius: 8, height: line.Height);
                    list.Add((CsgToMesh.Convert(cyl), new Color(80, 180, 255)));
                    list.Add((CsgToMesh.Convert(new Sphere(3.5).Translate(p1.Position3D)), Color.Red));
                    list.Add((CsgToMesh.Convert(new Sphere(3.5).Translate(p2.Position3D)), Color.Green));
                    return list;
                }),

            new HelpItem(
                "Line (2D / 3D Segment)",
                "2D / 3D Primitive",
                AccentGreen,
                "Represents a 1D line segment between two points.\nTwo points create a line: new Line(point1, point2) or (p1, p2).\nExposes line.Height (and Length, Dx, Dy, Dz) directly compatible with Cylinder height!\nMethods: ToCylinder(radius), ToThickRectangle(thickness), ToPath()",
                "// Create a line between two points\nvar p1 = new Point(0, 0, 0);\nvar p2 = new Point(0, 0, 35);\nvar line = new Line(p1, p2);\n\n// Pass line.Height directly to cylinder height\nvar pipe = new Cylinder(radius: 7, height: line.Height);\nDraw(pipe, RGB(255, 160, 50));",
                BuildMeshes: () =>
                {
                    var p1 = new Point(0, 0, 0);
                    var p2 = new Point(0, 0, 35);
                    var line = new Line(p1, p2);
                    var pipe = new Cylinder(radius: 7, height: line.Height);
                    return new() { (CsgToMesh.Convert(pipe), new Color(255, 160, 50)) };
                }),

            new HelpItem(
                "Path (2D Closed Polyline / Curve)",
                "2D Primitive",
                AccentGreen,
                "Creates a 2D path using MoveTo, LineTo, CurveTo, or Add(line).\nMUST be closed via .Close() before extruding or revolving! Validation will fail if path is left open.\nMethods: MoveTo, LineTo, CurveTo, Add, Close, Extrude, Revolve",
                "// Path: construct custom closed 2D polygon\nvar path = new Path();\npath.MoveTo(0, 0)\n    .LineTo(30, 0)\n    .LineTo(35, 15)\n    .LineTo(15, 25)\n    .LineTo(-5, 10)\n    .Close(); // Essential: must be closed!\n\nvar solid = path.Extrude(height: 15);\nDraw(solid, RGB(120, 240, 160));",
                BuildMeshes: () =>
                {
                    var path = new Path();
                    path.MoveTo(0, 0).LineTo(30, 0).LineTo(35, 15).LineTo(15, 25).LineTo(-5, 10).Close();
                    return new() { (CsgToMesh.Convert(path.Extrude(15)), new Color(120, 240, 160)) };
                }),

            new HelpItem(
                "Circle (2D)",
                "2D Primitive",
                AccentGreen,
                "Creates a 2D closed circle centered at origin (0, 0).\nParameters: radius, sides = 32\nMethods: Extrude(height), Revolve(angleDegrees, axisOffset), Loft, ToMesh()",
                "// Circle: 2D circular boundary\nvar circle = new Circle(radius: 16, sides: 36);\nvar cyl = circle.Extrude(height: 25);\nDraw(cyl, RGB(80, 180, 255));",
                BuildMeshes: () =>
                {
                    var circle = new Circle(16, 36);
                    return new() { (CsgToMesh.Convert(circle.Extrude(25)), new Color(80, 180, 255)) };
                }),

            new HelpItem(
                "Rectangle (2D)",
                "2D Primitive",
                AccentGreen,
                "Creates a 2D rectangle centered at origin (0, 0) with optional rounded corners.\nParameters: width, height, cornerRadius = 0, sides = 32\nMethods: Extrude(height), Revolve(angleDegrees), Loft, ToMesh()",
                "// Rectangle: with optional corner radius\nvar rect = new Rectangle(width: 40, height: 25, cornerRadius: 5);\nvar body = rect.Extrude(height: 12);\nDraw(body, RGB(255, 160, 80));",
                BuildMeshes: () =>
                {
                    var rect = new Rectangle(40, 25, cornerRadius: 5);
                    return new() { (CsgToMesh.Convert(rect.Extrude(12)), new Color(255, 160, 80)) };
                }),

            new HelpItem(
                "Square (2D)",
                "2D Primitive",
                AccentGreen,
                "Creates an equilateral 2D square centered at origin with optional rounded corners.\nParameters: size, cornerRadius = 0, sides = 32\nMethods: Extrude(height), Revolve(angleDegrees), Loft, ToMesh()",
                "// Square: 2D square primitive\nvar sq = new Square(size: 24, cornerRadius: 3);\nvar block = sq.Extrude(height: 24);\nDraw(block, RGB(220, 120, 240));",
                BuildMeshes: () =>
                {
                    var sq = new Square(24, cornerRadius: 3);
                    return new() { (CsgToMesh.Convert(sq.Extrude(24)), new Color(220, 120, 240)) };
                })
        };

        private List<HelpItem> Get2Dto3DItems() => new()
        {
            new HelpItem(
                "Extrude (Linear Extrusion)",
                "2D => 3D",
                AccentGreen,
                "Extrudes any closed 2D primitive (Circle, Rectangle, Square, closed Path) along the Z axis.\nParameters: height, centered = false, alignment = Alignment.z",
                "// Extrude: linear extrusion of 2D profile along Z\nvar star = new Path();\nstar.MoveTo(0, 15).LineTo(4, 5).LineTo(15, 5).LineTo(7, -2)\n    .LineTo(10, -12).LineTo(0, -6).LineTo(-10, -12).LineTo(-7, -2)\n    .LineTo(-15, 5).LineTo(-4, 5).Close();\n\nvar starSolid = star.Extrude(height: 10);\nDraw(starSolid, RGB(255, 215, 0));",
                BuildMeshes: () =>
                {
                    var star = new Path();
                    star.MoveTo(0, 15).LineTo(4, 5).LineTo(15, 5).LineTo(7, -2)
                        .LineTo(10, -12).LineTo(0, -6).LineTo(-10, -12).LineTo(-7, -2)
                        .LineTo(-15, 5).LineTo(-4, 5).Close();
                    return new() { (CsgToMesh.Convert(star.Extrude(10)), new Color(245, 215, 75)) };
                }),

            new HelpItem(
                "Revolve (Rotational Sweep / Lathe)",
                "2D => 3D",
                AccentGreen,
                "Revolves a closed 2D shape around an axis into a 3D solid (e.g. lathe, pulley, vase, donut).\nParameters: angleDegrees = 360, angleSteps = 30, axisOffset = 0, revolveAroundZ = true",
                "// Revolve: lathe a closed 2D shape around axis\nvar circle = new Circle(radius: 6, sides: 24);\n// Revolve with axisOffset creates a torus / donut\nvar ring = circle.Revolve(angleDegrees: 360, axisOffset: 20);\nDraw(ring, RGB(255, 100, 150));",
                BuildMeshes: () =>
                {
                    var circle = new Circle(radius: 6, sides: 24);
                    var ring = circle.Revolve(angleDegrees: 360, axisOffset: 20);
                    return new() { (CsgToMesh.Convert(ring), new Color(255, 100, 150)) };
                }),

            new HelpItem(
                "Loft (Cross-Section Skinning)",
                "2D => 3D",
                AccentGreen,
                "Creates a smooth transitional solid skin between two or more 2D cross sections.\nSupports transitioning between different shapes (e.g. Square to Circle duct adapter) with optional twist.\nParameters: bottom.Loft(top, height, twistDegrees, resolution, capped) or new Loft((s1, z1), (s2, z2)...)",
                "// Loft: Square base smoothly morphing into Circle top (HVAC duct transition)\nvar baseRect = new Rectangle(width: 30, height: 30, cornerRadius: 4);\nvar topCircle = new Circle(radius: 10);\n\nvar duct = baseRect.Loft(topCircle, height: 35, twistDegrees: 0);\nDraw(duct, RGB(100, 220, 240));",
                BuildMeshes: () =>
                {
                    var baseRect = new Rectangle(width: 30, height: 30, cornerRadius: 4);
                    var topCircle = new Circle(radius: 10);
                    var duct = baseRect.Loft(topCircle, height: 35, twistDegrees: 0);
                    return new() { (CsgToMesh.Convert(duct), new Color(100, 220, 240)) };
                }),

            new HelpItem(
                "TwistExtrude (Helical Extrusion)",
                "2D => 3D",
                AccentGreen,
                "Extrudes a closed 2D shape along Z while smoothly rotating around its centroid.\nSupports continuous twist angles and optional scale tapering.\nParameters: shape.TwistExtrude(height, twistDegrees, slices, scale)",
                "// TwistExtrude: twisted architectural pillar\nvar square = new Square(size: 20, cornerRadius: 2);\nvar column = square.TwistExtrude(height: 60, twistDegrees: 90, slices: 45, scale: 0.8);\nDraw(column, RGB(255, 170, 60));",
                BuildMeshes: () =>
                {
                    var square = new Square(size: 20, cornerRadius: 2);
                    var column = square.TwistExtrude(height: 60, twistDegrees: 90, slices: 45, scale: 0.8);
                    return new() { (CsgToMesh.Convert(column), new Color(255, 170, 60)) };
                }),

            new HelpItem(
                "LinearExtrude (Coordinate Polygon)",
                "2D => 3D",
                AccentGreen,
                "Extrudes a 2D polygon defined directly by raw [x0, y0, x1, y1...] coordinate pairs.\nParameters: double[] points, height, alignment = Alignment.z, twistRadians = 0",
                "// LinearExtrude: raw 2D coordinate array extrusion\ndouble[] pts = new double[] { -12, -12,  12, -12,  16, 12,  -16, 12 };\nvar prism = new LinearExtrude(pts, height: 25, twistRadians: MathHelper.DegreesToRadians(30));\nDraw(prism, RGB(120, 220, 140));",
                BuildMeshes: () =>
                {
                    double[] pts = new double[] { -12, -12, 12, -12, 16, 12, -16, 12 };
                    var prism = new LinearExtrude(pts, height: 25, twistRadians: MathHelper.DegreesToRadians(30));
                    return new() { (CsgToMesh.Convert(prism), new Color(120, 220, 140)) };
                })
        };

        private List<HelpItem> GetTransformItems() => new()
        {
            new HelpItem(
                "Translate",
                "Transformation",
                AccentPink,
                "Translates (moves) an object in 3D coordinate space by X, Y, Z offsets.\nParameters: object, x, y, z  OR  new Translate(object, new Vector3(x, y, z))",
                "var box = new Box(15, 15, 15);\nvar shifted = new Translate(box, x: 20, y: 0, z: 10);\nDraw(box, Color.Gray);\nDraw(shifted, Color.Red);",
                BuildMeshes: () =>
                {
                    var box = new Box(15, 15, 15);
                    var shifted = new Translate(box, x: 20, y: 0, z: 10);
                    return new() {
                        (CsgToMesh.Convert(box), Color.Gray),
                        (CsgToMesh.Convert(shifted), Color.Red)
                    };
                }),

            new HelpItem(
                "Rotate",
                "Transformation",
                AccentPink,
                "Rotates an object around X, Y, and/or Z axes. Angles must be in radians!\nUse MathHelper.DegreesToRadians(deg) for degrees.\nParameters: object, x: rx, y: ry, z: rz",
                "var box = new Box(10, 25, 10);\nvar rotated = new Rotate(box, z: MathHelper.DegreesToRadians(45));\nDraw(rotated, Color.Green);",
                BuildMeshes: () =>
                {
                    var box = new Box(10, 25, 10);
                    var rotated = new Rotate(box, z: MathHelper.DegreesToRadians(45));
                    return new() { (CsgToMesh.Convert(rotated), Color.Green) };
                }),

            new HelpItem(
                "Scale",
                "Transformation",
                AccentPink,
                "Scales an object non-uniformly or uniformly.\nParameters: object, scaleX, scaleY, scaleZ  OR  new Scale(object, uniformScale)",
                "var sphere = new Sphere(10);\nvar stretched = new Scale(sphere, 1.0, 1.0, 2.0);\nDraw(stretched, Color.Cyan);",
                BuildMeshes: () =>
                {
                    var sphere = new Sphere(10);
                    var stretched = new Scale(sphere, 1.0, 1.0, 2.0);
                    return new() { (CsgToMesh.Convert(stretched), Color.Cyan) };
                }),

            new HelpItem(
                "Align",
                "Transformation",
                AccentPink,
                "Snaps a bounding face of one object against a bounding face of another object.\nFaces: Face.Left, Face.Right, Face.Front, Face.Back, Face.Bottom, Face.Top\nParameters: objectToAlign, faceToAlign, targetObject, targetFace, offsetX, offsetY, offsetZ",
                "var table = new Box(40, 40, 4);\nvar pillar = new Cylinder(6, 20, 24);\nvar onTop = new Align(pillar, Face.Bottom, table, Face.Top);\nDraw(table + onTop, Color.White);",
                BuildMeshes: () =>
                {
                    var table = new Box(40, 40, 4);
                    var pillar = new Cylinder(6, 20, 24);
                    var onTop = new Align(pillar, Face.Bottom, table, Face.Top);
                    return new() { (CsgToMesh.Convert(table + onTop), Color.White) };
                }),

            new HelpItem(
                "SetCenter",
                "Transformation",
                AccentPink,
                "Translates the object so its bounding box center sits at the specified coordinates.\nParameters: object, x = 0, y = 0, z = 0, onX = true, onY = true, onZ = true",
                "var box = new Box(20, 30, 10);\nvar centered = new SetCenter(box, x: 0, y: 0, z: 0);\nDraw(centered, Color.Magenta);",
                BuildMeshes: () =>
                {
                    var box = new Box(20, 30, 10);
                    var centered = new SetCenter(box, x: 0, y: 0, z: 0);
                    return new() { (CsgToMesh.Convert(centered), Color.Magenta) };
                }),

            new HelpItem(
                "Mirror",
                "Transformation",
                AccentPink,
                "Creates a mirrored duplicate across a coordinate plane.\nMethods: object.NewMirrorAccrossX(offset), NewMirrorAccrossY(), NewMirrorAccrossZ()",
                "var wing = new Translate(new Box(25, 10, 2), 15, 0, 0);\nvar otherWing = wing.NewMirrorAccrossX();\nDraw(wing + otherWing, Color.Yellow);",
                BuildMeshes: () =>
                {
                    var wing = new Translate(new Box(25, 10, 2), 15, 0, 0);
                    var otherWing = wing.NewMirrorAccrossX();
                    return new() { (CsgToMesh.Convert(wing + otherWing), Color.Yellow) };
                })
        };

        private List<HelpItem> GetBooleanItems() => new()
        {
            new HelpItem(
                "Union (+ operator)",
                "Boolean CSG",
                AccentGreen,
                "Fuses multiple solids together into a single unified 3D volume.\nSyntax: shapeA + shapeB  OR  new Union(shapeA, shapeB)",
                "var arm1 = new Box(40, 8, 8);\nvar arm2 = new Box(8, 40, 8);\nvar mergedCross = arm1 + arm2;\nDraw(mergedCross, Color.Green);",
                BuildMeshes: () =>
                {
                    var arm1 = new Box(40, 8, 8);
                    var arm2 = new Box(8, 40, 8);
                    return new() { (CsgToMesh.Convert(arm1 + arm2), Color.Green) };
                }),

            new HelpItem(
                "Difference (- operator)",
                "Boolean CSG",
                AccentGreen,
                "Subtracts (cuts) the second shape from the first solid to carve holes and notches.\nSyntax: baseShape - cuttingShape  OR  new Difference(baseShape, cuttingShape)",
                "var solidBlock = new Box(30, 30, 20);\nvar hole = new Cylinder(7, 30, 24);\nvar boredBlock = solidBlock - hole;\nDraw(boredBlock, RGB(80, 180, 255));",
                BuildMeshes: () =>
                {
                    var solidBlock = new Box(30, 30, 20);
                    var hole = new Cylinder(7, 30, 24);
                    return new() { (CsgToMesh.Convert(solidBlock - hole), new Color(80, 180, 255)) };
                }),

            new HelpItem(
                "Intersection",
                "Boolean CSG",
                AccentGreen,
                "Calculates the overlapping volume shared by both solids.\nSyntax: new Intersection(shapeA, shapeB)",
                "var cube = new Box(22, 22, 22);\nvar ball = new Sphere(14);\nvar intersection = new Intersection(cube, ball);\nDraw(intersection, Color.Orange);",
                BuildMeshes: () =>
                {
                    var cube = new Box(22, 22, 22);
                    var ball = new Sphere(14);
                    return new() { (CsgToMesh.Convert(new Intersection(cube, ball)), Color.Orange) };
                }),

            new HelpItem(
                "Chained CSG Expressions",
                "Composite CSG",
                AccentGreen,
                "Combine operators in mathematical expressions with parentheses for complex designs.",
                "var plate = new Box(50, 50, 6);\nvar boss = new Cylinder(16, 12, 24);\nvar bore = new Cylinder(8, 25, 24);\nvar composite = (plate + boss) - bore;\nDraw(composite, RGB(100, 200, 255));",
                BuildMeshes: () =>
                {
                    var plate = new Box(50, 50, 6);
                    var boss = new Cylinder(16, 12, 24);
                    var bore = new Cylinder(8, 25, 24);
                    return new() { (CsgToMesh.Convert((plate + boss) - bore), new Color(100, 200, 255)) };
                })
        };

        private List<HelpItem> GetColorItems() => new()
        {
            new HelpItem(
                "Draw(shape)",
                "Rendering",
                AccentYellow,
                "Draws a 3D solid with the default neutral grey color Color(150, 150, 150).",
                "var box = new Box(20, 20, 20);\nDraw(box);",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Box(20, 20, 20)), new Color(150, 150, 150)) }),

            new HelpItem(
                "Draw(shape, color)",
                "Rendering",
                AccentYellow,
                "Draws a 3D solid with an explicit color parameter.",
                "Draw(new Sphere(12), Color.Red);\nDraw(new Translate(new Box(15, 15, 15), 25, 0, 0), Color.Cyan);",
                BuildMeshes: () => new() {
                    (CsgToMesh.Convert(new Sphere(12)), Color.Red),
                    (CsgToMesh.Convert(new Translate(new Box(15, 15, 15), 25, 0, 0)), Color.Cyan)
                }),

            new HelpItem(
                "RGB(r, g, b)",
                "Color Constructor",
                AccentYellow,
                "Constructs a color using standard 0-255 integer RGB channels.\nParameters: int red, int green, int blue (0 to 255)",
                "Draw(new Cylinder(10, 25, 32), RGB(255, 140, 20));",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Cylinder(10, 25, 32)), new Color(255, 140, 20)) }),

            new HelpItem(
                "RGBA(r, g, b, a)",
                "Color Constructor",
                AccentYellow,
                "Constructs a color with an alpha transparency channel (0 to 255).\nParameters: int red, int green, int blue, int alpha",
                "Draw(new Sphere(15), RGBA(80, 200, 255, 180));",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Sphere(15)), new Color(80, 200, 255, 180)) }),

            new HelpItem(
                "RGBf(r, g, b)",
                "Color Constructor",
                AccentYellow,
                "Constructs a color using normalized 0.0 to 1.0 floating point values.\nParameters: double red, double green, double blue (0.0 to 1.0)",
                "Draw(new Torus(10, 20), RGBf(0.8, 0.2, 0.9));",
                BuildMeshes: () => new() { (CsgToMesh.Convert(new Torus(10, 20)), new Color((int)(0.8 * 255), (int)(0.2 * 255), (int)(0.9 * 255))) }),

            new HelpItem(
                "Predefined Color Constants",
                "Built-in Colors",
                AccentYellow,
                "Predefined standard colors in the Color struct:\nColor.Red, Color.Green, Color.Blue, Color.Cyan, Color.Magenta, Color.Yellow,\nColor.White, Color.Black, Color.Orange, Color.Violet, Color.Indigo, Color.Pink",
                "Draw(new Translate(new Box(10, 10, 10), -20, 0, 0), Color.Red);\nDraw(new Box(10, 10, 10), Color.Green);\nDraw(new Translate(new Box(10, 10, 10), 20, 0, 0), Color.Blue);",
                BuildMeshes: () => new() {
                    (CsgToMesh.Convert(new Translate(new Box(10, 10, 10), -20, 0, 0)), Color.Red),
                    (CsgToMesh.Convert(new Box(10, 10, 10)), Color.Green),
                    (CsgToMesh.Convert(new Translate(new Box(10, 10, 10), 20, 0, 0)), Color.Blue)
                })
        };

        private List<HelpItem> GetMathItems() => new()
        {
            new HelpItem(
                "MathHelper.DegreesToRadians",
                "Angle Conversion",
                AccentPurple,
                "Converts degrees to radians for use with Rotate().\nExample: MathHelper.DegreesToRadians(90) -> PI/2",
                "double angleInRadians = MathHelper.DegreesToRadians(45);\nvar rotated = new Rotate(new Box(10, 10, 20), z: angleInRadians);\nDraw(rotated);",
                BuildMeshes: () =>
                {
                    double angleInRadians = MathHelper.DegreesToRadians(45);
                    var rotated = new Rotate(new Box(10, 10, 20), z: angleInRadians);
                    return new() { (CsgToMesh.Convert(rotated), Color.Cyan) };
                }),

            new HelpItem(
                "Constants: Tau & PI",
                "Math Constants",
                AccentPurple,
                "MathHelper.Tau is a full turn (2 * PI ≈ 6.28318).\nMath.PI is a half turn (≈ 3.14159).\nIdeal for circular loops: angle = i * MathHelper.Tau / count.",
                "int count = 6;\nfor (int i = 0; i < count; i++) {\n    double angle = i * MathHelper.Tau / count;\n    var p = new Translate(new Sphere(4), Math.Cos(angle) * 20, Math.Sin(angle) * 20, 0);\n    Draw(p, Color.Cyan);\n}",
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    int count = 6;
                    for (int i = 0; i < count; i++)
                    {
                        double angle = i * MathHelper.Tau / count;
                        var p = new Translate(new Sphere(4), Math.Cos(angle) * 20, Math.Sin(angle) * 20, 0);
                        list.Add((CsgToMesh.Convert(p), Color.Cyan));
                    }
                    return list;
                }),

            new HelpItem(
                "Standard C# Math Functions",
                "System.Math",
                AccentPurple,
                "Use standard C# math functions inside your scripts:\nMath.Sin(rad), Math.Cos(rad), Math.Sqrt(val), Math.Pow(b, e), Math.Abs(v), Math.Min(a, b), Math.Max(a, b)",
                "for (int i = -10; i <= 10; i++) {\n    double x = i * 2;\n    double z = Math.Sin(x * 0.3) * 8;\n    Draw(new Translate(new Sphere(1.5), x, 0, z), Color.Yellow);\n}",
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    for (int i = -10; i <= 10; i++)
                    {
                        double x = i * 2;
                        double z = Math.Sin(x * 0.3) * 8;
                        list.Add((CsgToMesh.Convert(new Translate(new Sphere(1.5), x, 0, z)), Color.Yellow));
                    }
                    return list;
                })
        };

        private List<HelpItem> GetExampleItems() => new()
        {
            new HelpItem(
                "Parametric Cube Joints & Struts",
                "Full Model",
                AccentGreen,
                "Extracts all 8 corner points of a cube for ball joint nodes, all 12 bounding edges for structural struts via CylinderBetween, and anchors parts to TopCenter and BottomCenter.",
                "// 1. Create a base cube\nvar cube = new Box(30, 30, 30);\n\n// 2. Extract unique corner points and attach spherical joint nodes\nCsgObject joints = null;\nforeach (var pt in cube.GetUniqueVertices())\n{\n    var node = new Sphere(radius: 4).Translate(pt);\n    joints = (joints == null) ? node : joints + node;\n}\n\n// 3. Extract all 12 bounding edges and bridge them with strut cylinders\nCsgObject struts = null;\nforeach (var (start, end) in cube.GetBoundingEdges())\n{\n    var strut = CylinderBetween(start, end, radius: 1.5);\n    struts = (struts == null) ? strut : struts + strut;\n}\n\n// 4. Parametrically anchor top mast and bottom base using TopCenter / BottomCenter\nvar mast = new Cylinder(radius: 4, height: 18).Translate(cube.TopCenter);\nvar basePlate = new Box(44, 44, 4).Translate(cube.BottomCenter - new Vector3(0, 0, 2));\n\n// 5. Draw entire assembly\nDraw(cube, RGB(50, 70, 110));\nDraw(struts, RGB(240, 220, 80));\nDraw(joints, RGB(255, 120, 40));\nDraw(mast, RGB(80, 250, 140));\nDraw(basePlate, RGB(180, 185, 200));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var cube = new Box(28, 28, 28);
                    list.Add((CsgToMesh.Convert(cube), new Color(55, 75, 120)));

                    CsgObject joints = null;
                    foreach (var pt in cube.GetUniqueVertices())
                    {
                        var node = new Sphere(3.8).Translate(pt);
                        joints = (joints == null) ? node : joints + node;
                    }
                    if (joints != null) list.Add((CsgToMesh.Convert(joints), new Color(255, 115, 45)));

                    CsgObject struts = null;
                    foreach (var (start, end) in cube.GetBoundingEdges())
                    {
                        var strut = Cylinder.CreateBetween(start, end, 1.4);
                        struts = (struts == null) ? strut : struts + strut;
                    }
                    if (struts != null) list.Add((CsgToMesh.Convert(struts), new Color(245, 215, 75)));

                    var mast = new Cylinder(radius: 3.5, height: 16).Translate(cube.TopCenter);
                    var basePlate = new Box(40, 40, 3.5).Translate(cube.BottomCenter - new Vector3(0, 0, 1.75));
                    list.Add((CsgToMesh.Convert(mast), new Color(80, 240, 140)));
                    list.Add((CsgToMesh.Convert(basePlate), new Color(170, 175, 190)));

                    return list;
                }),

            new HelpItem(
                "Twisted Architectural Tower",
                "Full Model",
                AccentGreen,
                "Demonstrates TwistExtrude on a rounded Square with 90° helical twist, smooth slices, and 0.75 taper scale factor, supported by a square pedestal and topped with a golden finial.",
                "var basePedestal = new Box(32, 32, 6);\nvar topCapital   = new Box(28, 28, 5).Translate(0, 0, 62);\nvar square = new Square(size: 22, cornerRadius: 2.5);\nvar twistedColumn = square.TwistExtrude(height: 56, twistDegrees: 90, slices: 40, scale: 0.75).Translate(0, 0, 6);\nvar finial = new Sphere(7).Translate(0, 0, 70);\n\nDraw(basePedestal, RGB(140, 145, 160));\nDraw(topCapital, RGB(160, 165, 180));\nDraw(twistedColumn, RGB(255, 165, 60));\nDraw(finial, RGB(255, 215, 0));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var basePedestal = new Box(32, 32, 6);
                    var topCapital = new Box(28, 28, 5).Translate(0, 0, 62);
                    var square = new Square(size: 22, cornerRadius: 2.5);
                    var twistedColumn = square.TwistExtrude(height: 56, twistDegrees: 90, slices: 40, scale: 0.75).Translate(0, 0, 6);
                    var finial = new Sphere(7).Translate(0, 0, 70);

                    list.Add((CsgToMesh.Convert(basePedestal), new Color(140, 145, 160)));
                    list.Add((CsgToMesh.Convert(topCapital), new Color(160, 165, 180)));
                    list.Add((CsgToMesh.Convert(twistedColumn), new Color(255, 165, 60)));
                    list.Add((CsgToMesh.Convert(finial), new Color(255, 215, 0)));
                    return list;
                }),

            new HelpItem(
                "Drilled Mounting Flange",
                "Full Model",
                AccentGreen,
                "Circular flange with a center hub, central through-bore, and 6 radial mounting bolt holes.",
                "// 1. Base Disk & Center Hub\nvar baseDisk = new Cylinder(radius: 35, height: 8, sides: 28);\nvar hub = new Cylinder(radius: 18, height: 22, sides: 20);\n\n// 2. Center Through-Bore\nvar centerBore = new Cylinder(radius: 10, height: 35, sides: 20);\n\n// 3. Circular array of 6 bolt holes\nCsgObject boltHoles = null;\nfor (int i = 0; i < 6; i++) {\n    double angle = i * MathHelper.Tau / 6;\n    var hole = new Translate(new Cylinder(3, 20, 16), Math.Cos(angle) * 26, Math.Sin(angle) * 26, 0);\n    boltHoles = (boltHoles == null) ? hole : boltHoles + hole;\n}\n\n// 4. CSG combine and draw\nvar flange = (baseDisk + hub) - (centerBore + boltHoles);\nDraw(flange, RGB(80, 180, 255));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var baseDisk = new Cylinder(radius: 32, height: 7, sides: 32);
                    var hub = new Cylinder(radius: 17, height: 20, sides: 28);
                    var centerBore = new Cylinder(radius: 9, height: 32, sides: 24);

                    CsgObject boltHoles = null;
                    for (int i = 0; i < 6; i++)
                    {
                        double angle = i * MathHelper.Tau / 6;
                        var hole = new Translate(new Cylinder(2.8, 16, 16), Math.Cos(angle) * 24, Math.Sin(angle) * 24, 0);
                        boltHoles = (boltHoles == null) ? hole : boltHoles + hole;
                    }

                    var flange = (baseDisk + hub) - (centerBore + boltHoles);
                    list.Add((CsgToMesh.Convert(flange), new Color(80, 180, 255)));
                    return list;
                }),

            new HelpItem(
                "Interlocking Chain Links",
                "Full Model",
                AccentGreen,
                "Two interlocking torus rings in perpendicular orientations with black and indigo finishes.",
                "var ring1 = new Torus(innerRadius: 10, outerRadius: 20);\nvar ring2 = new Rotate(\n    new Translate(new Torus(innerRadius: 10, outerRadius: 20), 20, 20, 0),\n    x: MathHelper.DegreesToRadians(90), z: MathHelper.DegreesToRadians(90));\n\nDraw(ring1, Color.Black);\nDraw(ring2, Color.Indigo);",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var ring1 = new Torus(innerRadius: 10, outerRadius: 20);
                    var ring2 = new Rotate(
                        new Translate(new Torus(innerRadius: 10, outerRadius: 20), 20, 20, 0),
                        x: MathHelper.DegreesToRadians(90), z: MathHelper.DegreesToRadians(90));

                    list.Add((CsgToMesh.Convert(ring1), Color.Black));
                    list.Add((CsgToMesh.Convert(ring2), Color.Indigo));
                    return list;
                }),

            new HelpItem(
                "Hollow Rounded Container",
                "Full Model",
                AccentGreen,
                "Hollow box container with cross-drilled ventilation and drainage ports.",
                "// Outer body minus inner hollow\nvar outer = new Box(40, 40, 30);\nvar inner = new Translate(new Box(35, 35, 28), 0, 0, 2);\n\n// Vent holes\nvar vent1 = new Cylinder(4, 50, 16);\nvar vent2 = new Rotate(vent1, x: MathHelper.DegreesToRadians(90));\n\nvar container = outer - inner - vent1 - vent2;\nDraw(container, RGB(60, 220, 140));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var outer = new Box(36, 36, 28);
                    var inner = new Translate(new Box(31, 31, 26), 0, 0, 2);
                    var ventX = new Cylinder(3.5, 45, 16, Alignment.x);
                    var ventY = new Cylinder(3.5, 45, 16, Alignment.y);

                    var container = outer - inner - ventX - ventY;
                    return new() { (CsgToMesh.Convert(container), new Color(60, 220, 140)) };
                }),

            new HelpItem(
                "Rainbow Spiral Sculpture",
                "Full Model",
                AccentGreen,
                "Procedural parametric stack of rotated square plates forming a twisting spiral with color gradient.",
                "// Procedural twisted tower\nfor (int i = 0; i < 12; i++) {\n    double h = i * 4;\n    double rad = MathHelper.DegreesToRadians(i * 15);\n    var plate = new Translate(\n        new Rotate(new Box(28 - i, 28 - i, 3.5), z: rad),\n        0, 0, h);\n    int r = (int)(80 + i * 14);\n    int b = (int)(255 - i * 18);\n    Draw(plate, RGB(r, 100, b));\n}",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    for (int i = 0; i < 10; i++)
                    {
                        double h = i * 4.5;
                        double rad = MathHelper.DegreesToRadians(i * 18);
                        var plate = new Translate(
                            new Rotate(new Box(28 - i * 1.5, 28 - i * 1.5, 3.8), z: rad),
                            0, 0, h);
                        int r = (int)(70 + i * 18);
                        int b = (int)(255 - i * 22);
                        list.Add((CsgToMesh.Convert(plate), new Color(r, 110, b)));
                    }
                    return list;
                }),

            new HelpItem(
                "Point -> Line -> Cylinder Compatibility",
                "Full Model",
                AccentGreen,
                "Demonstrates complete pipeline compatibility: points create lines, lines expose .Height property, and .Height is passed directly to new Cylinder(radius, height: line.Height) or new Cylinder(line, radius).",
                "// 1. Points create a line\nvar p1 = new Point(0, 0, -20);\nvar p2 = new Point(0, 0, 20);\nvar line = new Line(p1, p2);\n\n// 2. Line height used in cylinder\nvar cyl = new Cylinder(radius: 8, height: line.Height);\n\nDraw(cyl, RGB(80, 180, 255));\nDraw(new Sphere(3.5).Translate(p1), Color.Red);\nDraw(new Sphere(3.5).Translate(p2), Color.Green);",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var p1 = new Point(0, 0, -20);
                    var p2 = new Point(0, 0, 20);
                    var line = new Line(p1, p2);
                    var cyl = new Cylinder(radius: 8, height: line.Height);
                    var marker1 = new Sphere(3.5).Translate(p1.Position3D);
                    var marker2 = new Sphere(3.5).Translate(p2.Position3D);

                    list.Add((CsgToMesh.Convert(cyl), new Color(80, 180, 255)));
                    list.Add((CsgToMesh.Convert(marker1), new Color(255, 90, 90)));
                    list.Add((CsgToMesh.Convert(marker2), new Color(90, 255, 120)));
                    return list;
                }),

            new HelpItem(
                "LINQ Filtered Right-Side Spheres & Struts",
                "Full Model",
                AccentGreen,
                "Extracts all 12 bounding edges from a cube, uses LINQ (.SelectMany, .Where, .Distinct) to isolate right-side points (X > 0) and attach spheres, and bridges right edges with golden cylinder struts.",
                "// 1. Base cube\nvar cube = new Box(28, 28, 28);\n\n// 2. LINQ: Filter right-side points (X > 0)\nvar rightPoints = cube.GetBoundingEdges()\n    .SelectMany(e => new[] { e.Start, e.End })\n    .Where(p => p.X > 0)\n    .Distinct();\n\n// 3. Attach spheres to right-side points\nforeach (var pt in rightPoints)\n{\n    Draw(new Sphere(radius: 4.2).Translate(pt), Color.Red);\n}\n\n// 4. LINQ: Filter right-side edges\nvar rightEdges = cube.GetBoundingEdges()\n    .Where(e => e.Start.X > 0 && e.End.X > 0);\n\nforeach (var (start, end) in rightEdges)\n{\n    Draw(CylinderBetween(start, end, radius: 1.5), Color.Yellow);\n}\n\nDraw(cube, RGB(60, 80, 130));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var cube = new Box(28, 28, 28);
                    list.Add((CsgToMesh.Convert(cube), new Color(60, 80, 130)));

                    var rightPoints = cube.GetBoundingEdges()
                        .SelectMany(e => new[] { e.Start, e.End })
                        .Where(p => p.X > 0)
                        .Distinct();

                    foreach (var pt in rightPoints)
                    {
                        var sphere = new Sphere(4.2).Translate(pt);
                        list.Add((CsgToMesh.Convert(sphere), Color.Red));
                    }

                    var rightEdges = cube.GetBoundingEdges()
                        .Where(e => e.Start.X > 0 && e.End.X > 0);

                    foreach (var (start, end) in rightEdges)
                    {
                        var strut = Cylinder.CreateBetween(start, end, 1.5);
                        list.Add((CsgToMesh.Convert(strut), new Color(245, 215, 75)));
                    }

                    return list;
                }),

            new HelpItem(
                "Parametric Spur Gear",
                "Full Model",
                AccentGreen,
                "Generates an involute-style spur gear with 12 radial teeth, central hub, axle through-bore, and a keyed shaft notch.",
                "// 1. Pitch disk and hub\nvar baseDisk = new Cylinder(radius: 24, height: 6, sides: 32);\nvar hub = new Cylinder(radius: 12, height: 14, sides: 28);\n\n// 2. 12 radial gear teeth\nCsgObject teeth = null;\nint toothCount = 12;\nfor (int i = 0; i < toothCount; i++)\n{\n    double angle = i * MathHelper.Tau / toothCount;\n    var tooth = new Rotate(\n        new Translate(new Box(5, 7, 6), 24, 0, 0),\n        z: angle);\n    teeth = (teeth == null) ? tooth : teeth + tooth;\n}\n\n// 3. Central shaft bore and keyway notch\nvar shaftBore = new Cylinder(radius: 5, height: 20, sides: 24);\nvar keyway = new Translate(new Box(3, 4, 20), 4.5, 0, 0);\n\nvar gear = (baseDisk + teeth + hub) - (shaftBore + keyway);\nDraw(gear, RGB(245, 180, 50));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var baseDisk = new Cylinder(radius: 24, height: 6, sides: 32);
                    var hub = new Cylinder(radius: 12, height: 14, sides: 28);

                    CsgObject teeth = null;
                    int toothCount = 12;
                    for (int i = 0; i < toothCount; i++)
                    {
                        double angle = i * MathHelper.Tau / toothCount;
                        var tooth = new Rotate(
                            new Translate(new Box(5, 7, 6), 24, 0, 0),
                            z: angle);
                        teeth = (teeth == null) ? tooth : teeth + tooth;
                    }

                    var shaftBore = new Cylinder(radius: 5, height: 20, sides: 24);
                    var keyway = new Translate(new Box(3, 4, 20), 4.5, 0, 0);

                    var gear = (baseDisk + teeth + hub) - (shaftBore + keyway);
                    list.Add((CsgToMesh.Convert(gear), new Color(245, 180, 50)));
                    return list;
                }),

            new HelpItem(
                "Space-Truss Structural Lattice (LINQ)",
                "Full Model",
                AccentGreen,
                "Uses LINQ to filter top and bottom ring vertices from a bounding frame, construct diagonal cross-braces via CylinderBetween, and mount node spheres at all joints.",
                "// 1. Create a 3D structural bounding box\nvar frame = new Box(32, 32, 24);\n\n// 2. LINQ: extract top and bottom ring vertices\nvar topNodes = frame.GetCorners().Where(p => p.Z > 0).ToList();\nvar botNodes = frame.GetCorners().Where(p => p.Z < 0).ToList();\n\n// 3. Connect cross-bracing diagonal struts\nCsgObject diagonals = null;\nfor (int i = 0; i < topNodes.Count; i++)\n{\n    var tPt = topNodes[i];\n    var bPt = botNodes[(i + 1) % botNodes.Count];\n    var strut = CylinderBetween(tPt, bPt, radius: 1.2);\n    diagonals = (diagonals == null) ? strut : diagonals + strut;\n}\n\n// 4. Place node spheres at all corner vertices\nCsgObject nodes = null;\nforeach (var pt in frame.GetCorners())\n{\n    var sphere = new Sphere(radius: 3.2).Translate(pt);\n    nodes = (nodes == null) ? sphere : nodes + sphere;\n}\n\n// 5. Central glowing core\nvar core = new Sphere(radius: 7);\n\nDraw(diagonals, RGB(80, 220, 240));\nDraw(nodes, RGB(255, 100, 100));\nDraw(core, RGB(255, 220, 80));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var frame = new Box(32, 32, 24);

                    var topNodes = frame.GetCorners().Where(p => p.Z > 0).ToList();
                    var botNodes = frame.GetCorners().Where(p => p.Z < 0).ToList();

                    CsgObject diagonals = null;
                    for (int i = 0; i < topNodes.Count; i++)
                    {
                        var tPt = topNodes[i];
                        var bPt = botNodes[(i + 1) % botNodes.Count];
                        var strut = Cylinder.CreateBetween(tPt, bPt, 1.2);
                        diagonals = (diagonals == null) ? strut : diagonals + strut;
                    }

                    CsgObject nodes = null;
                    foreach (var pt in frame.GetCorners())
                    {
                        var sphere = new Sphere(3.2).Translate(pt);
                        nodes = (nodes == null) ? sphere : nodes + sphere;
                    }

                    var core = new Sphere(7);

                    if (diagonals != null) list.Add((CsgToMesh.Convert(diagonals), new Color(80, 220, 240)));
                    if (nodes != null) list.Add((CsgToMesh.Convert(nodes), new Color(255, 100, 100)));
                    list.Add((CsgToMesh.Convert(core), new Color(255, 220, 80)));
                    return list;
                }),

            new HelpItem(
                "Helical Spiral Staircase",
                "Full Model",
                AccentGreen,
                "Parametric spiral staircase featuring a central newel post pillar, cantilevered winding steps, and vertical perimeter baluster spindles.",
                "// 1. Central newel post column\nvar pillar = new Cylinder(radius: 4, height: 60, sides: 28);\n\n// 2. Procedural winding steps and baluster spindles\nCsgObject steps = null;\nCsgObject balusters = null;\nint stepCount = 16;\n\nfor (int i = 0; i < stepCount; i++)\n{\n    double z = (i - 8) * 3.5;\n    double angle = i * MathHelper.Tau / 12;\n\n    var tread = new Translate(\n        new Rotate(new Box(20, 6, 2.5), z: angle),\n        Math.Cos(angle) * 10, Math.Sin(angle) * 10, z);\n    steps = (steps == null) ? tread : steps + tread;\n\n    double outerX = Math.Cos(angle) * 19;\n    double outerY = Math.Sin(angle) * 19;\n    var baluster = new Cylinder(radius: 0.9, height: 12, sides: 12)\n        .Translate(outerX, outerY, z + 6);\n    balusters = (balusters == null) ? baluster : balusters + baluster;\n}\n\nDraw(pillar, RGB(180, 185, 200));\nDraw(steps, RGB(255, 155, 60));\nDraw(balusters, RGB(80, 190, 255));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var pillar = new Cylinder(radius: 4, height: 60, sides: 28);
                    list.Add((CsgToMesh.Convert(pillar), new Color(180, 185, 200)));

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

                    if (steps != null) list.Add((CsgToMesh.Convert(steps), new Color(255, 155, 60)));
                    if (balusters != null) list.Add((CsgToMesh.Convert(balusters), new Color(80, 190, 255)));
                    return list;
                }),

            new HelpItem(
                "Hollow Perforated Celestial Sphere",
                "Full Model",
                AccentGreen,
                "Hollow spherical cage created by subtracting a concentric inner sphere and 3 orthogonal perforation bores (X, Y, Z), encasing a central diamond gem.",
                "// 1. Outer shell sphere minus concentric inner hollow cavity\nvar outerSphere = new Sphere(radius: 20);\nvar innerHollow = new Sphere(radius: 16.5);\n\n// 2. Orthogonal perforation boring cylinders (X, Y, Z axes)\nvar boreX = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.x);\nvar boreY = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.y);\nvar boreZ = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.z);\n\n// 3. Subtract to sculpt perforated cage shell\nvar cage = outerSphere - innerHollow - boreX - boreY - boreZ;\n\n// 4. Inner suspended diamond gem\nvar gem = new Rotate(new Box(11, 11, 11),\n    x: MathHelper.DegreesToRadians(45),\n    y: MathHelper.DegreesToRadians(45));\n\nDraw(cage, RGB(60, 140, 240));\nDraw(gem, RGB(255, 75, 130));",
                IsFullExample: true,
                BuildMeshes: () =>
                {
                    var list = new List<(Mesh, Color)>();
                    var outerSphere = new Sphere(radius: 20);
                    var innerHollow = new Sphere(radius: 16.5);

                    var boreX = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.x);
                    var boreY = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.y);
                    var boreZ = new Cylinder(radius: 6, height: 45, sides: 24, alignment: Alignment.z);

                    var cage = outerSphere - innerHollow - boreX - boreY - boreZ;

                    var gem = new Rotate(new Box(11, 11, 11),
                        x: MathHelper.DegreesToRadians(45),
                        y: MathHelper.DegreesToRadians(45));

                    list.Add((CsgToMesh.Convert(cage), new Color(60, 140, 240)));
                    list.Add((CsgToMesh.Convert(gem), new Color(255, 75, 130)));
                    return list;
                })
        };
    }
}
