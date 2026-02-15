using MatterHackers.Agg.Platform;
using System;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;
using System.IO;
using MatterHackers.Agg.UI;

namespace CSharpCAD;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AggContext.Config.ProviderTypes.SystemWindowProvider = "MatterHackers.Agg.UI.OpenGLWinformsWindowProvider, agg_platform_win32";
        }
        else
        {
            AggContext.Config.ProviderTypes.SystemWindowProvider = "MatterHackers.GlfwProvider.GlfwWindowProvider, MatterHackers.GlfwProvider";
        }

        try
        {
            // Set the global culture for the app, current thread and all new threads
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            var main = new MainWindow(true);
            main.UseOpenGL = true;
            main.Title = "OpenCSharpCad";

            main.ShowAsSystemWindow();
        }
        catch (Exception ex)
        {
            string projectRoot = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", ".."));
            string logDir = Path.Combine(projectRoot, "logs");
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            string logPath = Path.Combine(logDir, "errors.log");
            string logMessage = $"[{DateTime.Now}] GLOBAL ERROR: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{new string('=', 30)}{Environment.NewLine}";
            File.AppendAllText(logPath, logMessage);
        }
    }
}
