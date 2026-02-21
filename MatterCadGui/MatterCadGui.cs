using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using System;
using System.Runtime.InteropServices;

namespace MatterHackers.MatterCad
{
    public class MatterCadGui : SystemWindow
    {
        MatterCadGuiWidget matterCadGuiWidget;

        public MatterCadGui(bool renderRayTrace) : base(800, 600)
        {
            if (renderRayTrace)
            {
                matterCadGuiWidget = new MatterCadGuiWidget();
                AddChild(matterCadGuiWidget);
                matterCadGuiWidget.AnchorAll();
                AnchorAll();
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                AggContext.Config.ProviderTypes.SystemWindowProvider = "MatterHackers.Agg.UI.OpenGLWinformsWindowProvider, agg_platform_win32";
            }
            else
            {
                AggContext.Config.ProviderTypes.SystemWindowProvider = "MatterHackers.GlfwProvider.GlfwWindowProvider, MatterHackers.GlfwProvider";
            }
        
            MatterCadGui cadWindow = new MatterCadGui(true)
            {
                UseOpenGL = true,
                Title = "MatterCADGui"
            };
            cadWindow.ShowAsSystemWindow();
        }
    }
}
