namespace OpenSharpCAD
{
    using System;

    public class AppStart
    {
        [STAThread]
        public static void Main(string[] args)
        {
            new MainWindow
                {
                UseOpenGL = true,
                Title = "OpenSharpCAD",
                Maximized = true
            }.ShowAsSystemWindow();
        }
    }
}
