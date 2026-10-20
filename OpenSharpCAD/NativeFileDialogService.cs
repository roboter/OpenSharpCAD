#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using MatterHackers.Agg.Platform;

namespace CSharpCAD
{
    /// <summary>
    /// Provides cross-platform native file open and save dialogs for macOS (osascript), Windows (PowerShell/WinForms), and Linux (zenity/kdialog).
    /// </summary>
    public static class NativeFileDialogService
    {
        public static string LastDirectoryUsed { get; set; } = Directory.GetCurrentDirectory();

        public static string? ShowOpenFileDialog(string title = "Open C# CAD Script", string filter = "C# CAD Files (*.cs;*.part)|*.cs;*.part|All Files (*.*)|*.*", string? initialDir = null)
        {
            initialDir = !string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir) ? initialDir : LastDirectoryUsed;
            Console.WriteLine($"[NativeFileDialog] ShowOpenFileDialog: title='{title}', initialDir='{initialDir}'");

            string? selectedPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                selectedPath = ShowMacOpenDialog(title, initialDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                selectedPath = ShowWindowsOpenDialog(title, filter, initialDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                selectedPath = ShowLinuxOpenDialog(title, initialDir);
            }

            Console.WriteLine($"[NativeFileDialog] ShowOpenFileDialog result: '{selectedPath}'");

            if (!string.IsNullOrEmpty(selectedPath) && File.Exists(selectedPath))
            {
                LastDirectoryUsed = Path.GetDirectoryName(selectedPath) ?? LastDirectoryUsed;
                return selectedPath;
            }

            return null;
        }

        public static string? ShowSaveFileDialog(string title = "Save C# CAD Script", string defaultName = "model.cs", string? initialDir = null)
        {
            initialDir = !string.IsNullOrEmpty(initialDir) && Directory.Exists(initialDir) ? initialDir : LastDirectoryUsed;
            Console.WriteLine($"[NativeFileDialog] ShowSaveFileDialog: title='{title}', defaultName='{defaultName}', initialDir='{initialDir}'");

            string? selectedPath = null;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                selectedPath = ShowMacSaveDialog(title, defaultName, initialDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                selectedPath = ShowWindowsSaveDialog(title, defaultName, initialDir);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                selectedPath = ShowLinuxSaveDialog(title, defaultName, initialDir);
            }

            Console.WriteLine($"[NativeFileDialog] ShowSaveFileDialog result: '{selectedPath}'");

            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (string.IsNullOrEmpty(Path.GetExtension(selectedPath)))
                {
                    selectedPath += ".cs";
                }
                LastDirectoryUsed = Path.GetDirectoryName(selectedPath) ?? LastDirectoryUsed;
                return selectedPath;
            }

            return null;
        }

        private static string? ShowMacOpenDialog(string title, string initialDir)
        {
            try
            {
                string locArg = Directory.Exists(initialDir)
                    ? $" default location POSIX file \"{EscapeAppleScript(initialDir)}\""
                    : "";

                string script = $@"
try
    activate
    set chosenFile to choose file with prompt ""{EscapeAppleScript(title)}""{locArg}
    return POSIX path of chosenFile
on error number -128
    return """"
end try";

                return RunAppleScript(script);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing Mac open dialog: {ex.Message}");
                return null;
            }
        }

        private static string? ShowMacSaveDialog(string title, string defaultName, string initialDir)
        {
            try
            {
                string locArg = Directory.Exists(initialDir)
                    ? $" default location POSIX file \"{EscapeAppleScript(initialDir)}\""
                    : "";

                string script = $@"
try
    activate
    set chosenFile to choose file name with prompt ""{EscapeAppleScript(title)}"" default name ""{EscapeAppleScript(defaultName)}""{locArg}
    return POSIX path of chosenFile
on error number -128
    return """"
end try";

                return RunAppleScript(script);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing Mac save dialog: {ex.Message}");
                return null;
            }
        }

        private static string? RunAppleScript(string script)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "osascript",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var p = Process.Start(psi);
                if (p == null) return null;

                using (var sw = p.StandardInput)
                {
                    sw.Write(script);
                }

                string output = p.StandardOutput.ReadToEnd().Trim();
                string err = p.StandardError.ReadToEnd().Trim();
                p.WaitForExit();

