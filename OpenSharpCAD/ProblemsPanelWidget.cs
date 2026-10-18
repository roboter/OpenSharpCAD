using System;
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

namespace CSharpCAD
{
    public class EditorDiagnostic
    {
        public string Id { get; set; } = "CS0000";
        public string Message { get; set; } = "";
        public int Line { get; set; } // 0-based
        public int Column { get; set; } // 0-based
        public int StartIndex { get; set; }
        public int Length { get; set; } = 1;
        public bool IsWarning { get; set; }
        public bool IsRuntime { get; set; }
    }

    /// <summary>
    /// Modern VS Code / Rider inspired Problems panel widget.
    /// Displays compilation errors, warnings, and runtime exceptions with clean Dracula dark styling.
    /// Clicking any error navigates directly to the exact spot in the code editor.
    /// </summary>
    public class ProblemsPanelWidget : GuiWidget
    {
        private static readonly Color HeaderBg     = new Color(22, 23, 32);
        private static readonly Color PanelBg      = new Color(18, 19, 26);
        private static readonly Color BorderLine   = new Color(48, 50, 68);
        private static readonly Color RowNormalBg  = new Color(25, 27, 37);
        private static readonly Color RowHoverBg   = new Color(38, 42, 58);
        private static readonly Color RowSelectBg  = new Color(50, 56, 80);
        private static readonly Color TextWhite    = new Color(248, 248, 242);
        private static readonly Color TextMuted    = new Color(130, 135, 160);
        private static readonly Color AccentRed    = new Color(255, 85, 85);
        private static readonly Color AccentGreen  = new Color(80, 250, 123);
        private static readonly Color AccentCyan   = new Color(139, 233, 253);
        private static readonly Color AccentYellow = new Color(241, 250, 140);
        private static readonly Color AccentOrange = new Color(255, 184, 108);

        private readonly Action<EditorDiagnostic> _onDiagnosticSelected;
        private readonly FlowLayoutWidget _rowsFlow;
        private readonly TextWidget _badgeText;
        private readonly GuiWidget _badgeContainer;
        private readonly GuiWidget _statusDot;
        private ProblemRowWidget _selectedRow;

        public ProblemsPanelWidget(Action<EditorDiagnostic> onDiagnosticSelected)
        {
            _onDiagnosticSelected = onDiagnosticSelected;

            HAnchor = HAnchor.Stretch;
            VAnchor = VAnchor.Stretch;
            BackgroundColor = PanelBg;

            var mainLayout = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
            };
            AddChild(mainLayout);

            // ── 1. Header Bar ─────────────────────────────────────────────────────────
            var header = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 32,
                BackgroundColor = HeaderBg,
                Padding = new BorderDouble(10, 4),
            };

            // Status Indicator Dot
            _statusDot = new GuiWidget(10, 10)
            {
                VAnchor = VAnchor.Center,
                BackgroundColor = AccentGreen,
                Margin = new BorderDouble(0, 0, 8, 0),
            };
            header.AddChild(_statusDot);

