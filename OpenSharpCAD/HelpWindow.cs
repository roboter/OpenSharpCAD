using System;
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;

namespace CSharpCAD
{
    /// <summary>
    /// Interactive Help & Reference overlay window that displays all available shapes,
    /// transformations, CSG boolean operations, colors, math functions, and ready-to-run examples.
    /// Includes direct "Insert / Load" buttons to paste code straight into the editor.
    /// </summary>
    public class HelpWindow : GuiWidget
    {
        private readonly Action<string, bool> _onInsertCode;
        private readonly Action _onClose;

        private FlowLayoutWidget _contentFlow;
        private ScrollableWidget _scrollableContent;
        private readonly List<StyledButton> _tabButtons = new();
        private int _currentTabIndex = 0;

        // Visual Palette (Dracula-inspired dark theme matching editor)
        private static readonly Color OverlayBg          = new Color(15, 17, 26, 210);
        private static readonly Color DialogBg           = new Color(34, 36, 48);
        private static readonly Color HeaderBg           = new Color(26, 27, 38);
        private static readonly Color TabBarBg           = new Color(28, 30, 42);
        private static readonly Color CardBg             = new Color(42, 45, 60);
        private static readonly Color CodeBoxBg          = new Color(20, 21, 29);
        private static readonly Color CardBorderColor     = new Color(68, 71, 90);
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
        /// Avoids the default ButtonViewText which renders black text on dark dialog backgrounds.
        /// </summary>
        private class StyledButton : GuiWidget
        {
            private readonly TextWidget _labelWidget;
            private Color _normalBg;
            private Color _hoverBg;