                if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output;
                }
                else if (!string.IsNullOrEmpty(err))
                {
                    Console.WriteLine($"[AppleScript Error] code {p.ExitCode}: {err}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppleScript Exception] {ex.Message}");
            }

            return null;
        }

        private static string? ShowWindowsOpenDialog(string title, string filter, string initialDir)
        {
            try
            {
                string ps = $"Add-Type -AssemblyName System.Windows.Forms; $d = New-Object System.Windows.Forms.OpenFileDialog; $d.Title = '{title.Replace("'", "''")}'; $d.Filter = '{filter.Replace("'", "''")}'; $d.InitialDirectory = '{initialDir.Replace("'", "''")}'; if ($d.ShowDialog() -eq 'OK') {{ Write-Output $d.FileName }}";
                return RunProcessWithArgs("powershell", "-NoProfile", "-Command", ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing Windows open dialog: {ex.Message}");
                return null;
            }
        }

        private static string? ShowWindowsSaveDialog(string title, string defaultName, string initialDir)
        {
            try
            {
                string ps = $"Add-Type -AssemblyName System.Windows.Forms; $d = New-Object System.Windows.Forms.SaveFileDialog; $d.Title = '{title.Replace("'", "''")}'; $d.FileName = '{defaultName.Replace("'", "''")}'; $d.Filter = 'C# CAD Files (*.cs)|*.cs|All Files (*.*)|*.*'; $d.InitialDirectory = '{initialDir.Replace("'", "''")}'; if ($d.ShowDialog() -eq 'OK') {{ Write-Output $d.FileName }}";
                return RunProcessWithArgs("powershell", "-NoProfile", "-Command", ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error showing Windows save dialog: {ex.Message}");
                return null;
            }
        }

        private static string? ShowLinuxOpenDialog(string title, string initialDir)
        {
            try
            {
                string? res = RunProcessWithArgs("zenity", "--file-selection", $"--title={title}", $"--filename={initialDir}/");
                if (!string.IsNullOrEmpty(res)) return res;

                return RunProcessWithArgs("kdialog", "--getopenfilename", initialDir, "*.cs *.part|C# CAD Files (*.cs, *.part)", "--title", title);
            }
            catch
            {
                return null;
            }
        }

        private static string? ShowLinuxSaveDialog(string title, string defaultName, string initialDir)
        {
            try
            {
                string defaultPath = Path.Combine(initialDir, defaultName);
                string? res = RunProcessWithArgs("zenity", "--file-selection", "--save", "--confirm-overwrite", $"--title={title}", $"--filename={defaultPath}");
                if (!string.IsNullOrEmpty(res)) return res;

                return RunProcessWithArgs("kdialog", "--getsavefilename", defaultPath, "*.cs|C# CAD Files (*.cs)", "--title", title);
            }
            catch
            {
                return null;
            }
        }

        private static string? RunProcessWithArgs(string fileName, params string[] args)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                foreach (var arg in args)
                {
                    psi.ArgumentList.Add(arg);
                }

                using var p = Process.Start(psi);
                if (p == null) return null;

                string output = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit();

                if (p.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                {
                    return output;
                }
            }
            catch { }

            return null;
        }

        private static string EscapeAppleScript(string s) => s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    /// <summary>
    /// Bridges NativeFileDialogService with AggContext.FileDialogs to fulfill MatterHackers.Agg.Platform.IFileDialogProvider.
    /// </summary>
    public class CrossPlatformFileDialogProvider : IFileDialogProvider
    {
        public string LastDirectoryUsed => NativeFileDialogService.LastDirectoryUsed;

        public string ResolveFilePath(string path) => path;

        public bool OpenFileDialog(OpenFileDialogParams openParams, Action<OpenFileDialogParams> callback)
        {
            var result = NativeFileDialogService.ShowOpenFileDialog(
                openParams.Title,
                openParams.Filter,
                openParams.InitialDirectory);

            if (!string.IsNullOrEmpty(result))
            {
                openParams.FileName = result;
                openParams.FileNames = new[] { result };
                MatterHackers.Agg.UI.UiThread.RunOnIdle(() => callback?.Invoke(openParams));
                return true;
            }

            return false;
        }

        public bool SelectFolderDialog(SelectFolderDialogParams folderParams, Action<SelectFolderDialogParams> callback)
        {
            return false;
        }

        public bool SaveFileDialog(SaveFileDialogParams saveParams, Action<SaveFileDialogParams> callback)
        {
            var result = NativeFileDialogService.ShowSaveFileDialog(
                saveParams.Title,
                saveParams.FileName ?? "model.cs",
                saveParams.InitialDirectory);

            if (!string.IsNullOrEmpty(result))
            {
                saveParams.FileName = result;
                saveParams.FileNames = new[] { result };
                MatterHackers.Agg.UI.UiThread.RunOnIdle(() => callback?.Invoke(saveParams));
                return true;
            }

            return false;
        }

        public void ShowFileInFolder(string fileName) { }
    }
}