            // Section Title
            var title = new TextWidget("PROBLEMS", pointSize: 10, textColor: TextWhite, bold: true)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 8, 0),
            };
            header.AddChild(title);

            // Count Badge
            _badgeContainer = new GuiWidget()
            {
                VAnchor = VAnchor.Center,
                BackgroundColor = new Color(AccentGreen, 35),
                Padding = new BorderDouble(8, 2),
            };
            _badgeText = new TextWidget("0 issues", pointSize: 9, textColor: AccentGreen, bold: true)
            {
                VAnchor = VAnchor.Center,
                AutoExpandBoundsToText = true,
            };
            _badgeContainer.AddChild(_badgeText);
            _badgeContainer.SetBoundsToEncloseChildren();
            header.AddChild(_badgeContainer);

            // Flexible separator
            header.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

            // Right-side hint
            var hint = new TextWidget("Click error to navigate", pointSize: 9, textColor: TextMuted)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 8, 0),
            };
            header.AddChild(hint);

            mainLayout.AddChild(header);

            // 1px Border line below header
            var sep = new GuiWidget
            {
                HAnchor = HAnchor.Stretch,
                Height = 1,
                BackgroundColor = BorderLine,
            };
            mainLayout.AddChild(sep);

            // ── 2. Scrollable Problem Items List ──────────────────────────────────────
            var scrollable = new ScrollableWidget(autoScroll: true)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
            };
            scrollable.ScrollArea.HAnchor = HAnchor.Stretch;

            _rowsFlow = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Fit,
            };
            scrollable.AddChild(_rowsFlow);
            mainLayout.AddChild(scrollable);

            // Show initial clean state
            ShowEmptyState();
        }

        public void Clear()
        {
            ShowEmptyState();
        }

        private void ShowEmptyState()
        {
            _rowsFlow.CloseChildren();
            _selectedRow = null;

            _statusDot.BackgroundColor = AccentGreen;
            _badgeContainer.BackgroundColor = new Color(AccentGreen, 30);
            _badgeText.TextColor = AccentGreen;
            _badgeText.Text = "0 issues";
            _badgeContainer.SetBoundsToEncloseChildren();

            var emptyRow = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 36,
                Padding = new BorderDouble(14, 8),
            };

            var checkIcon = new TextWidget("✓", pointSize: 11, textColor: AccentGreen, bold: true)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 8, 0),
            };
            emptyRow.AddChild(checkIcon);

            var msg = new TextWidget("No problems detected in CAD script — clean compile", pointSize: 10, textColor: TextMuted)
            {
                VAnchor = VAnchor.Center,
            };
            emptyRow.AddChild(msg);

            _rowsFlow.AddChild(emptyRow);
            _rowsFlow.Invalidate();
        }

        public void SetDiagnostics(List<EditorDiagnostic> diagnostics)
        {
            _rowsFlow.CloseChildren();
            _selectedRow = null;

            if (diagnostics == null || diagnostics.Count == 0)
            {
                ShowEmptyState();
                return;
            }

            int count = diagnostics.Count;
            bool hasErrors = diagnostics.Exists(d => !d.IsWarning);

            Color statusColor = hasErrors ? AccentRed : AccentYellow;
            string label = count == 1 ? "1 problem" : $"{count} problems";

            _statusDot.BackgroundColor = statusColor;
            _badgeContainer.BackgroundColor = new Color(statusColor, 40);
            _badgeText.TextColor = statusColor;
            _badgeText.Text = label;
            _badgeContainer.SetBoundsToEncloseChildren();

            for (int i = 0; i < diagnostics.Count; i++)
            {
                var diag = diagnostics[i];
                ProblemRowWidget row = null;
                row = new ProblemRowWidget(diag, d =>
                {
                    SelectRow(row);
                    _onDiagnosticSelected?.Invoke(d);
                });
                _rowsFlow.AddChild(row);
            }

            _rowsFlow.Invalidate();
        }

        private void SelectRow(ProblemRowWidget row)
        {
            if (_selectedRow != null && _selectedRow != row)
            {
                _selectedRow.IsSelected = false;
            }
            _selectedRow = row;
            if (_selectedRow != null)
            {
                _selectedRow.IsSelected = true;
            }
        }

        // ── Individual Problem Row Widget ─────────────────────────────────────────
        private class ProblemRowWidget : GuiWidget
        {
            public EditorDiagnostic Diagnostic { get; }
            private readonly Action<EditorDiagnostic> _onSelect;
            private bool _isSelected;

            public bool IsSelected
            {
                get => _isSelected;
                set
                {
                    _isSelected = value;
                    BackgroundColor = _isSelected ? RowSelectBg : RowNormalBg;
                    Invalidate();
                }
            }

            public ProblemRowWidget(EditorDiagnostic diag, Action<EditorDiagnostic> onSelect)
            {
                Diagnostic = diag;
                _onSelect   = onSelect;

                HAnchor = HAnchor.Stretch;
                Height  = 34;
                BackgroundColor = RowNormalBg;
                Cursor = Cursors.Hand;
                Margin = new BorderDouble(0, 1);

                MouseEnterBounds += (s, e) =>
                {
                    if (!_isSelected)
                    {
                        BackgroundColor = RowHoverBg;
                        Invalidate();
                    }
                };

                MouseLeaveBounds += (s, e) =>
                {
                    if (!_isSelected)
                    {
                        BackgroundColor = RowNormalBg;
                        Invalidate();
                    }
                };

                MouseDown += (s, e) =>
                {
                    _onSelect?.Invoke(Diagnostic);
                };

                var rowLayout = new FlowLayoutWidget(FlowDirection.LeftToRight)
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                    Padding = new BorderDouble(10, 4),
                };
                AddChild(rowLayout);

                // 1. Error Code / Severity Badge
                Color badgeBg;
                Color badgeFg;
                string badgeCode = diag.Id;

                if (diag.IsRuntime)
                {
                    badgeBg = new Color(AccentOrange, 40);
                    badgeFg = AccentOrange;
                    badgeCode = "RUNTIME";
                }
                else if (diag.IsWarning)
                {
                    badgeBg = new Color(AccentYellow, 40);
                    badgeFg = AccentYellow;
                }
                else
                {
                    badgeBg = new Color(AccentRed, 45);
                    badgeFg = AccentRed;
                }

                var codeBadge = new GuiWidget()
                {
                    VAnchor = VAnchor.Center,
                    BackgroundColor = badgeBg,
                    Padding = new BorderDouble(6, 2),
                    Margin = new BorderDouble(0, 0, 8, 0),
                };
                var codeText = new TextWidget(badgeCode, pointSize: 9, textColor: badgeFg, bold: true)
                {
                    VAnchor = VAnchor.Center,
                    AutoExpandBoundsToText = true,
                };
                codeBadge.AddChild(codeText);
                codeBadge.SetBoundsToEncloseChildren();
                rowLayout.AddChild(codeBadge);

                // 2. Message Description
                var msgText = new TextWidget(diag.Message, pointSize: 10, textColor: TextWhite)
                {
                    VAnchor = VAnchor.Center,
                    AutoExpandBoundsToText = true,
                    Margin = new BorderDouble(0, 0, 10, 0),
                };
                rowLayout.AddChild(msgText);

                // Flexible gap to push location to the right
                rowLayout.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

                // 3. Location Pill (Line, Column)
                if (!diag.IsRuntime)
                {
                    string locStr = $"Ln {diag.Line + 1}, Col {diag.Column + 1}";
                    var locPill = new GuiWidget()
                    {
                        VAnchor = VAnchor.Center,
                        BackgroundColor = new Color(AccentCyan, 25),
                        Padding = new BorderDouble(6, 2),
                    };
                    var locText = new TextWidget(locStr, pointSize: 9, textColor: AccentCyan, bold: true)
                    {
                        VAnchor = VAnchor.Center,
                        AutoExpandBoundsToText = true,
                    };
                    locPill.AddChild(locText);
                    locPill.SetBoundsToEncloseChildren();
                    rowLayout.AddChild(locPill);
                }
            }
        }
    }
}
