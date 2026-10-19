using System;
using System.Collections.Generic;
using System.Linq;
using MatterHackers.Agg;
using MatterHackers.Agg.Font;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.VertexSource;
using MatterHackers.VectorMath;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CSharpCAD
{
    // ── Dracula-inspired colour palette ─────────────────────────────────────────
    public static class EditorPalette
    {
        public static readonly Color Background   = new Color( 40,  42,  54);
        public static readonly Color GutterBg     = new Color( 33,  34,  44);
        public static readonly Color GutterFg     = new Color( 98, 114, 164);
        public static readonly Color GutterFgCur  = new Color(248, 248, 242);
        public static readonly Color GutterBorder = new Color( 68,  71,  90);
        public static readonly Color DefaultText  = new Color(248, 248, 242);  // white

        // token colours
        public static readonly Color Keyword   = new Color( 80, 250, 123);   // green
        public static readonly Color TypeName  = new Color( 80, 200, 255);   // cyan
        public static readonly Color Comment   = new Color( 98, 114, 164);   // muted
        public static readonly Color StringLit = new Color(241, 250, 140);   // yellow
        public static readonly Color NumLit    = new Color(189, 147, 249);   // purple
        public static readonly Color Punct     = new Color(248, 248, 242);   // same as default
        public static readonly Color Operator  = new Color(255, 121, 198);   // pink
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 1. Syntax-aware inner text-edit widget
    // ────────────────────────────────────────────────────────────────────────────
    public class SyntaxHighlightInternalTextEditWidget : InternalTextEditWidget
    {
        // Cache for parsed tokens — rebuilt when text changes
        private string _cachedText;
        private List<(int start, int length, Color color)> _tokens = new();

        public SyntaxHighlightInternalTextEditWidget(
            string text, double pointSize, bool multiLine, int tabIndex)
            : base(text, pointSize, multiLine, tabIndex)
        {
            BackgroundColor = EditorPalette.Background;
            // Make base text invisible — we redraw everything in OnDraw with syntax colors
            TextColor = Color.Transparent;
            // Restore cursor and selection visibility (they are derived from TextColor in the base class)
            CursorColor = EditorPalette.DefaultText;
            HighlightColor = new Color(EditorPalette.DefaultText, 100);

            RebuildTokens(text);
            TextChanged += (s, e) => RebuildTokens(Text);
        }

        // ── token classification ─────────────────────────────────────────────────
        private void RebuildTokens(string source)
        {
            _cachedText = source;
            _tokens.Clear();
            if (string.IsNullOrEmpty(source)) return;

            try
            {
                var tree = CSharpSyntaxTree.ParseText(source);
                var root = tree.GetRoot();

                // First add a default-colored span for the entire source
                // so all text gets drawn (gaps between colored tokens)
                _tokens.Add((0, source.Length, EditorPalette.DefaultText));

                foreach (var token in root.DescendantTokens())
                {
                    foreach (var trivia in token.LeadingTrivia)
                        ClassifyTrivia(trivia);

                    Color col = ClassifyToken(token);
                    if (col != EditorPalette.DefaultText)
                        _tokens.Add((token.SpanStart, token.Span.Length, col));

                    foreach (var trivia in token.TrailingTrivia)
                        ClassifyTrivia(trivia);
                }
            }
            catch { /* parse errors are fine — just show no highlight */ }
        }

        private void ClassifyTrivia(SyntaxTrivia trivia)
        {
            var kind = trivia.Kind();
            if (kind == SyntaxKind.SingleLineCommentTrivia ||
                kind == SyntaxKind.MultiLineCommentTrivia  ||
                kind == SyntaxKind.SingleLineDocumentationCommentTrivia ||
                kind == SyntaxKind.MultiLineDocumentationCommentTrivia)
            {
                _tokens.Add((trivia.SpanStart, trivia.Span.Length, EditorPalette.Comment));
            }
        }

        private static Color ClassifyToken(SyntaxToken token)
        {
            var kind = token.Kind();

            if (SyntaxFacts.IsKeywordKind(kind) || SyntaxFacts.IsReservedKeyword(kind))
                return EditorPalette.Keyword;

            if (kind == SyntaxKind.StringLiteralToken ||
                kind == SyntaxKind.InterpolatedStringTextToken ||
                kind == SyntaxKind.CharacterLiteralToken ||
                kind == SyntaxKind.InterpolatedStringStartToken ||
                kind == SyntaxKind.InterpolatedStringEndToken)
                return EditorPalette.StringLit;

            if (kind == SyntaxKind.NumericLiteralToken)
                return EditorPalette.NumLit;

            if (SyntaxFacts.IsAnyOverloadableOperator(kind) ||
                kind == SyntaxKind.PlusToken   || kind == SyntaxKind.MinusToken ||
                kind == SyntaxKind.AsteriskToken || kind == SyntaxKind.SlashToken ||
                kind == SyntaxKind.EqualsToken || kind == SyntaxKind.EqualsEqualsToken ||
                kind == SyntaxKind.ExclamationEqualsToken)
                return EditorPalette.Operator;

            // Identifiers that start with uppercase → treat as type names
            if (kind == SyntaxKind.IdentifierToken)
            {
                var text = token.ValueText;
                if (text.Length > 0 && char.IsUpper(text[0]))
                    return EditorPalette.TypeName;
            }

            return EditorPalette.DefaultText;
        }

        private List<EditorDiagnostic> _diagnostics = new();

        public void SetDiagnostics(List<EditorDiagnostic> diagnostics)
        {
            _diagnostics = diagnostics ?? new();
            Invalidate();
        }

        public override void OnDraw(Graphics2D g)
        {
            // base.OnDraw draws: background, selection highlight, cursor, then child TextWidget
            // The child TextWidget is now invisible (TextColor == Background).
            // We redraw all text here with syntax colors.
            base.OnDraw(g);

            if (_cachedText != Text)
                RebuildTokens(Text);

            var printer = Printer;
            double lineH = printer.TypeFaceStyle.EmSizeInPixels;
            string src   = Text ?? "";

            // ── Draw Full-Line Error / Warning Highlight across the entire editor width ──
            if (_diagnostics != null && _diagnostics.Count > 0 && src != null)
            {
                var drawnLines = new HashSet<int>();
                foreach (var diag in _diagnostics)
                {
                    if (diag.Line < 0) continue;
                    if (!drawnLines.Add(diag.Line)) continue;

                    // Locate character index at the start of diag.Line
                    int targetLine = diag.Line;
                    int curLine = 0;
                    int lineStartIdx = 0;
                    for (int i = 0; i < src.Length && curLine < targetLine; i++)
                    {
                        if (src[i] == '\n')
                        {
                            curLine++;
                            lineStartIdx = i + 1;
                        }
                    }

                    if (curLine == targetLine && lineStartIdx <= src.Length)
                    {
                        var off = printer.GetOffsetLeftOfCharacterIndex(Math.Min(lineStartIdx, src.Length));
                        double drawY = Height + off.Y - printer.TypeFaceStyle.AscentInPixels;
                        double boxY = drawY + printer.TypeFaceStyle.DescentInPixels;

                        var lineBg = diag.IsWarning
                            ? new Color(241, 250, 140, 35)
                            : new Color(255, 60, 60, 48);

                        // Highlight full width of the editor line
                        double fullWidth = Math.Max(Width, 3000);
                        g.FillRectangle(0, boxY, fullWidth, boxY + lineH, lineBg);

                        // Left vertical accent stripe on the edge of the editor text
                        var stripeColor = diag.IsWarning
                            ? new Color(241, 250, 140, 220)
                            : new Color(255, 80, 80, 240);
                        g.FillRectangle(0, boxY, 3, boxY + lineH, stripeColor);
                    }
                }
            }

            if (_tokens.Count == 0) return;

            // Sort tokens by start so later (more specific) tokens overwrite earlier ones
            // Already ordered: default span first, then specific tokens on top.
            foreach (var (start, length, color) in _tokens)
            {
                if (start < 0 || start + length > src.Length || length <= 0) continue;

                // Walk through the token text, splitting on newlines
                int segStart = start;
                while (segStart < start + length)
                {
                    int segEnd = segStart;
                    while (segEnd < start + length && src[segEnd] != '\n')
                        segEnd++;

                    if (segEnd > segStart)
                    {
                        var off = printer.GetOffsetLeftOfCharacterIndex(segStart);
                        // Align perfectly with the invisible base text. 
                        // The baseline in InternalTextEditWidget space is Height + off.Y - Ascent.
                        double drawY = Height + off.Y - printer.TypeFaceStyle.AscentInPixels;

                        string seg = src.Substring(segStart, segEnd - segStart);
                        g.DrawString(seg, off.X, drawY, pointSize: printer.TypeFaceStyle.EmSizeInPoints, color: color);
                    }

                    // Skip the newline
                    segStart = segEnd < start + length ? segEnd + 1 : segEnd;
                }
            }

            // ── Draw Error squiggly underlines & soft highlight boxes on exact spots ──
            if (_diagnostics != null && _diagnostics.Count > 0)
            {
                foreach (var diag in _diagnostics)
                {
                    if (diag.StartIndex < 0 || diag.StartIndex >= src.Length || diag.Length <= 0) continue;

                    int spanEnd = Math.Min(src.Length, diag.StartIndex + diag.Length);
                    int curStart = diag.StartIndex;

                    while (curStart < spanEnd)
                    {
                        int curEnd = curStart;
                        while (curEnd < spanEnd && src[curEnd] != '\n' && src[curEnd] != '\r')
                            curEnd++;

                        if (curEnd > curStart)
                        {
                            var off1 = printer.GetOffsetLeftOfCharacterIndex(curStart);
                            var off2 = printer.GetOffsetLeftOfCharacterIndex(curEnd);
                            double drawY = Height + off1.Y - printer.TypeFaceStyle.AscentInPixels;
                            double x1 = off1.X;
                            double x2 = Math.Max(x1 + 6, off2.X);

                            // 1. Soft highlight background box
                            double boxY = drawY + printer.TypeFaceStyle.DescentInPixels;
                            var bgTint = diag.IsWarning
                                ? new Color(241, 250, 140, 40)
                                : new Color(255, 85, 85, 45);
                            g.FillRectangle(x1, boxY, x2, boxY + lineH, bgTint);

                            // 2. Red / warning squiggly underline (sawtooth wave)
                            double waveY = drawY - 2;
                            var waveColor = diag.IsWarning
                                ? new Color(241, 250, 140, 240)
                                : new Color(255, 85, 85, 240);
                            bool waveUp = false;
                            for (double wx = x1; wx < x2; wx += 3)
                            {
                                double nextWx = Math.Min(x2, wx + 3);
                                double yA = waveUp ? waveY + 2.5 : waveY;
                                double yB = waveUp ? waveY : waveY + 2.5;
                                g.Line(wx, yA, nextWx, yB, waveColor, 1.5);
                                waveUp = !waveUp;
                            }
                        }

                        // Skip newline
                        curStart = (curEnd < spanEnd && (src[curEnd] == '\n' || src[curEnd] == '\r')) ? curEnd + 1 : curEnd;
                        if (curStart < spanEnd && src[curStart - 1] == '\r' && src[curStart] == '\n')
                            curStart++;
                    }
                }
            }
        }

        public override void OnKeyDown(KeyEventArgs keyEvent)
        {
            if (keyEvent.Control && (keyEvent.KeyCode == Keys.C || keyEvent.KeyCode == Keys.Insert))
            {
                string toCopy = "";
                if (Selecting)
                {
                    int start = Math.Min(CharIndexToInsertBefore, SelectionIndexToStartBefore);
                    int end = Math.Max(CharIndexToInsertBefore, SelectionIndexToStartBefore);
                    string all = Text ?? "";
                    start = Math.Max(0, Math.Min(start, all.Length));
                    end = Math.Max(0, Math.Min(end, all.Length));
                    if (end > start)
                    {
                        toCopy = all.Substring(start, end - start);
                    }
                }
                else
                {
                    toCopy = Text ?? "";
                }

                if (!string.IsNullOrEmpty(toCopy))
                {
                    HelpWindow.CopyToClipboard(toCopy);
                    keyEvent.Handled = true;
                    keyEvent.SuppressKeyPress = true;
                    return;
                }
            }

            base.OnKeyDown(keyEvent);
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 2. TextEditWidget wrapper that uses the syntax-aware inner widget
    // ────────────────────────────────────────────────────────────────────────────
    public class SyntaxTextEditWidget : TextEditWidget
    {
        public SyntaxTextEditWidget(string text, double pointSize = 16, bool readOnly = false)
        {
            // The base constructor already created an InternalTextEditWidget and added it.
            // We must remove it to avoid double-rendering and layout confusion.
            if (InternalTextEditWidget != null)
            {
                ScrollArea.RemoveChild(InternalTextEditWidget);
            }

            // Replace with our syntax-aware one
            InternalTextEditWidget = new SyntaxHighlightInternalTextEditWidget(
                text, pointSize, multiLine: true, tabIndex: 0);

            if (readOnly)
            {
                InternalTextEditWidget.ReadOnly = true;
            }

            // Configure Dracula scrollbar styling globally for editor views
            ScrollBar.ScrollBarWidth = 12;
            ScrollBar.DefaultBackgroundColor = new Color(24, 25, 34, 160);
            ScrollBar.DefaultThumbColor = new Color(98, 114, 164, 180);
            ScrollBar.DefaultThumbHoverColor = new Color(139, 233, 253, 230);
            ScrollBar.GrowThumbBy = 0;

            // Re-hook it (this adds it back to the ScrollArea)
            HookUpToInternalWidget(0, 0);

            // Enable multiline, auto-scrolling, and vertical scrollbar
            Multiline = true;
            AutoScroll = true;
            VerticalScrollBar.Show = ScrollBar.ShowState.WhenRequired;

            if (VerticalScrollBar.Children.Count >= 2)
            {
                VerticalScrollBar.Children[0].BackgroundColor = new Color(24, 25, 34, 160);
                VerticalScrollBar.Children[1].BackgroundColor = new Color(98, 114, 164, 180);
            }

            // The InternalTextEditWidget manages its own size in UpdateLocalBounds.
            // We should not set Fit/Stretch anchors here as it can interfere with the ScrollArea's own Fit logic.
            InternalTextEditWidget.HAnchor = HAnchor.Absolute;
            InternalTextEditWidget.VAnchor = VAnchor.Absolute;

            BackgroundColor = EditorPalette.Background;
            
            // Default to top
            TopLeftOffset = Vector2.Zero;

            TextChanged += (s, e) =>
            {
                ScrollArea.Invalidate();
                VerticalScrollBar.Invalidate();
            };
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 3. Line-number gutter
    // ────────────────────────────────────────────────────────────────────────────
    public class LineNumberGutter : GuiWidget
    {
        public const double GutterWidth = 54;

        private readonly SyntaxTextEditWidget _editor;
        private double _fontSize;

        public LineNumberGutter(SyntaxTextEditWidget editor, double fontSize)
        {
            _editor   = editor;
            _fontSize = fontSize;

            Width     = GutterWidth;
            HAnchor   = HAnchor.Absolute;
            VAnchor   = VAnchor.Stretch;
            BackgroundColor = EditorPalette.GutterBg;
        }

        private List<EditorDiagnostic> _diagnostics = new();

        public void SetDiagnostics(List<EditorDiagnostic> diagnostics)
        {
            _diagnostics = diagnostics ?? new();
            Invalidate();
        }

        public override void OnMouseWheel(MouseEventArgs mouseEvent)
        {
            _editor?.OnMouseWheel(mouseEvent);
            if (mouseEvent.WheelDelta == 0)
            {
                Invalidate();
            }
        }

        public override void OnDraw(Graphics2D g)
        {
            base.OnDraw(g);

            g.FillRectangle(LocalBounds, EditorPalette.GutterBg);
            g.Line(GutterWidth - 1, 0, GutterWidth - 1, Height, EditorPalette.GutterBorder, 1);

            string text  = _editor.Text ?? "";
            var printer  = _editor.InternalTextEditWidget.Printer;
            double lineH = printer.TypeFaceStyle.EmSizeInPixels;

            HashSet<int> errorLines = new();
            HashSet<int> warningLines = new();
            if (_diagnostics != null)
            {
                foreach (var d in _diagnostics)
                {
                    if (d.Line >= 0)
                    {
                        if (d.IsWarning)
                            warningLines.Add(d.Line);
                        else
                            errorLines.Add(d.Line);
                    }
                }
            }

            // Find current cursor line
            int cursorLine = 0;
            int insertIdx  = _editor.InternalTextEditWidget.CharIndexToInsertBefore;
            for (int i = 0; i < Math.Min(insertIdx, text.Length); i++)
                if (text[i] == '\n') cursorLine++;

            // Walk through line starts and use the printer's own coordinate
            // system (same used for cursor drawing in InternalTextEditWidget.OnDraw):
            //   drawY = ITE.Height + printerOffset.Y - lineH
            double iteHeight = _editor.InternalTextEditWidget.Height;
            double scrollY   = _editor.TopLeftOffset.Y;

            int lineNum   = 0;
            int charIndex = 0;

            while (charIndex <= text.Length)
            {
                // Get Y for this line using printer
                var off = printer.GetOffsetLeftOfCharacterIndex(charIndex);
                
                // The baseline in the gutter must match the baseline in the editor view.
                // We use the same math as the SyntaxHighlightInternalTextEditWidget but add the scroll offset.
                double ascent = printer.TypeFaceStyle.AscentInPixels;
                double scrollShift = _editor.ScrollArea.OriginRelativeParent.Y;
                double baselineY = iteHeight + off.Y - ascent + scrollShift;

                // Only draw if within the visible gutter
                if (baselineY + lineH >= 0 && baselineY - lineH <= Height)
                {
                    bool hasError   = errorLines.Contains(lineNum);
                    bool hasWarning = !hasError && warningLines.Contains(lineNum);
                    bool isCurrent  = (lineNum == cursorLine);

                    double barY = Math.Ceiling(baselineY + printer.TypeFaceStyle.DescentInPixels);

                    if (hasError)
                    {
                        // 1. Soft red background across gutter line
                        g.FillRectangle(0, barY, GutterWidth - 1, barY + lineH, new Color(255, 60, 60, 55));

                        // 2. Bold red left margin stripe
                        g.FillRectangle(0, barY, 4, barY + lineH, new Color(255, 80, 80, 240));

                        // 3. Crisp error badge near line number
                        double dotY = baselineY + lineH * 0.35;
                        double dotX = 10;
                        g.Circle(dotX, dotY, 4.5, new Color(255, 75, 75));
                        g.Circle(dotX, dotY, 2.0, new Color(255, 255, 255));
                    }
                    else if (hasWarning)
                    {
                        g.FillRectangle(0, barY, GutterWidth - 1, barY + lineH, new Color(241, 250, 140, 40));
                        g.FillRectangle(0, barY, 4, barY + lineH, new Color(241, 250, 140, 220));

                        double dotY = baselineY + lineH * 0.35;
                        double dotX = 10;
                        g.Circle(dotX, dotY, 4.0, new Color(241, 250, 140));
                    }
                    else if (isCurrent)
                    {
                        // Match the selection highlight box exactly
                        g.FillRectangle(0, barY, GutterWidth - 1, barY + lineH,
                                        new Color(60, 63, 80, 120));
                    }

                    Color numCol = hasError 
                        ? new Color(255, 110, 110) 
                        : (hasWarning ? new Color(241, 250, 140) : (isCurrent ? EditorPalette.GutterFgCur : EditorPalette.GutterFg));

                    string numStr = (lineNum + 1).ToString();
                    double charW  = printer.TypeFaceStyle.GetAdvanceForCharacter('0');
                    double drawX  = GutterWidth - 10 - charW * numStr.Length;
                    // Slightly larger font for line numbers
                    g.DrawString(numStr, drawX, Math.Ceiling(baselineY), _editor.InternalTextEditWidget.Printer.TypeFaceStyle.EmSizeInPoints * 0.85, color: numCol);
                }

                // Advance to next line
                int nextNewline = text.IndexOf('\n', charIndex);
                if (nextNewline < 0) break;
                charIndex = nextNewline + 1;
                lineNum++;
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 3.5. Autocomplete Popup Widget
    // ────────────────────────────────────────────────────────────────────────────
    // ────────────────────────────────────────────────────────────────────────────
    // 3.5. Autocomplete Item & Popup Widget
    // ────────────────────────────────────────────────────────────────────────────
    public class AutocompleteListItem : ListBoxTextItem
    {
        public RoslynCompletionItem RoslynItem { get; }

        public AutocompleteListItem(RoslynCompletionItem item)
            : base(FormatLabel(item), item.InsertionText ?? item.DisplayText)
        {
            RoslynItem = item;
            TextColor = Color.White;
            Height = 20;
            Selectable = true;
            Cursor = Cursors.Hand;
            HAnchor = HAnchor.Stretch;
        }

        private static string FormatLabel(RoslynCompletionItem item)
        {
            string tag = item.Kind switch
            {
                "method" => " [m] ",
                "constructor" => " [c] ",
                "property" => " [p] ",
                "field" => " [f] ",
                "class" => " [c] ",
                "struct" => " [s] ",
                "enum" => " [e] ",
                "interface" => " [i] ",
                "keyword" => " [k] ",
                "local" or "variable" => " [v] ",
                "parameter" => " [a] ",
                _ => " [ ] "
            };

            if (!string.IsNullOrEmpty(item.InlineDescription) && item.Kind != "constructor" && item.Kind != "method")
                return tag + item.DisplayText + "  (" + item.InlineDescription + ")";

            return tag + item.DisplayText;
        }
    }

    public class AutocompleteWidget : GuiWidget
    {
        public event Action<RoslynCompletionItem> ItemChosen;

        private readonly ListBox _listBox;
        private readonly TextWidget _descriptionWidget;
        private readonly SyntaxTextEditWidget _editor;
        private List<RoslynCompletionItem> _currentItems = new();
        private long _lastClickTicks = 0;

        public AutocompleteWidget(SyntaxTextEditWidget editor)
        {
            _editor = editor;
            Width = 480;
            Height = 160;
            BackgroundColor = new Color(40, 42, 54);
            Visible = false;

            var mainLayout = new FlowLayoutWidget(FlowDirection.TopToBottom)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch
            };

            // Suggestions List
            _listBox = new ListBox(new RectangleDouble(0, 0, 480, 130))
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch
            };
            _listBox.SelectedValueChanged += (s, e) => UpdateDescription();
            _listBox.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    if (_listBox.VerticalScrollBar != null && _listBox.VerticalScrollBar.Visible)
                    {
                        if (e.X >= _listBox.Width - _listBox.VerticalScrollBar.Width - 6)
                        {
                            return;
                        }
                    }
                    var item = GetSelectedItem();
                    if (item != null)
                    {
                        OnItemClicked(item);
                    }
                }
            };
            mainLayout.AddChild(_listBox);

            // Description footer bar
            var footer = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                Height = 24,
                BackgroundColor = new Color(30, 31, 41),
                Padding = new BorderDouble(6, 2)
            };
            _descriptionWidget = new TextWidget("", pointSize: 9, textColor: new Color(139, 233, 253))
            {
                VAnchor = VAnchor.Center,
                AutoExpandBoundsToText = true
            };
            footer.AddChild(_descriptionWidget);
            mainLayout.AddChild(footer);

            AddChild(mainLayout);
        }

        public override void OnDraw(Graphics2D g)
        {
            base.OnDraw(g);
            // Draw crisp popup border
            g.Rectangle(LocalBounds, new Color(68, 71, 90), 1);
        }

        public void Populate(List<RoslynCompletionItem> items, string prefix)
        {
            _currentItems.Clear();
            if (_listBox.ScrollArea.Children.Count > 0)
            {
                _listBox.ScrollArea.Children[0].CloseChildren();
            }

            prefix ??= string.Empty;

            // Filter & prioritize exact prefix matches
            var matches = new List<RoslynCompletionItem>();
            var secondaryMatches = new List<RoslynCompletionItem>();

            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(prefix))
                {
                    matches.Add(item);
                }
                else if (item.DisplayText.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(item);
                }
                else if (item.DisplayText.IndexOf(prefix, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    secondaryMatches.Add(item);
                }

                if (matches.Count >= 30) break;
            }

            if (matches.Count < 30)
            {
                matches.AddRange(secondaryMatches.Take(30 - matches.Count));
            }

            if (matches.Count == 0)
            {
                Visible = false;
                return;
            }

            _currentItems = matches;
            int itemIdx = 0;
            foreach (var match in matches)
            {
                int capturedIdx = itemIdx;
                var listItem = new AutocompleteListItem(match);

                listItem.MouseDown += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        _listBox.SelectedIndex = capturedIdx;
                    }
                };

                listItem.MouseUp += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        OnItemClicked(match);
                    }
                };

                listItem.Click += (s, e) =>
                {
                    if (e.Button == MouseButtons.Left)
                    {
                        OnItemClicked(match);
                    }
                };

                _listBox.AddChild(listItem);

                var holder = listItem.Parent;
                if (holder != null)
                {
                    holder.Cursor = Cursors.Hand;
                    holder.MouseUp += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            OnItemClicked(match);
                        }
                    };
                    holder.Click += (s, e) =>
                    {
                        if (e.Button == MouseButtons.Left)
                        {
                            OnItemClicked(match);
                        }
                    };
                }

                itemIdx++;
            }

            _listBox.SelectedIndex = 0;
            UpdateDescription();
            Visible = true;
            Invalidate();
        }

        private void OnItemClicked(RoslynCompletionItem item)
        {
            if (item == null) return;
            long now = DateTime.UtcNow.Ticks;
            if (now - _lastClickTicks < TimeSpan.FromMilliseconds(200).Ticks)
            {
                return;
            }
            _lastClickTicks = now;
            ItemChosen?.Invoke(item);
        }

        private void UpdateDescription()
        {
            var item = GetSelectedItem();
            if (item == null)
            {
                _descriptionWidget.Text = "";
                return;
            }

            int idx = _listBox.SelectedIndex + 1;
            int total = _currentItems.Count;
            string countPrefix = (item.Kind == "constructor" || item.Kind == "method") && total > 1
                ? $" ({idx} of {total})"
                : "";

            string text = item.Kind.ToUpperInvariant() + countPrefix + ": " + item.DisplayText;
            if (!string.IsNullOrEmpty(item.InlineDescription))
            {
                text += " — " + item.InlineDescription;
            }
            _descriptionWidget.Text = text;
        }

        public void SelectNext() 
        { 
            if (_listBox.SelectedIndex < _listBox.Count - 1) 
                _listBox.SelectedIndex++; 
        }
        
        public void SelectPrev() 
        { 
            if (_listBox.SelectedIndex > 0) 
                _listBox.SelectedIndex--; 
        }

        public RoslynCompletionItem GetSelectedItem()
        {
            int idx = _listBox.SelectedIndex;
            if (idx >= 0 && idx < _currentItems.Count)
            {
                return _currentItems[idx];
            }
            return null;
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 4. The public-facing CodeEditorWidget
    // ────────────────────────────────────────────────────────────────────────────
    public class CodeEditorWidget : GuiWidget
    {
        public SyntaxTextEditWidget Editor { get; }
        private readonly LineNumberGutter _gutter;
        private readonly AutocompleteWidget _autocomplete;
        private readonly RoslynCompletionEngine _completionEngine;
        private System.Threading.CancellationTokenSource _cts;

        public new string Text
        {
            get => Editor.Text;
            set => Editor.Text = value;
        }

        public new event EventHandler TextChanged;

        public CodeEditorWidget(string initialText = "", double fontSize = 16)
        {
            HAnchor         = HAnchor.Stretch;
            VAnchor         = VAnchor.Stretch;
            BackgroundColor = EditorPalette.Background;

            _completionEngine = new RoslynCompletionEngine();

            // Pre-warm the Roslyn MEF engine in background
            System.Threading.Tasks.Task.Run(() => _completionEngine.GetCompletionsAsync("", 0));

            Editor = new SyntaxTextEditWidget(initialText, fontSize)
            {
                Multiline = true,
                HAnchor   = HAnchor.Stretch,
                VAnchor   = VAnchor.Stretch,
            };

            _gutter = new LineNumberGutter(Editor, fontSize);

            // Sync gutter scroll + invalidate whenever editor changes or scrolls
            Editor.TextChanged += (s, e) =>
            {
                _gutter.Invalidate();
                TextChanged?.Invoke(this, e);
            };

            // Sync gutter scroll
            Editor.ScrollPositionChanged += (s, e) => _gutter.Invalidate();

            // Redraw gutter when cursor moves (line highlight)
            Editor.InternalTextEditWidget.InsertBarPositionChanged += (s, e) =>
                _gutter.Invalidate();

            // Horizontal layout: gutter | editor
            var row = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
            };
            row.AddChild(_gutter);
            row.AddChild(Editor);
            AddChild(row);

            // Add autocomplete popup as a top-level overlay
            _autocomplete = new AutocompleteWidget(Editor);
            _autocomplete.ItemChosen += item => ApplyAutocomplete(item);
            AddChild(_autocomplete);

            // Hook events for autocomplete
            Editor.InternalTextEditWidget.KeyDown += Editor_KeyDown;
            Editor.InternalTextEditWidget.KeyUp += Editor_KeyUp;
            Editor.InternalTextEditWidget.MouseDown += (s, e) => _autocomplete.Visible = false;

            // Ensure we are at the top
            Editor.ScrollPosition = new Vector2(0, 0);
            Editor.TopLeftOffset = new Vector2(0, 0);
        }

        private void Editor_KeyDown(object sender, KeyEventArgs e)
        {
            // Ctrl+Space manual trigger for autocomplete
            bool isCtrl = (e.Modifiers & Keys.Control) != 0 || (e.Modifiers & Keys.Alt) != 0;
            if (isCtrl && e.KeyCode == Keys.Space)
            {
                TriggerAutocomplete(force: true);
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            if (_autocomplete.Visible)
            {
                if (e.KeyCode == Keys.Down) { _autocomplete.SelectNext(); e.Handled = true; }
                else if (e.KeyCode == Keys.Up) { _autocomplete.SelectPrev(); e.Handled = true; }
                else if (e.KeyCode == Keys.PageDown) { for (int i = 0; i < 5; i++) _autocomplete.SelectNext(); e.Handled = true; }
                else if (e.KeyCode == Keys.PageUp) { for (int i = 0; i < 5; i++) _autocomplete.SelectPrev(); e.Handled = true; }
                else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
                {
                    ApplyAutocomplete();
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    _autocomplete.Visible = false;
                    e.Handled = true;
                }
            }
        }

        private void Editor_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Handled) return;

            // Don't trigger on pure navigation keys
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down ||
                e.KeyCode == Keys.Left || e.KeyCode == Keys.Right ||
                e.KeyCode == Keys.Home || e.KeyCode == Keys.End ||
                e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown ||
                e.KeyCode == Keys.Escape || e.KeyCode == Keys.Enter || e.KeyCode == Keys.Tab)
            {
                return;
            }

            TriggerAutocomplete(force: false);
        }

        private static readonly HashSet<string> s_controlKeywords = new(StringComparer.Ordinal)
        {
            "if", "for", "foreach", "while", "switch", "catch", "using", "lock", "fixed", "sizeof", "typeof", "default"
        };

        private static bool CheckInvocation(string text, int pos, out string target, out bool isCtor, out int argIndex)
        {
            target = null;
            isCtor = false;
            argIndex = 0;

            if (string.IsNullOrEmpty(text) || pos <= 0 || pos > text.Length)
                return false;

            int depth = 0;
            int argCount = 0;
            int openParenPos = -1;

            for (int i = pos - 1; i >= 0; i--)
            {
                char c = text[i];
                if (c == ')')
                {
                    depth++;
                }
                else if (c == '(')
                {
                    if (depth > 0)
                    {
                        depth--;
                    }
                    else
                    {
                        openParenPos = i;
                        break;
                    }
                }
                else if (c == ',' && depth == 0)
                {
                    argCount++;
                }
                else if (c == ';' || c == '{' || c == '}')
                {
                    break;
                }
            }

            if (openParenPos < 0) return false;

            // Find identifier before open paren
            int endId = -1;
            int startId = -1;
            for (int i = openParenPos - 1; i >= 0; i--)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    if (endId != -1) { startId = i + 1; break; }
                    continue;
                }
                if (char.IsLetterOrDigit(text[i]) || text[i] == '_')
                {
                    if (endId == -1) endId = i;
                    if (i == 0) { startId = 0; break; }
                }
                else
                {
                    if (endId != -1) { startId = i + 1; break; }
                    break;
                }
            }

            if (endId == -1 || startId == -1) return false;

            string id = text.Substring(startId, endId - startId + 1);
            if (s_controlKeywords.Contains(id)) return false;

            // Check if preceded by "new"
            bool isNew = false;
            int beforeStart = startId - 1;
            while (beforeStart >= 0 && char.IsWhiteSpace(text[beforeStart]))
                beforeStart--;
            if (beforeStart >= 2 && text.Substring(beforeStart - 2, 3) == "new")
            {
                bool boundary = (beforeStart - 3 < 0) || !char.IsLetterOrDigit(text[beforeStart - 3]);
                if (boundary) isNew = true;
            }

            target = id;
            isCtor = isNew;
            argIndex = argCount;
            return true;
        }

        private async void TriggerAutocomplete(bool force)
        {
            string text = Editor.Text ?? "";
            int pos = Editor.InternalTextEditWidget.CharIndexToInsertBefore;

            string prefix = "";
            bool shouldTrigger = force;
            bool isSignatureTrigger = false;
            string invocationTarget = null;
            bool isConstructor = false;
            int currentArgIndex = 0;

            if (CheckInvocation(text, pos, out invocationTarget, out isConstructor, out currentArgIndex))
            {
                shouldTrigger = true;
                isSignatureTrigger = true;
            }
            else if (pos > 0 && pos <= text.Length)
            {
                char prev = text[pos - 1];
                if (prev == ')')
                {
                    _autocomplete.Visible = false;
                    return;
                }
                else if (prev == '.')
                {
                    // Member access trigger (e.g. "myBox.")
                    shouldTrigger = true;
                    prefix = "";
                }
                else if (char.IsLetterOrDigit(prev) || prev == '_')
                {
                    // Identifier trigger (e.g. "Bo", "myVa")
                    int start = pos - 1;
                    while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_'))
                        start--;

                    prefix = text.Substring(start, pos - start);
                    if (prefix.Length >= 1)
                    {
                        shouldTrigger = true;
                    }
                }
            }

            if (!shouldTrigger)
            {
                _autocomplete.Visible = false;
                return;
            }

            _cts?.Cancel();
            _cts = new System.Threading.CancellationTokenSource();
            var token = _cts.Token;

            try
            {
                List<RoslynCompletionItem> completions = null;
                if (isSignatureTrigger)
                {
                    completions = await _completionEngine.GetSignaturesAsync(
                        invocationTarget, isConstructor, currentArgIndex, text, pos, token);
                    prefix = "";
                }
                else
                {
                    completions = await _completionEngine.GetCompletionsAsync(text, pos, token);
                }

                if (token.IsCancellationRequested) return;

                if (completions != null && completions.Count > 0)
                {
                    _autocomplete.Populate(completions, prefix);
                    if (_autocomplete.Visible)
                    {
                        UpdateAutocompletePos();
                    }
                }
                else
                {
                    _autocomplete.Visible = false;
                }
            }
            catch (System.OperationCanceledException)
            {
                // Request superseded by newer keystroke
            }
            catch
            {
                _autocomplete.Visible = false;
            }
        }

        private void UpdateAutocompletePos()
        {
            var ite = Editor.InternalTextEditWidget;
            var pos = ite.InsertBarPosition;

            // Map editor-local cursor position to CodeEditorWidget coordinates
            Vector2 editorPos = ite.TransformToParentSpace(this, pos);
            
            double targetX = editorPos.X;
            double targetY = editorPos.Y - _autocomplete.Height - 5;

            // If popup would extend below the bottom boundary, place it above the cursor line
            if (targetY < 0)
            {
                double lineH = ite.Printer.TypeFaceStyle.EmSizeInPixels;
                targetY = editorPos.Y + lineH + 5;
            }

            // Keep within horizontal bounds
            if (targetX + _autocomplete.Width > Width)
            {
                targetX = Math.Max(0, Width - _autocomplete.Width - 10);
            }

            _autocomplete.OriginRelativeParent = new Vector2(targetX, targetY);
            _autocomplete.BringToFront();
        }

        private void ApplyAutocomplete(RoslynCompletionItem explicitItem = null)
        {
            var item = explicitItem ?? _autocomplete.GetSelectedItem();
            if (item == null) return;

            string selected = item.InsertionText ?? item.DisplayText;
            if (string.IsNullOrEmpty(selected)) return;

            string text = Editor.Text ?? "";
            int pos = Editor.InternalTextEditWidget.CharIndexToInsertBefore;

            // Handle signature items (constructor/method)
            if (item.Kind == "constructor" || item.Kind == "method")
            {
                int openParen = -1;
                for (int i = pos - 1; i >= 0; i--)
                {
                    if (text[i] == '(') { openParen = i; break; }
                    if (text[i] == ';' || text[i] == '\n') break;
                }

                if (openParen >= 0)
                {
                    int endName = openParen - 1;
                    while (endName >= 0 && char.IsWhiteSpace(text[endName])) endName--;
                    int startName = endName;
                    while (startName >= 0 && (char.IsLetterOrDigit(text[startName]) || text[startName] == '_')) startName--;
                    startName++;

                    if (endName >= startName)
                    {
                        int parenIdx = item.DisplayText.IndexOf('(');
                        string canonicalName = parenIdx > 0 ? item.DisplayText.Substring(0, parenIdx).Trim() : "";

                        string correctedPrefix = text.Substring(0, startName) + canonicalName + text.Substring(endName + 1, openParen - endName);

                        string insertion = selected;
                        if (pos < text.Length && text[pos] == ')' && insertion.EndsWith(")"))
                        {
                            insertion = insertion.Substring(0, insertion.Length - 1);
                        }

                        string resultText = correctedPrefix + insertion + text.Substring(pos);
                        Editor.Text = resultText;
                        Editor.InternalTextEditWidget.CharIndexToInsertBefore = correctedPrefix.Length + insertion.Length;
                        _autocomplete.Visible = false;
                        Editor.InternalTextEditWidget.Focus();
                        return;
                    }
                }
            }

            int start = pos;
            // Replace preceding identifier characters if typing a prefix
            while (start > 0 && (char.IsLetterOrDigit(text[start - 1]) || text[start - 1] == '_'))
                start--;

            string newText = text.Substring(0, start) + selected + text.Substring(pos);
            Editor.Text = newText;
            Editor.InternalTextEditWidget.CharIndexToInsertBefore = start + selected.Length;
            _autocomplete.Visible = false;
            Editor.InternalTextEditWidget.Focus();
        }

        /// <summary>Updates compilation/runtime diagnostics for squiggly highlights and gutter markers.</summary>
        public void SetDiagnostics(List<EditorDiagnostic> diagnostics)
        {
            if (Editor.InternalTextEditWidget is SyntaxHighlightInternalTextEditWidget syn)
            {
                syn.SetDiagnostics(diagnostics);
            }
            _gutter?.SetDiagnostics(diagnostics);
            Editor.Invalidate();
            InvalidateGutter();
        }

        /// <summary>
        /// Selects the exact error span in the editor, positions the cursor, and scrolls it into view.
        /// </summary>
        public void NavigateTo(int line, int col, int startIndex, int length)
        {
            string text = Editor.Text ?? "";
            if (startIndex < 0 || startIndex > text.Length)
            {
                int curLine = 0;
                int i = 0;
                while (i < text.Length && curLine < line)
                {
                    if (text[i] == '\n') curLine++;
                    i++;
                }
                startIndex = Math.Min(text.Length, i + Math.Max(0, col));
            }

            int end = Math.Min(text.Length, startIndex + Math.Max(1, length));

            // Set selection on the exact error range
            Editor.InternalTextEditWidget.SetSelection(startIndex, end - 1);
            Editor.InternalTextEditWidget.CharIndexToInsertBefore = startIndex;

            // Scroll so the error line is centered/visible in the editor
            var printer = Editor.InternalTextEditWidget.Printer;
            var off = printer.GetOffsetLeftOfCharacterIndex(startIndex);
            double targetScrollY = Math.Max(0, -off.Y - Editor.Height / 3);
            Editor.TopLeftOffset = new Vector2(0, targetScrollY);

            // Focus the editor
            Editor.InternalTextEditWidget.Focus();
            Editor.Invalidate();
            InvalidateGutter();
        }

        /// <summary>Called externally when the parent scrolls, to keep the gutter in sync.</summary>
        public void InvalidateGutter() =>
            (Children[0] as FlowLayoutWidget)?.Children[0]?.Invalidate();
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 6. Read-only syntax-highlighted code box widget (used in HelpWindow cards)
    // ────────────────────────────────────────────────────────────────────────────
    public class ReadOnlyCodeBoxWidget : GuiWidget
    {
        public SyntaxTextEditWidget Editor { get; }
        private readonly double _fontSize;
        private readonly double _maxDisplayHeight;

        public ReadOnlyCodeBoxWidget(string text, double fontSize = 10, double maxDisplayHeight = 260)
        {
            _fontSize = fontSize;
            _maxDisplayHeight = maxDisplayHeight;

            HAnchor = HAnchor.Stretch;
            Margin = new BorderDouble(0, 4, 0, 0);
            Padding = new BorderDouble(6, 4);
            BackgroundColor = EditorPalette.Background;

            Editor = new SyntaxTextEditWidget(text ?? "", fontSize, readOnly: true)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                BackgroundColor = EditorPalette.Background,
            };

            AddChild(Editor);
            UpdateHeight(text ?? "");
        }

        public override string Text
        {
            get => Editor?.Text ?? "";
            set
            {
                if (Editor != null)
                {
                    Editor.Text = value ?? "";
                    Editor.TopLeftOffset = Vector2.Zero;
                }
                UpdateHeight(value ?? "");
            }
        }

        public void UpdateHeight(string content)
        {
            int lineCount = 1;
            for (int i = 0; i < content.Length; i++)
            {
                if (content[i] == '\n') lineCount++;
            }

            double lineH = Math.Ceiling(_fontSize * 1.55);
            double neededHeight = Math.Max(38, lineCount * lineH + 16);
            Height = Math.Min(neededHeight, _maxDisplayHeight);
            if (Editor != null)
            {
                Editor.Height = Height;
                Editor.ScrollArea.Invalidate();
                Editor.VerticalScrollBar.Invalidate();
            }
            Invalidate();
        }

        public override void OnDraw(Graphics2D g)
        {
            base.OnDraw(g);
            // Crisp border matching Dracula comment/border
            g.Rectangle(LocalBounds, new Color(68, 71, 90));
        }
    }
}