            public StyledButton(string text, Color textColor, Color normalBg, Color hoverBg, double pointSize = 10, BorderDouble padding = default)
            {
                _normalBg = normalBg;
                _hoverBg = hoverBg;
                BackgroundColor = normalBg;
                Cursor = Cursors.Hand;
                Padding = (padding.Width == 0 && padding.Height == 0) ? new BorderDouble(10, 5) : padding;
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

        private enum ResizeEdge
        {
            Left,
            Right,
            Top,
            Bottom,
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        private class ResizeHandleWidget : GuiWidget
        {
            private readonly ResizeEdge _edge;
            private readonly Action<ResizeEdge, Vector2> _onResize;
            private readonly Action _onStartDrag;
            private readonly Action _onEndDrag;
            private Vector2 _startScreenPos;
            private bool _isDragging;

            public ResizeHandleWidget(
                ResizeEdge edge,
                Action<ResizeEdge, Vector2> onResize,
                Action onStartDrag,
                Action onEndDrag,
                Cursors cursor)
            {
                _edge = edge;
                _onResize = onResize;
                _onStartDrag = onStartDrag;
                _onEndDrag = onEndDrag;
                Cursor = cursor;
                BackgroundColor = Color.Transparent;
            }

            public override void OnMouseDown(MouseEventArgs mouseEvent)
            {
                if (mouseEvent.Button == MouseButtons.Left)
                {
                    _isDragging = true;
                    _startScreenPos = this.TransformToScreenSpace(new Vector2(mouseEvent.X, mouseEvent.Y));
                    _onStartDrag?.Invoke();
                }
                base.OnMouseDown(mouseEvent);
            }

            public override void OnMouseMove(MouseEventArgs mouseEvent)
            {
                if (_isDragging)
                {
                    var curScreen = this.TransformToScreenSpace(new Vector2(mouseEvent.X, mouseEvent.Y));
                    var delta = curScreen - _startScreenPos;
                    _onResize?.Invoke(_edge, delta);
                }
                base.OnMouseMove(mouseEvent);
            }

            public override void OnMouseUp(MouseEventArgs mouseEvent)
            {
                _isDragging = false;
                _onEndDrag?.Invoke();
                base.OnMouseUp(mouseEvent);
            }

            public override void OnDraw(Graphics2D g)
            {
                base.OnDraw(g);
                if (_edge == ResizeEdge.BottomRight)
                {
                    // Draw 3 diagonal grip lines in the bottom-right corner
                    var gripColor = new Color(120, 138, 190, 180);
                    for (int i = 0; i < 3; i++)
                    {
                        double offset = 4 + i * 4;
                        g.Line(Width - offset, 3, Width - 3, offset, gripColor, 1.5);
                    }
                }
            }
        }

        private record HelpItem(
            string Title,
            string Badge,
            Color BadgeColor,
            string Description,
            string Snippet,
            bool IsFullExample = false);

        public HelpWindow(Action<string, bool> onInsertCode, Action onClose)
            : base(540, 420)
        {
            _onInsertCode = onInsertCode;
            _onClose      = onClose;

            HAnchor = HAnchor.Absolute;
            VAnchor = VAnchor.Absolute;
            BackgroundColor = DialogBg;
            MinimumSize = new Vector2(460, 320);
            Position = new Vector2(430, 40);

            // Sleek window frame styling
            Border = 1;
            BorderColor = new Color(80, 88, 120);

            BuildUi();
        }

        public GuiWidget DialogCard => this;

        private StyledButton _maxButton;
        private bool _isResizing;
        private bool _isDraggingHeader;
        private bool _hasUserMovedOrResized;
        private bool _isMaximized;
        private Vector2 _dragStartPos;
        private Vector2 _dragStartSize;
        private Vector2 _restorePos;
        private Vector2 _restoreSize;

        public override void OnKeyDown(KeyEventArgs keyEvent)
        {
            if (keyEvent.KeyCode == Keys.Escape || keyEvent.KeyCode == Keys.F1)
            {
                _onClose?.Invoke();
                keyEvent.Handled = true;
                return;
            }
            base.OnKeyDown(keyEvent);
        }

        private void BuildUi()
        {
            var dialogLayout = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
            };
            AddChild(dialogLayout);

            // 1. Top Header Bar (Draggable to move dialog, double-click to maximize/restore)
            var header = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 44,
                BackgroundColor = HeaderBg,
                Padding = new BorderDouble(14, 6),
                Cursor = Cursors.Hand,
            };

            Vector2 headerStartMouseScreen = Vector2.Zero;
            header.MouseDown += (s, e) =>
            {
                if (e.Clicks >= 2)
                {
                    _isDraggingHeader = false;
                    ToggleMaximize();
                    return;
                }

                if (e.Button == MouseButtons.Left && !_isMaximized)
                {
                    _isDraggingHeader = true;
                    _hasUserMovedOrResized = true;
                    headerStartMouseScreen = header.TransformToScreenSpace(new Vector2(e.X, e.Y));
                    _dragStartPos = Position;
                }
            };

            header.MouseMove += (s, e) =>
            {
                if (_isDraggingHeader && !_isMaximized)
                {
                    var curScreen = header.TransformToScreenSpace(new Vector2(e.X, e.Y));
                    var delta = curScreen - headerStartMouseScreen;
                    double parentW = Parent?.Width ?? 1024;
                    double parentH = Parent?.Height ?? 768;
                    double newX = Math.Clamp(_dragStartPos.X + delta.X, 0, Math.Max(0, parentW - Width));
                    double newY = Math.Clamp(_dragStartPos.Y + delta.Y, 0, Math.Max(0, parentH - Height));
                    Position = new Vector2(newX, newY);
                    Invalidate();
                }
            };

            header.MouseUp += (s, e) =>
            {
                _isDraggingHeader = false;
            };

            var titleText = new TextWidget("OpenSharpCAD — Shapes, Transformations & CSG Reference", pointSize: 13, textColor: TextWhite, bold: true, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                AutoExpandBoundsToText = true,
            };
            header.AddChild(titleText);

            header.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

            var hintText = new TextWidget("Press ESC to close", pointSize: 10, textColor: TextMuted, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 10, 0),
                AutoExpandBoundsToText = true,
            };
            header.AddChild(hintText);

            // Maximize / Restore Button
            _maxButton = new StyledButton("[ ]", TextWhite, new Color(68, 71, 90, 180), new Color(120, 138, 190), pointSize: 10, padding: new BorderDouble(8, 5))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 6, 0),
            };
            _maxButton.Click += (s, e) => ToggleMaximize();
            header.AddChild(_maxButton);

            var closeButton = new StyledButton("X", TextWhite, new Color(68, 71, 90, 180), new Color(255, 85, 85), pointSize: 11, padding: new BorderDouble(10, 5))
            {
                VAnchor = VAnchor.Center,
            };
            closeButton.Click += (s, e) => _onClose?.Invoke();
            header.AddChild(closeButton);

            dialogLayout.AddChild(header);

