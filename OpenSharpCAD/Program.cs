using MatterHackers.Agg.Platform;
using System;
using System.Runtime.InteropServices;
using System.Globalization;
using System.Threading;
using System.IO;
using System.Reflection;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

namespace CSharpCAD;

public class MacOsInformationProvider : IOsInformationProvider
{
    public OSType OperatingSystem => OSType.Mac;

    public MatterHackers.Agg.Point2D DesktopSize => new MatterHackers.Agg.Point2D(1920, 1080); // Default size

    public long PhysicalMemory => 8L * 1024L * 1024L * 1024L; // 8GB default
}

class Program
{
    private static void EnsureGlfwProviderLoaded()
    {
        var candidatePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Submodules", "agg-sharp", "Glfw", "bin", "Debug", "net6.0", "MatterHackers.GlfwProvider.dll"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Submodules", "agg-sharp", "Glfw", "bin", "Release", "net6.0", "MatterHackers.GlfwProvider.dll"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Submodules", "agg-sharp", "Glfw", "bin", "Debug", "net6.0-windows", "MatterHackers.GlfwProvider.dll"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Submodules", "agg-sharp", "Glfw", "bin", "Release", "net6.0-windows", "MatterHackers.GlfwProvider.dll")
        };

        foreach (var path in candidatePaths)
        {
            var fullPath = Path.GetFullPath(path);
            if (File.Exists(fullPath))
            {
                Assembly.LoadFrom(fullPath);
                return;
            }
        }
    }

    [STAThread]
    static void Main(string[] args)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AggContext.Config.ProviderTypes.SystemWindowProvider = "MatterHackers.Agg.UI.OpenGLWinformsWindowProvider, agg_platform_win32";
            AggContext.Config.ProviderTypes.OsInformationProvider = "MatterHackers.Agg.Platform.WinformsInformationProvider, agg_platform_win32";
        }
        else
        {
            EnsureGlfwProviderLoaded();
            AggContext.Config.ProviderTypes.SystemWindowProvider = "MatterHackers.GlfwProvider.GlfwWindowProvider, MatterHackers.GlfwProvider";
            AggContext.OsInformation = new MacOsInformationProvider();
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
            // Always output to console so the user can see what happened
            Console.Error.WriteLine($"GLOBAL ERROR: {ex.Message}");
            Console.Error.WriteLine(ex.StackTrace);

            try
            {
                string baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(baseDir))
                {
                    baseDir = Path.GetTempPath();
                }

                string logDir = Path.Combine(baseDir, "CSharpCAD", "logs");

                if (!Directory.Exists(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }
                string logPath = Path.Combine(logDir, "errors.log");
                string logMessage = $"[{DateTime.Now}] GLOBAL ERROR: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}{new string('=', 30)}{Environment.NewLine}";
                File.AppendAllText(logPath, logMessage);
            }
            catch (Exception logEx)
            {
                Console.Error.WriteLine($"Failed to write to log file: {logEx.Message}");
            }
        }
    }
}
