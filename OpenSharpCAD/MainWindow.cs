#nullable enable
using System;
using System.Collections.Generic;
using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using MatterHackers.Csg.Operations;
using MatterHackers.VectorMath;

using System.CodeDom.Compiler;
using System.Text;
using MatterHackers.Csg.Transform;
using MatterHackers.Csg.Solids;
using MatterHackers.RenderOpenGl;
using MatterHackers.RenderOpenGl.OpenGl;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace CSharpCAD
{
    public class TrackballTumbleView : TrackballTumbleWidget
    {
        public Action DrawContent;
        public WorldView World;
        /// <summary>Fired after every camera rotation/move so overlays can refresh.</summary>
        public Action ViewChanged;

        public TrackballTumbleView(WorldView world, GuiWidget sourceWidget)
            : base(world, sourceWidget)
        {
            this.World = world;
            this.TransformState = MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Translation;
        }

        public override void OnDraw(Graphics2D graphics2D)
        {
            base.OnDraw(graphics2D);
            if (DrawContent != null)
            {
                RectangleDouble screenRect = this.TransformToScreenSpace(LocalBounds);
                // Scale viewport for Retina displays
                screenRect.Left *= GuiWidget.DeviceScale;
                screenRect.Bottom *= GuiWidget.DeviceScale;
                screenRect.Right *= GuiWidget.DeviceScale;
                screenRect.Top *= GuiWidget.DeviceScale;

                if (screenRect.Width <= 0 || screenRect.Height <= 0)
                {
                    return;
                }

                GLHelper.SetGlContext(this.World, screenRect, new LightingData()
                {
                    AmbientLight = new float[] { 0.2f, 0.2f, 0.2f, 1.0f }
                });
                DrawContent();
                GLHelper.UnsetGlContext();
            }
        }

        public override void OnMouseDown(MouseEventArgs mouseEvent)
        {
            if (mouseEvent.Button == MouseButtons.Left)
            {
                TransformState = MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Translation;
            }
            else if (mouseEvent.Button == MouseButtons.Right)
            {
                TransformState = MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Rotation;
            }
            else if (mouseEvent.Button == MouseButtons.Middle)
            {
                TransformState = MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Translation;
            }

            base.OnMouseDown(mouseEvent);

            if (MouseCaptured)
            {
                Vector2 pos = new Vector2(mouseEvent.X, mouseEvent.Y);
                if (mouseEvent.Button == MouseButtons.Left || mouseEvent.Button == MouseButtons.Middle)
                {
                    TrackBallController.OnMouseDown(pos, Matrix4X4.Identity, MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Translation);
                }
                else if (mouseEvent.Button == MouseButtons.Right)
                {
                    TrackBallController.OnMouseDown(pos, Matrix4X4.Identity, MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Rotation);
                }
            }
        }

        public override void OnMouseMove(MouseEventArgs mouseEvent)
        {
            base.OnMouseMove(mouseEvent);
            ViewChanged?.Invoke();
        }

        public override void OnMouseUp(MouseEventArgs mouseEvent)
        {
            base.OnMouseUp(mouseEvent);
            ViewChanged?.Invoke();
        }
    }

    public class MainWindow : SystemWindow
    {
        public TrackballTumbleView trackBallWidget;
        private Button outputScad;
        private Splitter verticalSplitter;
        private AxisOrientationWidget axisWidget;

        private GuiWidget objectEditorView;
        private FlowLayoutWidget objectEditorList;
        private FlowLayoutWidget textSide;
        private CodeEditorWidget _codeEditor;
        private ProblemsPanelWidget _problemsPanel;
        private Union rootUnion = new Union("root");
        dynamic classRef;
        public dynamic ClassRef => classRef;

        // Keep track of error lines for highlighting
        private List<int> errorLines = new List<int>();

        private HelpWindow _helpWindow;
        private string? _currentFilePath = null;
        private bool _isModified = false;
        private TextWidget _fileStatusWidget;

        // Animation UI & State
        private FlowLayoutWidget _animationBar;
        private HelpWindow.StyledButton _playPauseBtn;
        private TextEditWidget _timeInput;
        private TextEditWidget _fpsInput;
        private TextEditWidget _stepsInput;
        private TextWidget _stepLabel;

        private bool _isAnimating = false;
        private int _currentStep = 0;
        private int _totalSteps = 100;
        private double _fps = 20.0;
        private double _currentTime = 0.0;
        private RunningInterval? _animationInterval;

        public MainWindow(bool renderRayTrace) : base(800, 600)
        {
            MatterHackers.Agg.Platform.AggContext.FileDialogs = new CrossPlatformFileDialogProvider();

            var testUnion = new Translate(new Box(10, 10, 20) - new Box(8, 20, 10), 5, 5, 5);

            rootUnion.Box(8, 20, 40);
            rootUnion.Add(new Translate(new BoxPrimitive(10, 10, 20), 5, 10, 5));
            rootUnion.Add(new BoxPrimitive(8, 20, 10));

            verticalSplitter = new Splitter()
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                Orientation = Orientation.Vertical,
                SplitterDistance = 420,
                SplitterBackground = new Color(50, 52, 68),
            };
            verticalSplitter.Panel1.BackgroundColor = new Color(30, 32, 44);
            verticalSplitter.Panel2.BackgroundColor = new Color(20, 22, 30);
            AddChild(verticalSplitter);

            {
                // panel 1: Editor & Toolbar
                textSide = new FlowLayoutWidget(FlowDirection.TopToBottom)
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                    BackgroundColor = new Color(30, 32, 44),
                };

                // Top toolbar above editor
                var toolBar = new FlowLayoutWidget(FlowDirection.LeftToRight)
                {
                    HAnchor = HAnchor.Stretch,
                    Height = 36,
                    BackgroundColor = new Color(26, 27, 38),
                    Padding = new BorderDouble(10, 4),
                };

                var editorTitle = new TextWidget("C# CAD", pointSize: 11, textColor: new Color(180, 185, 205), bold: true)
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(0, 0, 6, 0),
                };
                toolBar.AddChild(editorTitle);

                _fileStatusWidget = new TextWidget("Untitled.cs", pointSize: 10, textColor: new Color(139, 233, 253))
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(0, 0, 10, 0),
                };
                toolBar.AddChild(_fileStatusWidget);

                var newBtn = new HelpWindow.StyledButton("📄 New", new Color(248, 248, 242), new Color(50, 54, 72), new Color(75, 82, 110), pointSize: 9.5, padding: new BorderDouble(8, 4))
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(2, 0),
                    ToolTipText = "New C# CAD Script [Ctrl+N / Cmd+N]",
                };
                newBtn.Click += (s, e) => NewFile();
                toolBar.AddChild(newBtn);

                var openBtn = new HelpWindow.StyledButton("📂 Open", new Color(248, 248, 242), new Color(50, 54, 72), new Color(75, 82, 110), pointSize: 9.5, padding: new BorderDouble(8, 4))
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(2, 0),
                    ToolTipText = "Open CAD Script [Ctrl+O / Cmd+O]",
                };
                openBtn.Click += (s, e) => OpenFile();
                toolBar.AddChild(openBtn);

                var saveBtn = new HelpWindow.StyledButton("💾 Save", new Color(248, 248, 242), new Color(50, 54, 72), new Color(75, 82, 110), pointSize: 9.5, padding: new BorderDouble(8, 4))
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(2, 0),
                    ToolTipText = "Save Script [Ctrl+S / Cmd+S]",
                };
                saveBtn.Click += (s, e) => SaveFile();
                toolBar.AddChild(saveBtn);

                var saveAsBtn = new HelpWindow.StyledButton("💾 Save As...", new Color(248, 248, 242), new Color(50, 54, 72), new Color(75, 82, 110), pointSize: 9.5, padding: new BorderDouble(8, 4))
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(2, 0),
                    ToolTipText = "Save Script As [Ctrl+Shift+S / Cmd+Shift+S]",
                };
                saveAsBtn.Click += (s, e) => SaveFileAs();
                toolBar.AddChild(saveAsBtn);

                var animateBtn = new HelpWindow.StyledButton("🎬 Animate", new Color(248, 248, 242), new Color(50, 54, 72), new Color(75, 82, 110), pointSize: 9.5, padding: new BorderDouble(8, 4))
                {
                    VAnchor = VAnchor.Center,
                    Margin = new BorderDouble(2, 0),
                    ToolTipText = "Toggle Animation Controls",
                };
                animateBtn.Click += (s, e) =>
                {
                    if (_animationBar != null)
                    {
                        _animationBar.Visible = !_animationBar.Visible;
                        if (!_animationBar.Visible && _isAnimating)
                        {
                            StopAnimation();
                        }
                    }
                };
                toolBar.AddChild(animateBtn);

                toolBar.AddChild(new GuiWidget { HAnchor = HAnchor.Stretch });

                var helpBtn = new GuiWidget()
                {
                    VAnchor = VAnchor.Center,
                    BackgroundColor = new Color(68, 71, 90),
                    Cursor = Cursors.Hand,
                    Padding = new BorderDouble(10, 4),
                };
                var helpText = new TextWidget("Cheat Sheet & Reference [F1]", pointSize: 10, textColor: Color.White, bold: true, ellipsisIfClipped: false)
                {
                    VAnchor = VAnchor.Center,
                    HAnchor = HAnchor.Center,
                    AutoExpandBoundsToText = true,
                    Selectable = false,
                };
                helpBtn.AddChild(helpText);
                helpBtn.SetBoundsToEncloseChildren();
                helpBtn.MouseEnterBounds += (s, e) => { helpBtn.BackgroundColor = new Color(98, 114, 164); helpBtn.Invalidate(); };
                helpBtn.MouseLeaveBounds += (s, e) => { helpBtn.BackgroundColor = new Color(68, 71, 90); helpBtn.Invalidate(); };
                helpBtn.Click += (s, e) => ToggleHelp();
                toolBar.AddChild(helpBtn);

                textSide.AddChild(toolBar);

                var code = new StringBuilder();
                code.AppendLine("var holesize =4;");
                code.AppendLine("// draw a box");
                code.AppendLine("var box = new Box(12, 20, 20);");
                code.AppendLine("var rotated = new Rotate(box, x: MathHelper.DegreesToRadians(45));");
                code.AppendLine("var cylinder = new Cylinder(6, 20, 24);");
                code.AppendLine("// draw with default grey");
                code.AppendLine("Draw(box + rotated - cylinder);");
                code.AppendLine("// draw with a color: RGB(r, g, b)  or  RGBA(r, g, b, a)  or  RGBf(0.0-1.0, ...)");
                code.AppendLine("var translated = new Translate(new Box(40, 10, 10), 0, holesize, holesize);");
                code.AppendLine("Draw(translated, RGB(80, 180, 255));");

                _codeEditor = new CodeEditorWidget(code.ToString().Replace('\r', '\n'), fontSize: 16)
                {
                    HAnchor = HAnchor.Stretch,
                    VAnchor = VAnchor.Stretch,
                };
                _codeEditor.TextChanged += Hello_TextChanged;
                _codeEditor.Editor.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.F1)
                    {
                        ToggleHelp();
                        e.Handled = true;
                    }
                    else if (e.Control && e.KeyCode == Keys.N)
                    {
                        NewFile();
                        e.Handled = true;
                    }
                    else if (e.Control && e.KeyCode == Keys.O)
                    {
                        OpenFile();
                        e.Handled = true;
                    }
                    else if (e.Control && e.Shift && e.KeyCode == Keys.S)
                    {
                        SaveFileAs();
                        e.Handled = true;
                    }
                    else if (e.Control && e.KeyCode == Keys.S)
                    {
                        SaveFile();
                        e.Handled = true;
                    }
                };
                _isModified = false;
                UpdateTitleAndStatus();
                textSide.AddChild(_codeEditor);
                verticalSplitter.Panel1.AddChild(textSide);
            }


            // panel 2 stuff
            Splitter rightSplitter = new Splitter()
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                Orientation = Orientation.Horizontal,
                SplitterDistance = 150,
            };
            verticalSplitter.Panel2.AddChild(rightSplitter);

            var world = new WorldView(800, 600);
            world.TranslationMatrix = Matrix4X4.CreateTranslation(0, 0, -50);
            trackBallWidget = new TrackballTumbleView(world, rightSplitter.Panel1)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Stretch,
                DrawContent = glLightedView_DrawGlContent,
                TransformState = MatterHackers.VectorMath.TrackBall.TrackBallTransformType.Translation,
            };
            rightSplitter.Panel1.AddChild(trackBallWidget);

            // Axis orientation gizmo – floating overlay in the 3D panel
            axisWidget = new AxisOrientationWidget(world);
            rightSplitter.Panel1.AddChild(axisWidget);

            // Floating 3D viewport controls (Home, Zoom In, Zoom Out)
            var viewportHud = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Right,
                VAnchor = VAnchor.Top,
                Margin = new BorderDouble(0, 0, 14, 14),
                Padding = new BorderDouble(6, 4),
                BackgroundColor = new Color(24, 26, 36, 210),
            };

            var homeBtn = new HelpWindow.StyledButton("🏠 Home", new Color(248, 248, 242), new Color(68, 71, 90), new Color(98, 114, 164), pointSize: 10, padding: new BorderDouble(8, 5))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(2, 0),
            };
            homeBtn.Click += (s, e) => CenterView();
            viewportHud.AddChild(homeBtn);

            var zoomInBtn = new HelpWindow.StyledButton("+", new Color(248, 248, 242), new Color(50, 54, 72), new Color(70, 76, 100), pointSize: 11, padding: new BorderDouble(8, 4))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(2, 0),
            };
            zoomInBtn.Click += (s, e) =>
            {
                trackBallWidget.World.Scale *= 1.2;
                trackBallWidget.Invalidate();
            };
            viewportHud.AddChild(zoomInBtn);

            var zoomOutBtn = new HelpWindow.StyledButton("-", new Color(248, 248, 242), new Color(50, 54, 72), new Color(70, 76, 100), pointSize: 11, padding: new BorderDouble(8, 4))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(2, 0),
            };
            zoomOutBtn.Click += (s, e) =>
            {
                trackBallWidget.World.Scale *= (1.0 / 1.2);
                trackBallWidget.Invalidate();
            };
            viewportHud.AddChild(zoomOutBtn);

            rightSplitter.Panel1.AddChild(viewportHud);

            // Bottom Animation Control Bar (Time, FPS, Steps)
            _animationBar = new FlowLayoutWidget(FlowDirection.LeftToRight)
            {
                HAnchor = HAnchor.Stretch,
                VAnchor = VAnchor.Bottom,
                Height = 38,
                BackgroundColor = new Color(24, 26, 36, 235),
                Padding = new BorderDouble(10, 4),
            };

            _playPauseBtn = new HelpWindow.StyledButton("▶ Play", new Color(20, 24, 32), new Color(80, 250, 123), new Color(100, 255, 140), pointSize: 9.5, padding: new BorderDouble(10, 4))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(2, 0),
                ToolTipText = "Play / Pause Animation",
            };
            _playPauseBtn.Click += (s, e) => TogglePlayPause();
            _animationBar.AddChild(_playPauseBtn);

            var resetAnimBtn = new HelpWindow.StyledButton("⏮", new Color(248, 248, 242), new Color(50, 54, 72), new Color(75, 82, 110), pointSize: 9.5, padding: new BorderDouble(8, 4))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(2, 0),
                ToolTipText = "Reset Animation (Time = 0.0)",
            };
            resetAnimBtn.Click += (s, e) => ResetAnimation();
            _animationBar.AddChild(resetAnimBtn);

            _animationBar.AddChild(new GuiWidget(8, 1));

            var timeLbl = new TextWidget("Time:", pointSize: 9.5, textColor: new Color(139, 233, 253), bold: true)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 4, 0),
            };
            _animationBar.AddChild(timeLbl);

            _timeInput = new TextEditWidget("0.000", pointSize: 9.5, pixelWidth: 55)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 12, 0),
                BackgroundColor = new Color(40, 44, 58),
            };
            _timeInput.TextColor = Color.White;
            _timeInput.EditComplete += (s, e) => OnTimeInputChanged();
            _timeInput.EnterPressed += (s, e) => OnTimeInputChanged();
            _animationBar.AddChild(_timeInput);

            var fpsLbl = new TextWidget("FPS:", pointSize: 9.5, textColor: new Color(248, 248, 242), bold: true)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 4, 0),
            };
            _animationBar.AddChild(fpsLbl);

            _fpsInput = new TextEditWidget("20", pointSize: 9.5, pixelWidth: 40)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 12, 0),
                BackgroundColor = new Color(40, 44, 58),
            };
            _fpsInput.TextColor = Color.White;
            _fpsInput.EditComplete += (s, e) => OnFpsInputChanged();
            _fpsInput.EnterPressed += (s, e) => OnFpsInputChanged();
            _animationBar.AddChild(_fpsInput);

            var stepsLbl = new TextWidget("Steps:", pointSize: 9.5, textColor: new Color(248, 248, 242), bold: true)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 4, 0),
            };
            _animationBar.AddChild(stepsLbl);

            _stepsInput = new TextEditWidget("100", pointSize: 9.5, pixelWidth: 50)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 12, 0),
                BackgroundColor = new Color(40, 44, 58),
            };
            _stepsInput.TextColor = Color.White;
            _stepsInput.EditComplete += (s, e) => OnStepsInputChanged();
            _stepsInput.EnterPressed += (s, e) => OnStepsInputChanged();
            _animationBar.AddChild(_stepsInput);

            _stepLabel = new TextWidget("Step 0 / 100", pointSize: 9, textColor: new Color(160, 165, 185))
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(4, 0, 0, 0),
            };
            _animationBar.AddChild(_stepLabel);

            rightSplitter.Panel1.AddChild(_animationBar);

            // Refresh gizmo on every camera rotation
            trackBallWidget.ViewChanged += () => axisWidget?.Invalidate();

            _problemsPanel = new ProblemsPanelWidget(diag =>
            {
                _codeEditor.NavigateTo(diag.Line, diag.Column, diag.StartIndex, diag.Length);
            });
            rightSplitter.Panel2.AddChild(_problemsPanel);

            BackgroundColor = Color.White;
            PerformLayout();
            Compile();
        }

        public void SetScriptAndCompile(string script)
        {
            _codeEditor.Text = script;
            Compile();
        }

        public void NewFile()
        {
            Console.WriteLine("[MainWindow] NewFile invoked");
            string defaultCode = @"// New C# CAD Script
var box = new Box(20, 20, 20, createCentered: true);
var cylinder = new Cylinder(radius: 6, height: 30);
Draw(box - cylinder, RGB(80, 180, 255));
";
            _currentFilePath = null;
            _codeEditor.Text = defaultCode.Replace("\r\n", "\n");
            _isModified = false;
            UpdateTitleAndStatus();
            Compile();
        }

        public void OpenFile()
        {
            Console.WriteLine("[MainWindow] OpenFile invoked");
            string? selectedPath = NativeFileDialogService.ShowOpenFileDialog(
                "Open C# CAD Script",
                "C# CAD Files (*.cs;*.part)|*.cs;*.part|All Files (*.*)|*.*",
                _currentFilePath != null ? System.IO.Path.GetDirectoryName(_currentFilePath) : null);

            if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
            {
                try
                {
                    string content = File.ReadAllText(selectedPath);
                    _currentFilePath = selectedPath;
                    _codeEditor.Text = content.Replace("\r\n", "\n");
                    _isModified = false;
                    UpdateTitleAndStatus();
                    Compile();
                    Console.WriteLine($"[MainWindow] Successfully opened '{selectedPath}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error opening file '{selectedPath}': {ex.Message}");
                }
            }
        }

        public void SaveFile()
        {
            Console.WriteLine($"[MainWindow] SaveFile invoked (current path: '{_currentFilePath}')");
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveFileAs();
            }
            else
            {
                try
                {
                    File.WriteAllText(_currentFilePath, _codeEditor.Text);
                    _isModified = false;
                    UpdateTitleAndStatus();
                    Console.WriteLine($"[MainWindow] Saved to '{_currentFilePath}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving file '{_currentFilePath}': {ex.Message}");
                }
            }
        }

        public void SaveFileAs()
        {
            Console.WriteLine("[MainWindow] SaveFileAs invoked");
            string defaultName = !string.IsNullOrEmpty(_currentFilePath)
                ? System.IO.Path.GetFileName(_currentFilePath)
                : "model.cs";

            string? selectedPath = NativeFileDialogService.ShowSaveFileDialog(
                "Save C# CAD Script",
                defaultName,
                _currentFilePath != null ? System.IO.Path.GetDirectoryName(_currentFilePath) : null);

            if (!string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    File.WriteAllText(selectedPath, _codeEditor.Text);
                    _currentFilePath = selectedPath;
                    _isModified = false;
                    UpdateTitleAndStatus();
                    Console.WriteLine($"[MainWindow] Saved as '{selectedPath}'");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error saving file '{selectedPath}': {ex.Message}");
                }
            }
        }

        public void UpdateTitleAndStatus()
        {
            string fileName = !string.IsNullOrEmpty(_currentFilePath)
                ? System.IO.Path.GetFileName(_currentFilePath)
                : "Untitled.cs";

            string modIndicator = _isModified ? " *" : "";

            if (_fileStatusWidget != null)
            {
                _fileStatusWidget.Text = $"{fileName}{modIndicator}";
            }

            this.Title = $"OpenSharpCAD - {fileName}{modIndicator}";
        }

        public void TogglePlayPause()
        {
            if (_isAnimating)
            {
                StopAnimation();
            }
            else
            {
                StartAnimation();
            }
        }

        public void StartAnimation()
        {
            if (_isAnimating) return;
            _isAnimating = true;
            if (_playPauseBtn != null)
            {
                _playPauseBtn.SetText("⏸ Pause");
                _playPauseBtn.SetColors(new Color(255, 184, 108), new Color(255, 200, 130), new Color(20, 24, 32));
            }

            double intervalSeconds = Math.Max(0.01, 1.0 / Math.Max(1.0, _fps));
            _animationInterval = UiThread.SetInterval(OnAnimationTick, intervalSeconds);
        }

        public void StopAnimation()
        {
            if (!_isAnimating && _animationInterval == null) return;
            _isAnimating = false;
            if (_animationInterval != null)
            {
                UiThread.ClearInterval(_animationInterval);
                _animationInterval = null;
            }
            if (_playPauseBtn != null)
            {
                _playPauseBtn.SetText("▶ Play");
                _playPauseBtn.SetColors(new Color(80, 250, 123), new Color(100, 255, 140), new Color(20, 24, 32));
            }
        }

        public void ResetAnimation()
        {
            StopAnimation();
            _currentStep = 0;
            _currentTime = 0.0;
            UpdateAnimationUi();
            RebuildCurrentFrame();
        }

        private void OnAnimationTick()
        {
            if (!_isAnimating || classRef == null) return;

            _currentStep = (_currentStep + 1) % _totalSteps;
            _currentTime = (double)_currentStep / _totalSteps;

            UpdateAnimationUi();
            RebuildCurrentFrame();
        }

        private void UpdateAnimationUi()
        {
            if (_timeInput != null && !_timeInput.ContainsFocus)
            {
                _timeInput.Text = _currentTime.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture);
            }
            if (_stepLabel != null)
            {
                _stepLabel.Text = $"Step {_currentStep} / {_totalSteps}";
            }
        }

        private void RebuildCurrentFrame()
        {
            if (classRef == null) return;
            try
            {
                classRef.BuildTime(_currentTime);
                trackBallWidget?.Invalidate();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Animation Error] {ex.Message}");
            }
        }

        private void OnTimeInputChanged()
        {
            if (_timeInput != null && double.TryParse(_timeInput.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedT))
            {
                _currentTime = Math.Clamp(parsedT, 0.0, 1.0);
                _currentStep = (int)Math.Round(_currentTime * _totalSteps) % _totalSteps;
                if (_stepLabel != null)
                {
                    _stepLabel.Text = $"Step {_currentStep} / {_totalSteps}";
                }
                RebuildCurrentFrame();
            }
        }

        private void OnFpsInputChanged()
        {
            if (_fpsInput != null && double.TryParse(_fpsInput.Text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double parsedFps))
            {
                _fps = Math.Clamp(parsedFps, 1.0, 60.0);
                if (_isAnimating)
                {
                    if (_animationInterval != null)
                    {
                        UiThread.ClearInterval(_animationInterval);
                    }
                    double intervalSeconds = Math.Max(0.01, 1.0 / _fps);
                    _animationInterval = UiThread.SetInterval(OnAnimationTick, intervalSeconds);
                }
            }
        }

        private void OnStepsInputChanged()
        {
            if (_stepsInput != null && int.TryParse(_stepsInput.Text, out int parsedSteps))
            {
                _totalSteps = Math.Max(1, parsedSteps);
                _currentStep = (int)Math.Round(_currentTime * _totalSteps) % _totalSteps;
                if (_stepLabel != null)
                {
                    _stepLabel.Text = $"Step {_currentStep} / {_totalSteps}";
                }
            }
        }

        private System.Threading.Timer _compileDebounceTimer;

        private void Hello_TextChanged(object sender, EventArgs e)
        {
            _isModified = true;
            UpdateTitleAndStatus();

            // Clear error lines tracking
            errorLines.Clear();
            _compileDebounceTimer?.Dispose();
            _compileDebounceTimer = new System.Threading.Timer(_ =>
            {
                UiThread.RunOnIdle(() => Compile());
            }, null, 250, System.Threading.Timeout.Infinite);
        }

        public void ToggleHelp()
        {
            if (_helpWindow == null || _helpWindow.HasBeenClosed)
            {
                _helpWindow = new HelpWindow(
                    onInsertCode: (snippet, replaceWhole) =>
                    {
                        if (replaceWhole)
                        {
                            _codeEditor.Text = snippet;
                            Compile();
                            CenterView();
                        }
                        else
                        {
                            string current = _codeEditor.Text ?? "";
                            if (string.IsNullOrWhiteSpace(current))
                            {
                                _codeEditor.Text = snippet;
                            }
                            else
                            {
                                _codeEditor.Text = current.TrimEnd() + "\n\n" + snippet;
                            }
                        }
                        if (!HasBeenClosed && _codeEditor?.Editor?.Parent != null)
                        {
                            try { _codeEditor.Editor.Focus(); } catch { }
                        }
                    },
                    onClose: () =>
                    {
                        _helpWindow = null;
                        if (!HasBeenClosed && _codeEditor?.Editor?.Parent != null)
                        {
                            try { _codeEditor.Editor.Focus(); } catch { }
                        }
                    }
                );
                _helpWindow.ShowAsSystemWindow();
            }
            else
            {
                _helpWindow.BringToFront();
                _helpWindow.Focus();
            }
        }

        public override void OnKeyDown(KeyEventArgs keyEvent)
        {
            if (keyEvent.KeyCode == Keys.F1)
            {
                ToggleHelp();
                keyEvent.Handled = true;
                return;
            }
            if (keyEvent.Control && keyEvent.KeyCode == Keys.N)
            {
                NewFile();
                keyEvent.Handled = true;
                return;
            }
            if (keyEvent.Control && keyEvent.KeyCode == Keys.O)
            {
                OpenFile();
                keyEvent.Handled = true;
                return;
            }
            if (keyEvent.Control && keyEvent.Shift && keyEvent.KeyCode == Keys.S)
            {
                SaveFileAs();
                keyEvent.Handled = true;
                return;
            }
            if (keyEvent.Control && keyEvent.KeyCode == Keys.S)
            {
                SaveFile();
                keyEvent.Handled = true;
                return;
            }

            base.OnKeyDown(keyEvent);
        }

        public override void OnClosed(EventArgs e)
        {
            StopAnimation();
            var hw = _helpWindow;
            _helpWindow = null;
            if (hw != null && !hw.HasBeenClosed)
            {
                try { hw.Close(); } catch { }
            }
            base.OnClosed(e);
        }

        private void Compile()
        {
            var compilerService = new CompilerService();
            List<Diagnostic> errors;

            string script = _codeEditor.Text ?? "";
            dynamic newClassRef = compilerService.Compile(script, out errors);

            var editorDiagnostics = new List<EditorDiagnostic>();

            bool hasCompileErrors = (errors != null && Enumerable.Any(errors, e => e.Severity == DiagnosticSeverity.Error)) || newClassRef == null;

            if (errors != null && errors.Count > 0)
            {
                foreach (var error in errors)
                {
                    var lineSpan = error.Location.GetLineSpan();
                    int line = lineSpan.StartLinePosition.Line; // #line 1 "script" makes this directly 0-indexed relative to user script
                    if (line < 0) line = 0;
                    int col = lineSpan.StartLinePosition.Character;
                    if (col < 0) col = 0;

                    int startIdx = GetCharIndex(script, line, col);
                    int endCol = lineSpan.EndLinePosition.Character;
                    int length = 1;
                    if (lineSpan.EndLinePosition.Line == line && endCol > col)
                    {
                        length = endCol - col;
                    }
                    else
                    {
                        int end = startIdx;
                        while (end < script.Length && (char.IsLetterOrDigit(script[end]) || script[end] == '_'))
                            end++;
                        length = Math.Max(1, end - startIdx);
                    }

                    editorDiagnostics.Add(new EditorDiagnostic
                    {
                        Id = error.Id,
                        Message = error.GetMessage(),
                        Line = line,
                        Column = col,
                        StartIndex = startIdx,
                        Length = length,
                        IsWarning = error.Severity == DiagnosticSeverity.Warning,
                        IsRuntime = false,
                    });
                }

                if (hasCompileErrors)
                {
                    // Compilation error: report diagnostics and keep previous 3D object
                    _codeEditor.SetDiagnostics(editorDiagnostics);
                    _problemsPanel.SetDiagnostics(editorDiagnostics);
                    trackBallWidget.Invalidate();
                    axisWidget?.Invalidate();
                    return;
                }
            }

            bool buildSuccess = false;
            try
            {
                // Pre-compute & cache meshes once during compile at current animation time
                newClassRef?.BuildTime(_currentTime);
                if (newClassRef != null && !string.IsNullOrEmpty((string)newClassRef.LastError))
                {
                    int rtLine = 0;
                    try { rtLine = (int)newClassRef.LastErrorLine; } catch { }
                    if (rtLine < 0) rtLine = 0;
                    int rtStartIdx = GetCharIndex(script, rtLine, 0);

                    editorDiagnostics.Add(new EditorDiagnostic
                    {
                        Id = "Runtime",
                        Message = (string)newClassRef.LastError,
                        Line = rtLine,
                        Column = 0,
                        StartIndex = rtStartIdx,
                        Length = 1,
                        IsRuntime = true,
                    });
                }
                else if (newClassRef != null)
                {
                    buildSuccess = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during script Build: " + ex.Message);
                int exLine = 0;
                var match = System.Text.RegularExpressions.Regex.Match(ex.StackTrace ?? "", @"(?:script:line |:line )(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int parsedL))
                {
                    exLine = Math.Max(0, parsedL - 1);
                }
                int exStartIdx = GetCharIndex(script, exLine, 0);

                editorDiagnostics.Add(new EditorDiagnostic
                {
                    Id = "Runtime",
                    Message = ex.Message,
                    Line = exLine,
                    Column = 0,
                    StartIndex = exStartIdx,
                    Length = 1,
                    IsRuntime = true,
                });
            }

            // Only update the active 3D object if the build succeeded without error!
            // If compilation or runtime error occurs, keep previous 3D object so it remains visible.
            if (buildSuccess)
            {
                classRef = newClassRef;
            }

            _codeEditor.SetDiagnostics(editorDiagnostics);
            _problemsPanel.SetDiagnostics(editorDiagnostics);
            trackBallWidget.Invalidate();
            axisWidget?.Invalidate();

            if (!_hasCenteredView && classRef != null)
            {
                _hasCenteredView = true;
                CenterView();
            }
        }

        private bool _hasCenteredView = false;

        public void CenterView()
        {
            if (trackBallWidget == null) return;
            trackBallWidget.ZeroVelocity();

            AxisAlignedBoundingBox bounds = null;
            try
            {
                bounds = classRef?.GetBounds();
            }
            catch { }

            Vector3 center = (bounds != null) ? bounds.Center : Vector3.Zero;
            Vector3 size = (bounds != null) ? bounds.Size : new Vector3(30, 30, 30);
            double maxDim = Math.Max(size.X, Math.Max(size.Y, size.Z));
            if (maxDim < 0.001 || double.IsNaN(maxDim)) maxDim = 30;

            var world = trackBallWidget.World;
            world.Reset();

            double vHeight = 2.0 * 7.0 * Math.Tan(MathHelper.DegreesToRadians(45.0) * 0.5); // ~5.8
            double w = trackBallWidget.Width > 0 ? trackBallWidget.Width : 800;
            double h = trackBallWidget.Height > 0 ? trackBallWidget.Height : 600;
            double aspect = w / h;
            double vWidth = vHeight * aspect;
            double targetExtent = Math.Min(vHeight, vWidth) * 0.65;
            double scale = targetExtent / maxDim;

            world.Scale = scale;
            world.Translate(-center);
            world.RotationMatrix = Matrix4X4.CreateRotationX(MathHelper.DegreesToRadians(-45)) * Matrix4X4.CreateRotationZ(MathHelper.DegreesToRadians(45));

            trackBallWidget.Invalidate();
            axisWidget?.Invalidate();
        }

        private static int GetCharIndex(string text, int line, int col)
        {
            if (string.IsNullOrEmpty(text)) return 0;
            int curLine = 0;
            int idx = 0;
            while (idx < text.Length && curLine < line)
            {
                if (text[idx] == '\n') curLine++;
                idx++;
            }
            return Math.Min(text.Length, idx + Math.Max(0, col));
        }

        private void LogException(Exception ex)
        {
            // Always output to console so the user can see what happened
            Console.Error.WriteLine($"ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);

            try
            {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(baseDir))
                {
                    baseDir = System.IO.Path.GetTempPath();
                }

                string logDir = System.IO.Path.Combine(baseDir, "CSharpCAD", "logs");

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                string logPath = System.IO.Path.Combine(logDir, "errors.log");
                string logMessage = $"[{DateTime.Now}] {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{new string('-', 30)}{Environment.NewLine}";
                File.AppendAllText(logPath, logMessage);
            }
            catch { }
        }

        private void glLightedView_DrawGlContent()
        {
            try
            {
                if (classRef == null) return;
                classRef.Render();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during classRef.Render(): " + ex.Message);
                LogException(ex);
            }
        }

        private MatterHackers.PolygonMesh.Mesh Pyramid()
        {
            MatterHackers.PolygonMesh.Mesh mesh = new MatterHackers.PolygonMesh.Mesh();
            mesh.Vertices.Add(new Vector3Float(1, 0, 1));  // V0
            mesh.Vertices.Add(new Vector3Float(1, 0, -1)); // v1
            mesh.Vertices.Add(new Vector3Float(-1, 0, -1)); // v2
            mesh.Vertices.Add(new Vector3Float(-1, 0, 1)); // v3
            mesh.Vertices.Add(new Vector3Float(0, 1, 0)); // v4

            mesh.Faces.Add(0, 1, 2, mesh.Vertices);
            mesh.Faces.Add(0, 2, 3, mesh.Vertices);
            mesh.Faces.Add(3, 0, 4, mesh.Vertices);
            mesh.Faces.Add(0, 1, 4, mesh.Vertices);
            mesh.Faces.Add(1, 2, 4, mesh.Vertices);
            mesh.Faces.Add(2, 3, 4, mesh.Vertices);

            return mesh;
        }
    }
}
