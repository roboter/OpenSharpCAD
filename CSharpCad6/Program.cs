using MatterHackers.Agg.Platform;
using System;
using System.Globalization;
using System.Threading;

namespace OpenCSharpCad;

class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // Set the global culture for the app, current thread and all new threads
        CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
        var (width, height) = RootSystemWindow.GetStartupBounds();

        var main = Application.LoadRootWindow(width, height);

        // Set default Agg providers
        AggContext.Config.ProviderTypes.SystemWindowProvider = "MatterHackers.GlfwProvider.GlfwWindowProvider, MatterHackers.GlfwProvider";

        main.ShowAsSystemWindow();
    }
}
