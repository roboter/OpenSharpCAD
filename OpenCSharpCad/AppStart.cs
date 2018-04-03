using System;

namespace OpenSharpCAD
{
    public class AppStart
    {
        [STAThread]
        public static void Main(string[] args)
        {
            
            new MainWindow(true)
            {
                UseOpenGL = true,
                Title = "OpenSharpCAD",
                Maximized = true
            }.ShowAsSystemWindow();
        }
    }
}