            // 2. Navigation Category Tabs Bar
            var tabBar = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 38,
                BackgroundColor = TabBarBg,
                Padding = new BorderDouble(8, 4),
            };

            string[] tabNames = new[]
            {
                "Shapes",
                "Transforms",
                "Booleans",
                "Colors",
                "Math & Helpers",
                "Examples"
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
                    pointSize: 10,
                    padding: new BorderDouble(12, 6)
                )
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(3, 0),
                };
                btn.Click += (s, e) => SelectTab(tabIndex);
                _tabButtons.Add(btn);
                tabBar.AddChild(btn);
            }

            dialogLayout.AddChild(tabBar);

            // 3. Scrollable Content Area
            _scrollableContent = new ScrollableWidget(autoScroll: true)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                Padding = new BorderDouble(12),
            };

            // Ensure ScrollArea stretches to the scrollable container width so content gets full width
            _scrollableContent.ScrollArea.HAnchor = HAnchor.Stretch;

            _contentFlow = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                Width = 730,
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
            };

            // Dynamically maintain child width to track scrollable viewport
            _scrollableContent.BoundsChanged += (s, e) =>
            {
                if (_scrollableContent.Width > 60)
                {
                    _contentFlow.Width = Math.Max(300, _scrollableContent.Width - 28);
                }
            };

            _scrollableContent.AddChild(_contentFlow);
            dialogLayout.AddChild(_scrollableContent);

            // 4. Resize Handles (All 4 borders and 4 corners)
            const double handleThickness = 6;
            const double cornerSize = 16;

            void AddResizeHandle(ResizeEdge edge, Cursors cursor, HAnchor hAnchor, VAnchor vAnchor, double w, double h)
            {
                var handle = new ResizeHandleWidget(
                    edge,
                    HandleResize,
                    () =>
                    {
                        _isResizing = true;
                        _hasUserMovedOrResized = true;
                        _dragStartPos = Position;
                        _dragStartSize = Size;
                    },
                    () => _isResizing = false,
                    cursor)
                {
                    HAnchor = hAnchor,
                    VAnchor = vAnchor,
                    Width = w,
                    Height = h
                };
                AddChild(handle);
            }

            // Edges
            AddResizeHandle(ResizeEdge.Right,  Cursors.SizeWE, HAnchor.Right,   VAnchor.Stretch, handleThickness, 0);
            AddResizeHandle(ResizeEdge.Left,   Cursors.SizeWE, HAnchor.Left,    VAnchor.Stretch, handleThickness, 0);
            AddResizeHandle(ResizeEdge.Top,    Cursors.SizeNS, HAnchor.Stretch, VAnchor.Top,     0, handleThickness);
            AddResizeHandle(ResizeEdge.Bottom, Cursors.SizeNS, HAnchor.Stretch, VAnchor.Bottom,  0, handleThickness);

            // Corners
            AddResizeHandle(ResizeEdge.TopRight,    Cursors.SizeNESW, HAnchor.Right, VAnchor.Top,    cornerSize, cornerSize);
            AddResizeHandle(ResizeEdge.TopLeft,     Cursors.SizeNWSE, HAnchor.Left,  VAnchor.Top,    cornerSize, cornerSize);
            AddResizeHandle(ResizeEdge.BottomLeft,  Cursors.SizeNESW, HAnchor.Left,  VAnchor.Bottom, cornerSize, cornerSize);
            AddResizeHandle(ResizeEdge.BottomRight, Cursors.SizeNWSE, HAnchor.Right, VAnchor.Bottom, cornerSize + 2, cornerSize + 2);

            // Load initial tab
            SelectTab(0);
        }

        public void ToggleMaximize()
        {
            double parentW = Parent?.Width ?? 1024;
            double parentH = Parent?.Height ?? 768;

            if (!_isMaximized)
            {
                _restorePos = Position;
                _restoreSize = Size;
                Position = new Vector2(16, 16);
                Size = new Vector2(Math.Max(520, parentW - 32), Math.Max(380, parentH - 32));
                _isMaximized = true;
                _hasUserMovedOrResized = true;
                _maxButton?.SetText("[=]");
            }
            else
            {
                Position = _restorePos;
                Size = _restoreSize;
                _isMaximized = false;
                _maxButton?.SetText("[ ]");
            }
            Invalidate();
        }

        private void HandleResize(ResizeEdge edge, Vector2 deltaScreen)
        {
            if (_isMaximized) return;

            double parentW = Parent?.Width ?? 1024;
            double parentH = Parent?.Height ?? 768;
            double minW = 460;
            double minH = 320;
            double maxW = Math.Max(minW, parentW - 20);
            double maxH = Math.Max(minH, parentH - 20);

            double newW = _dragStartSize.X;
            double newH = _dragStartSize.Y;
            double newX = _dragStartPos.X;
            double newY = _dragStartPos.Y;

            switch (edge)
            {
                case ResizeEdge.Right:
                case ResizeEdge.BottomRight:
                case ResizeEdge.TopRight:
                    newW = Math.Clamp(_dragStartSize.X + deltaScreen.X, minW, maxW);
                    break;
                case ResizeEdge.Left:
                case ResizeEdge.BottomLeft:
                case ResizeEdge.TopLeft:
                    double candidateW = Math.Clamp(_dragStartSize.X - deltaScreen.X, minW, maxW);
                    newX = _dragStartPos.X + (_dragStartSize.X - candidateW);
                    newW = candidateW;
                    break;
            }

            switch (edge)
            {
                case ResizeEdge.Top:
                case ResizeEdge.TopLeft:
                case ResizeEdge.TopRight:
                    newH = Math.Clamp(_dragStartSize.Y + deltaScreen.Y, minH, maxH);
                    break;
                case ResizeEdge.Bottom:
                case ResizeEdge.BottomLeft:
                case ResizeEdge.BottomRight:
                    double candidateH = Math.Clamp(_dragStartSize.Y - deltaScreen.Y, minH, maxH);
                    newY = _dragStartPos.Y + (_dragStartSize.Y - candidateH);
                    newH = candidateH;
                    break;
            }

            Position = new Vector2(newX, newY);
            Size = new Vector2(newW, newH);
            Invalidate();
        }

        public override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (Parent != null)
            {
                if (!_hasUserMovedOrResized)
                {
                    double posX = Math.Max(20, Math.Min(430, Parent.Width - Width - 20));
                    double posY = Math.Max(20, Math.Min(40, Parent.Height - Height - 20));
                    Position = new Vector2(posX, posY);
                }
                Parent.BoundsChanged -= Parent_BoundsChanged;
                Parent.BoundsChanged += Parent_BoundsChanged;
            }
        }

        private void Parent_BoundsChanged(object sender, EventArgs e)
        {
            EnsureWithinParent();
        }

        public void EnsureWithinParent()
        {
            if (Parent == null) return;
            if (_isMaximized)
            {
                Position = new Vector2(16, 16);
                Size = new Vector2(Math.Max(520, Parent.Width - 32), Math.Max(380, Parent.Height - 32));
                return;
            }

            double maxX = Math.Max(0, Parent.Width - Width);
            double maxY = Math.Max(0, Parent.Height - Height);
            double clampedX = Math.Clamp(Position.X, 0, maxX);
            double clampedY = Math.Clamp(Position.Y, 0, maxY);
            if (clampedX != Position.X || clampedY != Position.Y)
            {
                Position = new Vector2(clampedX, clampedY);
                Invalidate();
            }
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

            // Scroll to top
            _scrollableContent.TopLeftOffset = Vector2.Zero;
        }

        private GuiWidget CreateItemCard(HelpItem item)
        {
            var card = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
                BackgroundColor = CardBg,
                Margin = new BorderDouble(0, 4, 0, 8),
                Padding = new BorderDouble(12, 10),
            };

            // Top row: Title + Badge + Insert Button
            var topRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 28,
            };

            var titleLabel = new TextWidget(item.Title, pointSize: 13, textColor: TextWhite, bold: true, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                AutoExpandBoundsToText = true,
            };
            topRow.AddChild(titleLabel);

            var badge = new GuiWidget(0, 18)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(10, 0, 0, 0),
                Padding = new BorderDouble(7, 3),
                BackgroundColor = new Color(item.BadgeColor, 40),
            };
            badge.AddChild(new TextWidget(item.Badge, pointSize: 9, textColor: item.BadgeColor, bold: true, ellipsisIfClipped: false)
            {
                VAnchor = VAnchor.Center,
                HAnchor = HAnchor.Center,
                AutoExpandBoundsToText = true,
            });
            badge.SetBoundsToEncloseChildren();
            topRow.AddChild(badge);

            topRow.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

            string btnText = item.IsFullExample ? "Load Example" : "Insert Snippet";
            var insertBtn = new StyledButton(btnText, new Color(20, 24, 32), AccentGreen, AccentGreenHover, pointSize: 10, padding: new BorderDouble(10, 4))
            {
                VAnchor = VAnchor.Center,
            };
            insertBtn.Click += (s, e) =>
            {
                _onInsertCode?.Invoke(item.Snippet, item.IsFullExample);
            };
            topRow.AddChild(insertBtn);

            card.AddChild(topRow);

            // Description — rendered line by line to guarantee clear visibility and proper heights
            var descFlow = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
                Margin = new BorderDouble(0, 6, 0, 6),
            };

            var descLines = item.Description.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var dLine in descLines)
            {
                var dText = new TextWidget(dLine, pointSize: 10, textColor: TextMuted, ellipsisIfClipped: false)
                {
                    AutoExpandBoundsToText = true,
                    Margin = new BorderDouble(0, 1),
                };
                descFlow.AddChild(dText);
            }
            card.AddChild(descFlow);

            // Code Snippet Box (click to insert as well)
            var codeBox = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
                BackgroundColor = CodeBoxBg,
                Padding = new BorderDouble(10, 8),
                Margin = new BorderDouble(0, 4, 0, 0),
                Cursor = Cursors.Hand,
            };

            string[] lines = item.Snippet.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var lineText = new TextWidget(line, pointSize: 10, textColor: AccentYellow, ellipsisIfClipped: false)
                {
                    AutoExpandBoundsToText = true,
                };
                codeBox.AddChild(lineText);
            }

            codeBox.Click += (s, e) =>
            {
                _onInsertCode?.Invoke(item.Snippet, item.IsFullExample);
            };

            card.AddChild(codeBox);

            return card;
        }

        private List<HelpItem> GetItemsForTab(int tabIndex) => tabIndex switch
        {
            0 => GetShapeItems(),
            1 => GetTransformItems(),
            2 => GetBooleanItems(),
            3 => GetColorItems(),
            4 => GetMathItems(),
            5 => GetExampleItems(),
            _ => new List<HelpItem>()
        };

        private List<HelpItem> GetShapeItems() => new()
        {
            new HelpItem(
                "Box",
                "Solid Primitive",
                AccentCyan,
                "Creates a rectangular box centered at the origin by default.\nParameters: width (X), depth (Y), height (Z), name = \"\", createCentered = true",
                "// Box(width, depth, height)\nvar box = new Box(20, 30, 10);\nDraw(box, RGB(80, 180, 255));"),

            new HelpItem(
                "Cylinder",
                "Solid Primitive",
                AccentCyan,
                "Creates a cylindrical solid centered along the Z axis.\nParameters: radius, height, sides (resolution), alignment = Alignment.z",
                "// Cylinder(radius, height, sides)\nvar cyl = new Cylinder(radius: 8, height: 30, sides: 36);\nDraw(cyl, Color.Orange);"),

            new HelpItem(
                "Cone / Tapered Cylinder",
                "Solid Primitive",
                AccentCyan,
                "Creates a cone or tapered cylinder with different top and bottom radii.\nParameters: radius1 (bottom), radius2 (top, 0 for sharp cone), height, sides",
                "// Cylinder(radius1, radius2, height, sides)\nvar cone = new Cylinder(radius1: 14, radius2: 0, height: 25, sides: 36);\nDraw(cone, Color.Yellow);"),

            new HelpItem(
                "Sphere",
                "Solid Primitive",
                AccentCyan,
                "Creates a UV sphere centered at (0, 0, 0).\nParameters: radius, name = \"\"",
                "// Sphere(radius)\nvar sphere = new Sphere(radius: 12);\nDraw(sphere, Color.Cyan);"),

            new HelpItem(
                "Torus (Donut)",
                "Solid Primitive",
                AccentCyan,
                "Creates a torus ring centered at origin with inner and outer radii.\nParameters: innerRadius, outerRadius, alignment = Alignment.z",
                "// Torus(innerRadius, outerRadius)\nvar donut = new Torus(innerRadius: 10, outerRadius: 22);\nDraw(donut, Color.Violet);"),

            new HelpItem(
                "Circle (2D Primitive)",
                "2D Primitive",
                AccentGreen,
                "Creates a 2D circle that can be extruded into a cylinder or revolved into a torus.\nMethods: Extrude(height), Revolve(angleDegrees, axisOffset), ToMesh()",
                "// Circle(radius)\nvar c = new Circle(radius: 15);\nvar cylinder = c.Extrude(height: 25);\nDraw(cylinder, RGB(100, 200, 255));"),

            new HelpItem(
                "Rectangle & Square (2D)",
                "2D Primitive",
                AccentGreen,
                "Creates 2D rectangles or squares (with optional rounded corners).\nMethods: Extrude(height), Revolve(angleDegrees), ToMesh()",
                "// Rectangle(width, height) or Square(size)\nvar rect = new Rectangle(width: 40, height: 25, cornerRadius: 4);\nvar body = rect.Extrude(height: 12);\nDraw(body, RGB(255, 160, 80));"),

            new HelpItem(
                "Path (2D Closed Polyline)",
                "2D Primitive",
                AccentGreen,
                "Creates a 2D path from points. MUST be closed (via .Close()) before extruding/revolving!\nValidation will alert if the path is left open.\nMethods: MoveTo, LineTo, CurveTo, Close, Extrude, Revolve",
                "// Path: build closed shape & extrude\nvar p = new Path();\np.MoveTo(-15, -10).LineTo(15, -10).LineTo(20, 10).LineTo(-20, 10).Close();\nvar prism = p.Extrude(height: 18);\nDraw(prism, RGB(120, 240, 160));"),

            new HelpItem(
                "Line & Dot (2D)",
                "2D Primitive",
                AccentYellow,
                "Line and Dot 2D primitives. Lines and dots cannot be extruded directly because they do not enclose an area. Combine lines into a closed Path or convert Dot to Circle.",
                "// Dot and Line: connect lines into closed path\nvar p = new Path();\np.Add(new Line(0, 0, 30, 0));\np.Add(new Line(30, 0, 15, 25));\np.Close(); // Required validation!\nDraw(p.Extrude(10), Color.Cyan);"),

            new HelpItem(
                "LinearExtrude",
                "2D Extrusion",
                AccentGreen,
                "Extrudes a 2D polygon defined by [x0, y0, x1, y1...] pairs along the Z axis with optional twist.\nParameters: double[] points, height, alignment = Alignment.z, twistRadians = 0",
                "// 2D Polygon Linear Extrusion with twist\ndouble[] pts = new double[] { -10, -10,  10, -10,  14, 10,  -14, 10 };\nvar shape = new LinearExtrude(pts, height: 25, twistRadians: MathHelper.DegreesToRadians(30));\nDraw(shape, RGB(120, 220, 140));")
        };

        private List<HelpItem> GetTransformItems() => new()
        {
            new HelpItem(
                "Translate",
                "Transformation",
                AccentPink,
                "Translates (moves) an object in 3D coordinate space by X, Y, Z offsets.\nParameters: object, x, y, z  OR  new Translate(object, new Vector3(x, y, z))",
                "var box = new Box(15, 15, 15);\nvar shifted = new Translate(box, x: 20, y: 0, z: 10);\nDraw(box, Color.Gray);\nDraw(shifted, Color.Red);"),

            new HelpItem(
                "Rotate",
                "Transformation",
                AccentPink,
                "Rotates an object around X, Y, and/or Z axes. Angles must be in radians!\nUse MathHelper.DegreesToRadians(deg) for degrees.\nParameters: object, x: rx, y: ry, z: rz",
                "var box = new Box(10, 25, 10);\nvar rotated = new Rotate(box, z: MathHelper.DegreesToRadians(45));\nDraw(rotated, Color.Green);"),

            new HelpItem(
                "Scale",
                "Transformation",
                AccentPink,
                "Scales an object non-uniformly or uniformly.\nParameters: object, scaleX, scaleY, scaleZ  OR  new Scale(object, uniformScale)",
                "var sphere = new Sphere(10);\nvar stretched = new Scale(sphere, 1.0, 1.0, 2.0);\nDraw(stretched, Color.Cyan);"),

            new HelpItem(
                "Align",
                "Transformation",
                AccentPink,
                "Snaps a bounding face of one object against a bounding face of another object.\nFaces: Face.Left, Face.Right, Face.Front, Face.Back, Face.Bottom, Face.Top\nParameters: objectToAlign, faceToAlign, targetObject, targetFace, offsetX, offsetY, offsetZ",
                "var table = new Box(40, 40, 4);\nvar pillar = new Cylinder(6, 20, 24);\nvar onTop = new Align(pillar, Face.Bottom, table, Face.Top);\nDraw(table + onTop, Color.White);"),

            new HelpItem(
                "SetCenter",
                "Transformation",
                AccentPink,
                "Translates the object so its bounding box center sits at the specified coordinates.\nParameters: object, x = 0, y = 0, z = 0, onX = true, onY = true, onZ = true",
                "var box = new Box(20, 30, 10);\nvar centered = new SetCenter(box, x: 0, y: 0, z: 0);\nDraw(centered, Color.Magenta);"),

            new HelpItem(
                "Mirror",
                "Transformation",
                AccentPink,
                "Creates a mirrored duplicate across a coordinate plane.\nMethods: object.NewMirrorAccrossX(offset), NewMirrorAccrossY(), NewMirrorAccrossZ()",
                "var wing = new Translate(new Box(25, 10, 2), 15, 0, 0);\nvar otherWing = wing.NewMirrorAccrossX();\nDraw(wing + otherWing, Color.Yellow);")
        };

        private List<HelpItem> GetBooleanItems() => new()
        {
            new HelpItem(
                "Union (+ operator)",
                "Boolean CSG",
                AccentGreen,
                "Fuses multiple solids together into a single unified 3D volume.\nSyntax: shapeA + shapeB  OR  new Union(shapeA, shapeB)",
                "var arm1 = new Box(40, 8, 8);\nvar arm2 = new Box(8, 40, 8);\nvar mergedCross = arm1 + arm2;\nDraw(mergedCross, Color.Green);"),

            new HelpItem(
                "Difference (- operator)",
                "Boolean CSG",
                AccentGreen,
                "Subtracts (cuts) the second shape from the first solid to carve holes and notches.\nSyntax: baseShape - cuttingShape  OR  new Difference(baseShape, cuttingShape)",
                "var solidBlock = new Box(30, 30, 20);\nvar hole = new Cylinder(7, 30, 24);\nvar boredBlock = solidBlock - hole;\nDraw(boredBlock, RGB(80, 180, 255));"),

            new HelpItem(
                "Intersection",
                "Boolean CSG",
                AccentGreen,
                "Calculates the overlapping volume shared by both solids.\nSyntax: new Intersection(shapeA, shapeB)",
                "var cube = new Box(22, 22, 22);\nvar ball = new Sphere(14);\nvar intersection = new Intersection(cube, ball);\nDraw(intersection, Color.Orange);"),

            new HelpItem(
                "Chained CSG Expressions",
                "Composite CSG",
                AccentGreen,
                "Combine operators in mathematical expressions with parentheses for complex designs.",
                "var plate = new Box(50, 50, 6);\nvar boss = new Cylinder(16, 12, 24);\nvar bore = new Cylinder(8, 25, 24);\nvar composite = (plate + boss) - bore;\nDraw(composite, RGB(100, 200, 255));")
        };

        private List<HelpItem> GetColorItems() => new()
        {
            new HelpItem(
                "Draw(shape)",
                "Rendering",
                AccentYellow,
                "Draws a 3D solid with the default neutral grey color Color(150, 150, 150).",
                "var box = new Box(20, 20, 20);\nDraw(box);"),

            new HelpItem(
                "Draw(shape, color)",
                "Rendering",
                AccentYellow,
                "Draws a 3D solid with an explicit color parameter.",
                "Draw(new Sphere(12), Color.Red);\nDraw(new Translate(new Box(15, 15, 15), 25, 0, 0), Color.Cyan);"),

            new HelpItem(
                "RGB(r, g, b)",
                "Color Constructor",
                AccentYellow,
                "Constructs a color using standard 0-255 integer RGB channels.\nParameters: int red, int green, int blue (0 to 255)",
                "Draw(new Cylinder(10, 25, 32), RGB(255, 140, 20));"),

            new HelpItem(
                "RGBA(r, g, b, a)",
                "Color Constructor",
                AccentYellow,
                "Constructs a color with an alpha transparency channel (0 to 255).\nParameters: int red, int green, int blue, int alpha",
                "Draw(new Sphere(15), RGBA(80, 200, 255, 180));"),

            new HelpItem(
                "RGBf(r, g, b)",
                "Color Constructor",
                AccentYellow,
                "Constructs a color using normalized 0.0 to 1.0 floating point values.\nParameters: double red, double green, double blue (0.0 to 1.0)",
                "Draw(new Torus(10, 20), RGBf(0.8, 0.2, 0.9));"),

            new HelpItem(
                "Predefined Color Constants",
                "Built-in Colors",
                AccentYellow,
                "Predefined standard colors in the Color struct:\nColor.Red, Color.Green, Color.Blue, Color.Cyan, Color.Magenta, Color.Yellow,\nColor.White, Color.Black, Color.Orange, Color.Violet, Color.Gold, Color.Silver",
                "Draw(new Translate(new Box(10, 10, 10), -20, 0, 0), Color.Red);\nDraw(new Box(10, 10, 10), Color.Green);\nDraw(new Translate(new Box(10, 10, 10), 20, 0, 0), Color.Blue);")
        };

        private List<HelpItem> GetMathItems() => new()
        {
            new HelpItem(
                "MathHelper.DegreesToRadians",
                "Angle Conversion",
                AccentPurple,
                "Converts degrees to radians for use with Rotate().\nExample: MathHelper.DegreesToRadians(90) -> PI/2",
                "double angleInRadians = MathHelper.DegreesToRadians(45);\nvar rotated = new Rotate(new Box(10, 10, 20), z: angleInRadians);\nDraw(rotated);"),

            new HelpItem(
                "Constants: Tau & PI",
                "Math Constants",
                AccentPurple,
                "MathHelper.Tau is a full turn (2 * PI ≈ 6.28318).\nMath.PI is a half turn (≈ 3.14159).\nIdeal for circular loops: angle = i * MathHelper.Tau / count.",
                "int count = 6;\nfor (int i = 0; i < count; i++) {\n    double angle = i * MathHelper.Tau / count;\n    var p = new Translate(new Sphere(4), Math.Cos(angle) * 20, Math.Sin(angle) * 20, 0);\n    Draw(p, Color.Cyan);\n}"),

            new HelpItem(
                "Standard C# Math Functions",
                "System.Math",
                AccentPurple,
                "Use standard C# math functions inside your scripts:\nMath.Sin(rad), Math.Cos(rad), Math.Sqrt(val), Math.Pow(b, e), Math.Abs(v), Math.Min(a, b), Math.Max(a, b)",
                "for (int i = -10; i <= 10; i++) {\n    double x = i * 2;\n    double z = Math.Sin(x * 0.3) * 8;\n    Draw(new Translate(new Sphere(1.5), x, 0, z), Color.Yellow);\n}")
        };

        private List<HelpItem> GetExampleItems() => new()
        {
            new HelpItem(
                "Drilled Mounting Flange",
                "Full Model",
                AccentGreen,
                "Circular flange with a center hub, central through-bore, and 6 radial mounting bolt holes.",
                "// 1. Base Disk & Center Hub\nvar baseDisk = new Cylinder(radius: 35, height: 8, sides: 28);\nvar hub = new Cylinder(radius: 18, height: 22, sides: 20);\n\n// 2. Center Through-Bore\nvar centerBore = new Cylinder(radius: 10, height: 35, sides: 20);\n\n// 3. Circular array of 6 bolt holes\nCsgObject boltHoles = null;\nfor (int i = 0; i < 6; i++) {\n    double angle = i * MathHelper.Tau / 6;\n    var hole = new Translate(new Cylinder(3, 20, 16), Math.Cos(angle) * 26, Math.Sin(angle) * 26, 0);\n    boltHoles = (boltHoles == null) ? hole : boltHoles + hole;\n}\n\n// 4. CSG combine and draw\nvar flange = (baseDisk + hub) - (centerBore + boltHoles);\nDraw(flange, RGB(80, 180, 255));",
                IsFullExample: true),

            new HelpItem(
                "Interlocking Chain Links",
                "Full Model",
                AccentGreen,
                "Two interlocking torus rings in perpendicular orientations with metallic colors.",
                "// Two interlocking toruses\nvar ring1 = new Torus(innerRadius: 12, outerRadius: 22);\nvar ring2 = new Rotate(\n    new Translate(new Torus(innerRadius: 12, outerRadius: 22), 22, 0, 0),\n    y: MathHelper.DegreesToRadians(90));\n\nDraw(ring1, Color.Gold);\nDraw(ring2, Color.Silver);",
                IsFullExample: true),

            new HelpItem(
                "Hollow Rounded Container",
                "Full Model",
                AccentGreen,
                "Hollow box container with cross-drilled ventilation and drainage ports.",
                "// Outer body minus inner hollow\nvar outer = new Box(40, 40, 30);\nvar inner = new Translate(new Box(35, 35, 28), 0, 0, 2);\n\n// Vent holes\nvar vent1 = new Cylinder(4, 50, 16);\nvar vent2 = new Rotate(vent1, x: MathHelper.DegreesToRadians(90));\n\nvar container = outer - inner - vent1 - vent2;\nDraw(container, RGB(60, 220, 140));",
                IsFullExample: true),

            new HelpItem(
                "Rainbow Spiral Sculpture",
                "Full Model",
                AccentGreen,
                "Procedural parametric stack of rotated square plates forming a twisting spiral with color gradient.",
                "// Procedural twisted tower\nfor (int i = 0; i < 12; i++) {\n    double h = i * 4;\n    double rad = MathHelper.DegreesToRadians(i * 15);\n    var plate = new Translate(\n        new Rotate(new Box(28 - i, 28 - i, 3.5), z: rad),\n        0, 0, h);\n    int r = (int)(80 + i * 14);\n    int b = (int)(255 - i * 18);\n    Draw(plate, RGB(r, 100, b));\n}",
                IsFullExample: true)
        };
    }
}
