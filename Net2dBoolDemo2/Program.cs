

using Net3dBoolDemo2;

namespace Net2dBoolDemo2
{
    class Program
    {
        static void Main(string[] args)
        {
            var demoWindow = new TDemoWindow();

            // Run the game at 60 updates per second
            demoWindow.Run(60.0);
        }
    }
}
