using System;


namespace OpenCSharpCad
{
    public class AppStart
    {

        [STAThread]
        public static void Main(string[] args)
        {
            new MainWindow(true)
            {
                UseOpenGL = true,
                Title = "OpenCSharpCad"
            }.ShowAsSystemWindow();
        }
    }
}
