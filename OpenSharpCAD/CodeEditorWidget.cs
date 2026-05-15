using System;
using System.Collections.Generic;
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
    internal static class EditorPalette
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

        public override void OnDraw(Graphics2D g)
        {
            // base.OnDraw draws: background, selection highlight, cursor, then child TextWidget
            // The child TextWidget is now invisible (TextColor == Background).
            // We redraw all text here with syntax colors.
            base.OnDraw(g);

            if (_cachedText != Text)
                RebuildTokens(Text);

            if (_tokens.Count == 0) return;

            var printer = Printer;
            double lineH = printer.TypeFaceStyle.EmSizeInPixels;
            string src   = Text ?? "";

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
        }

        private double _pointSize;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // 2. TextEditWidget wrapper that uses the syntax-aware inner widget
    // ────────────────────────────────────────────────────────────────────────────
    public class SyntaxTextEditWidget : TextEditWidget
    {
        public SyntaxTextEditWidget(string text, double pointSize = 16)
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

            // Re-hook it (this adds it back to the ScrollArea)
            HookUpToInternalWidget(0, 0);

            // The InternalTextEditWidget manages its own size in UpdateLocalBounds.
            // We should not set Fit/Stretch anchors here as it can interfere with the ScrollArea's own Fit logic.
            InternalTextEditWidget.HAnchor = HAnchor.Absolute;
            InternalTextEditWidget.VAnchor = VAnchor.Absolute;

            BackgroundColor = EditorPalette.Background;
            
            // Default to top
            TopLeftOffset = Vector2.Zero;
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

        public override void OnDraw(Graphics2D g)
        {
            base.OnDraw(g);

            g.FillRectangle(LocalBounds, EditorPalette.GutterBg);
            g.Line(GutterWidth - 1, 0, GutterWidth - 1, Height, EditorPalette.GutterBorder, 1);

            string text  = _editor.Text ?? "";
            var printer  = _editor.InternalTextEditWidget.Printer;
            double lineH = printer.TypeFaceStyle.EmSizeInPixels;

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
                    bool isCurrent = (lineNum == cursorLine);
                    Color numCol   = isCurrent ? EditorPalette.GutterFgCur : EditorPalette.GutterFg;

                    if (isCurrent)
                    {
                        // Match the selection highlight box exactly
                        double barY = Math.Ceiling(baselineY + printer.TypeFaceStyle.DescentInPixels);
                        g.FillRectangle(0, barY, GutterWidth - 1, barY + lineH,
                                        new Color(60, 63, 80, 120));
                    }

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
    public class AutocompleteWidget : GuiWidget
    {
        private ListBox _listBox;
        private List<string> _allSuggestions;
        private SyntaxTextEditWidget _editor;

        public AutocompleteWidget(SyntaxTextEditWidget editor, List<string> suggestions)
        {
            _editor = editor;
            _allSuggestions = suggestions;

            BackgroundColor = new Color(40, 42, 54);

            _listBox = new ListBox(new RectangleDouble(0, 0, 180, 120));
            AddChild(_listBox);
            
            Width = 180;
            Height = 120;
            Visible = false;
        }

        public override void OnDraw(Graphics2D g)
        {
            base.OnDraw(g);
            g.Rectangle(LocalBounds, new Color(68, 71, 90));
        }

        public void Filter(string prefix)
        {
            if (_listBox.ScrollArea.Children.Count > 0)
            {
                _listBox.ScrollArea.Children[0].CloseChildren();
            }
            
            var matches = new List<string>();
            foreach (var s in _allSuggestions)
            {
                if (s.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    matches.Add(s);
                    if (matches.Count >= 20) break;
                }
            }

            if (matches.Count == 0)
            {
                Visible = false;
                return;
            }

            foreach (var match in matches)
            {
                _listBox.AddChild(new ListBoxTextItem(match, match) { 
                    TextColor = Color.White,
                    Height = 20
                });
            }

            _listBox.SelectedIndex = 0;
            Visible = true;
            Invalidate();
        }

        public void SelectNext() 
        { 
            if (_listBox.SelectedIndex < _listBox.Count - 1) _listBox.SelectedIndex++; 
        }
        
        public void SelectPrev() 
        { 
            if (_listBox.SelectedIndex > 0) _listBox.SelectedIndex--; 
        }

        public string GetSelected()
        {
            var item = _listBox.SelectedItem as FlowLayoutWidget;
            if (item != null && item.Children.Count > 0)
            {
                var textItem = item.Children[0] as ListBoxTextItem;
                return textItem?.ItemValue;
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
        private AutocompleteWidget _autocomplete;

        private static readonly string[] Keywords = {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
            "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
            "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
            "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
            "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
            "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
            "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw",
            "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while",
            // CAD Specific
            "Draw", "Box", "Sphere", "Cylinder", "Union", "Difference", "Intersection",
            "Rotate", "Translate", "Scale", "RGB", "RGBA", "RGBf", "Color", "Vector3", "CsgObject"
        };

        public string Text
        {
            get => Editor.Text;
            set => Editor.Text = value;
        }

        public event EventHandler TextChanged;

        public CodeEditorWidget(string initialText = "", double fontSize = 16)
        {
            HAnchor         = HAnchor.Stretch;
            VAnchor         = VAnchor.Stretch;
            BackgroundColor = EditorPalette.Background;

            Editor = new SyntaxTextEditWidget(initialText, fontSize)
            {
                Multiline = true,
                HAnchor   = HAnchor.Stretch,
                VAnchor   = VAnchor.Stretch,
            };

            var gutter = new LineNumberGutter(Editor, fontSize);

            // Sync gutter scroll + invalidate whenever editor changes or scrolls
            Editor.TextChanged += (s, e) =>
            {
                gutter.Invalidate();
                TextChanged?.Invoke(this, e);
            };

            // Sync gutter scroll
            Editor.ScrollPositionChanged += (s, e) => gutter.Invalidate();

            // Redraw gutter when cursor moves (line highlight)
            Editor.InternalTextEditWidget.InsertBarPositionChanged += (s, e) =>
                gutter.Invalidate();

            // Horizontal layout: gutter | editor
            var row = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
            };
            row.AddChild(gutter);
            row.AddChild(Editor);
            AddChild(row);

            // Add autocomplete popup as a top-level overlay
            var sortedKeywords = new List<string>(Keywords);
            sortedKeywords.Sort();
            _autocomplete = new AutocompleteWidget(Editor, sortedKeywords);
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
            if (_autocomplete.Visible)
            {
                if (e.KeyCode == Keys.Down) { _autocomplete.SelectNext(); e.Handled = true; }
                else if (e.KeyCode == Keys.Up) { _autocomplete.SelectPrev(); e.Handled = true; }
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

            // Simple word-based trigger
            string text = Editor.Text;
            int pos = Editor.InternalTextEditWidget.CharIndexToInsertBefore;

            if (pos > 0 && char.IsLetterOrDigit(text[pos - 1]))
            {
                int start = pos - 1;
                while (start > 0 && char.IsLetterOrDigit(text[start - 1])) start--;
                string prefix = text.Substring(start, pos - start);

                if (prefix.Length >= 1)
                {
                    _autocomplete.Filter(prefix);
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
            else
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
            
            // Offset to show below the cursor line
            _autocomplete.OriginRelativeParent = new Vector2(editorPos.X, editorPos.Y - _autocomplete.Height - 5);
            _autocomplete.BringToFront();
        }

        private void ApplyAutocomplete()
        {
            string selected = _autocomplete.GetSelected();
            if (string.IsNullOrEmpty(selected)) return;

            string text = Editor.Text;
            int pos = Editor.InternalTextEditWidget.CharIndexToInsertBefore;
            int start = pos;
            while (start > 0 && char.IsLetterOrDigit(text[start - 1])) start--;

            string newText = text.Substring(0, start) + selected + text.Substring(pos);
            Editor.Text = newText;
            Editor.InternalTextEditWidget.CharIndexToInsertBefore = start + selected.Length;
            _autocomplete.Visible = false;
        }

        /// <summary>Called externally when the parent scrolls, to keep the gutter in sync.</summary>
        public void InvalidateGutter() =>
            (Children[0] as FlowLayoutWidget)?.Children[0]?.Invalidate();
    }
}
