using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using System;
using System.Diagnostics;
using Vector2 = MatterHackers.VectorMath.Vector2;

namespace OpenCSharpCad;

public class RootSystemWindow : SystemWindow
{
    public static bool UseGl { get; set; } = true;

    private static Vector2 minSize { get; set; } = new Vector2(600, 480);

    private Stopwatch totalDrawTime = new Stopwatch();

    private AverageMillisecondTimer millisecondTimer = new AverageMillisecondTimer();

    public static bool ShowMemoryUsed = false;

    private int drawCount = 0;

    private bool exitDialogOpen = false;

    public RootSystemWindow(double width, double height)
        : base(width, height)
    {
        this.Name = "MatterControl";
        this.Padding = new BorderDouble(0); // To be re-enabled once native borders are turned off
        this.AnchorAll();


        var prevLayerButton = new Button("<<", 0, 0);
        //  prevLayerButton.Click += prevLayer_ButtonClick;
        this.AddChild(prevLayerButton);
        this.AnchorAll();

        var textEditWidget = new TextEditWidget("Edit Me", 0, 0, 12);

        this.AddChild(textEditWidget);

        this.AnchorAll();

        GuiWidget.DefaultEnforceIntegerBounds = true;

        // TODO: Needs review - doesn't seem like we want to scale on Touchscreen, rather we want device specific, configuration based scaling. Suggest remove
        if (GuiWidget.TouchScreenMode)
        {
            // TODO: This steps on user scaling
            GuiWidget.DeviceScale = 1.3;
            SystemWindow.ShareSingleOsWindow = true;
        }

        string textSizeMode = String.Empty;
        if (!string.IsNullOrEmpty(textSizeMode))
        {
            if (double.TryParse(textSizeMode, out double textSize))
            {
                GuiWidget.DeviceScale = textSize;
            }
        }

        UseOpenGL = UseGl;
    }

    public static (int width, int height) GetStartupBounds(int overrideWidth = -1, int overrideHeight = -1)
    {
        int width = 0;
        int height = 0;
        if (GuiWidget.TouchScreenMode)
        {
            minSize = new Vector2(800, 480);
        }


        Point2D desktopSize = AggContext.DesktopSize;

        if (overrideWidth != -1)
        {
            width = overrideWidth;
        }
        else // try to set it to a good size
        {
            if (width < desktopSize.x)
            {
                width = 1280;
            }
        }

        if (overrideHeight != -1)
        {
            // Height should be constrained to actual
            height = Math.Min(overrideHeight, desktopSize.y);
        }
        else
        {
            if (height < desktopSize.y)
            {
                height = 720;
            }
        }


        return (width, height);
    }
}