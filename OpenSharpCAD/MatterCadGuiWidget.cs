using MatterHackers.Agg;
using MatterHackers.Agg.UI;
using System;
using System.Diagnostics;
using Vector2 = MatterHackers.VectorMath.Vector2;

namespace OpenCSharpCad;

class MatterCadGuiWidget : SystemWindow
{
    public static bool UseGl { get; set; } = true;

    private static Vector2 minSize { get; set; } = new Vector2(600, 480);

    private Stopwatch totalDrawTime = new Stopwatch();

    private AverageMillisecondTimer millisecondTimer = new AverageMillisecondTimer();

    public static bool ShowMemoryUsed = true;

    private int drawCount = 0;

    public MatterCadGuiWidget(double width, double height) : base(width, height)
    {

        this.Name = "MatterControl";
        this.Padding = new BorderDouble(0); // To be re-enabled once native borders are turned off
        this.AnchorAll();

        GuiWidget.DefaultEnforceIntegerBounds = true;

        // TODO: Needs review - doesn't seem like we want to scale on Touchscreen, rather we want device specific, configuration based scaling. Suggest remove
        if (GuiWidget.TouchScreenMode)
        {
            // TODO: This steps on user scaling
            GuiWidget.DeviceScale = 1.3;
            SystemWindow.ShareSingleOsWindow = true;
        }

        string textSizeMode = "10";// UserSettings.Instance.get(UserSettingsKey.ApplicationTextSize);
        if (!string.IsNullOrEmpty(textSizeMode))
        {
            if (double.TryParse(textSizeMode, out double textSize))
            {
                GuiWidget.DeviceScale = textSize;
            }
        }

        UseOpenGL = UseGl;

        this.SetStartupTraits();
    }

    public void SetStartupTraits()
    {
        string version = "2.0";

        this.MinimumSize = minSize;

        this.Title = $"1223";


        this.Title += string.Format(" - {0}Bit", IntPtr.Size == 4 ? 32 : 64);


        this.DesktopPosition = new Point2D(-1, -1);

        this.Maximized = false;
    }

    public override void OnDraw(Graphics2D graphics2D)
    {
        totalDrawTime.Restart();
        GuiWidget.DrawCount = 0;
        using (new PerformanceTimer("Draw Timer", "MC Draw"))
        {
            base.OnDraw(graphics2D);
        }

        totalDrawTime.Stop();

        millisecondTimer.Update((int)totalDrawTime.ElapsedMilliseconds);

        if (ShowMemoryUsed)
        {
            long memory = GC.GetTotalMemory(false);
            this.Title = $"Allocated = {memory:n0} : {millisecondTimer.GetAverage()}ms, d{drawCount++} Size = {this.Width}x{this.Height}, onIdle = {UiThread.CountExpired}:{UiThread.Count}, widgetsDrawn = {GuiWidget.DrawCount}";
        }

        // msGraph.AddData("ms", totalDrawTime.ElapsedMilliseconds);
        // msGraph.Draw(MatterHackers.Agg.Transform.Affine.NewIdentity(), graphics2D);
    }
}
