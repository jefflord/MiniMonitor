using InputSimulatorStandard.Native;
using InputSimulatorStandard;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Http;
using System.IO;
using TextCopy;

namespace EdgeDevToolsAutomation
{
    public class EdgeAutomation
    {
        // Import necessary Windows API functions
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        private const int SW_RESTORE = 9; // Restores a minimized window

        [STAThread]
        public static void Main(string[] args)
        {
            string windowTitlePart = "YouTube Music";
            string processName = "msedge"; // Process name for Microsoft Edge

            Console.WriteLine($"Searching for Edge window containing '{windowTitlePart}'...");

            IntPtr edgeWindowHandle = IntPtr.Zero;
            Process edgeProcess = null;

            // Find the Edge process and its main window
            foreach (Process p in Process.GetProcessesByName(processName))
            {
                if (!string.IsNullOrEmpty(p.MainWindowTitle) && p.MainWindowTitle.Contains(windowTitlePart, StringComparison.OrdinalIgnoreCase))
                {
                    edgeProcess = p;
                    edgeWindowHandle = p.MainWindowHandle;
                    break;
                }
            }

            if (edgeWindowHandle == IntPtr.Zero)
            {
                Console.WriteLine($"Could not find an Edge window with '{windowTitlePart}' in its title.");
                return;
            }

            Console.WriteLine($"Found Edge window: '{edgeProcess.MainWindowTitle}' with handle: {edgeWindowHandle}");

            // Bring the window to the foreground
            Console.WriteLine("Bringing window to foreground...");
            ShowWindow(edgeWindowHandle, SW_RESTORE); // Ensure it's not minimized
            SetForegroundWindow(edgeWindowHandle);
            Thread.Sleep(300); // Give it a moment to activate

            var simulator = new InputSimulatorStandard.KeyboardSimulator();

            // 1. Hit F12 to open Developer Tools
            //Console.WriteLine("Hitting F12 to open Dev Tools...");
            //simulator.KeyPress(VirtualKeyCode.F12);


            // Use Ctrl+Shift+J to open the console, then Ctrl + ` to focus the text area
            Console.WriteLine("Opening Dev Console with Ctrl+Shift+J and focusing with Ctrl+`");

            simulator.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT }, // Modifier keys
                VirtualKeyCode.VK_J);

            Thread.Sleep(500); // Wait for console to open

            // Simulate Ctrl + ` (backtick/backquote) to focus the console input area
            simulator.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.OEM_3);
            Thread.Sleep(300); // Wait for focus to shift

            // download this script so we can run it (http://localhost:8081/minimonitor/assets/main.js)
            // use a basic web request to download the script and save it locally
            string scriptUrl = "http://localhost:8081/minimonitor/assets/main.js";
            string localFilePath = Path.Combine(Path.GetTempPath(), "minimonitor.main.js");

            string jsCode = "";
            using (var http = new HttpClient())
            {
                // Synchronously download content (simpler for this console app)
                jsCode = http.GetStringAsync(scriptUrl).GetAwaiter().GetResult();
            }

            
            // Use clipboard + paste instead of typing the whole JS
            Console.WriteLine("Copying JS code to clipboard and pasting into DevTools console...");
            try
            {
                ClipboardService.SetText(jsCode);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to set clipboard text: {ex.Message}");
                return;
            }

            Thread.Sleep(100); // small delay to ensure clipboard is set
            // Paste with Ctrl+V
            simulator.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            Thread.Sleep(150); // Give it a moment to paste

            Console.WriteLine("Hitting Enter to run JS...");
            simulator.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(300); // Give JS time to execute

            // Now paste the watchForSongChangesAndSend call via clipboard as well
            string watchCall = "MyClass.watchForSongChangesAndSend();";
            Console.WriteLine($"Copying and pasting: {watchCall}");
            simulator.TextEntry(watchCall);
            
            Thread.Sleep(100);
            simulator.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
            Thread.Sleep(150);

            Console.WriteLine("Hitting Enter to run JS...");
            simulator.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(300); // Give JS time to execute

            // 4. Close Dev Tools by hitting F12 again
            Console.WriteLine("Hitting F12 to close Dev Tools...");
            simulator.KeyPress(VirtualKeyCode.F12);
            Thread.Sleep(300); // Give Dev Tools time to close

            Console.WriteLine("Automation complete.");
        }
    }
}