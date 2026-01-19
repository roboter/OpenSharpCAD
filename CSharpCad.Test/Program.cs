using System;
using System.Collections.Generic;
using CSharpCAD;
using MatterHackers.Csg;

namespace CSharpCad.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Running Compile Method Test...");

            // Provide the script content as if typed in the editor
            string script = "Draw(new Box(8, 20, 10));";

            ICompilerService compiler = new CompilerService();
            List<string> errors;

            var result = compiler.Compile(script, out errors);

            if (errors.Count > 0)
            {
                Console.WriteLine("Test Failed: Compilation Errors:");
                foreach (var err in errors)
                {
                    Console.WriteLine(err);
                }
                Environment.Exit(1);
            }

            if (result == null)
            {
                Console.WriteLine("Test Failed: Compile returned null without errors.");
                Environment.Exit(1);
            }

            Console.WriteLine("Compilation Successful.");

            // Optional: Call Render to ensure the dynamic object works
            try
            {
                // result is Test.RenderTest instance
                // We can't cast it easily because it's a dynamic type from a different context/assembly technically, 
                // but checking it's not null is the main step. 
                // However, we can use dynamic to invoke Render just to be sure it doesn't crash.
                dynamic dynamicRef = result;
                dynamicRef.Render();
                Console.WriteLine("Render() call successful.");
                Console.WriteLine("Test Passed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test Failed during Render calls: {ex.Message}");
                Environment.Exit(1);
            }
        }
    }
}
