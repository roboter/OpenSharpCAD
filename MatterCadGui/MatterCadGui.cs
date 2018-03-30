using System;
using MatterHackers.Agg.UI;

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
            MatterCadGui cadWindow = new MatterCadGui(true)
            {
                UseOpenGL = true,
                Title = "MatterCADGui"
            };
            cadWindow.ShowAsSystemWindow();
        }
    }
}
