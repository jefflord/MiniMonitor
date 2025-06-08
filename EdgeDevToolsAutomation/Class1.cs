using InputSimulatorStandard.Native;
using InputSimulatorStandard;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

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
            Thread.Sleep(30); // Give it a moment to activate

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
            Thread.Sleep(30); // Wait for focus to shift

            // 3. Add and run some JS
            string jsCode = @"if(typeof(MyClass)==='undefined'){!function(){var e=document.createElement(""script"");let t=window.trustedTypes.createPolicy(""myAppPolicy"",{createScriptURL:e=>e});e.src=t.createScriptURL(""http://localhost/minimonitor/assets/main.js""),e.onload=async function(){console.log(""Script 'main.js' has loaded successfully!""),MyClass.watchForSongChangesAndSend()},e.onerror=function(){alert(""Error loading script 'main.js'."")};var r=document.getElementsByTagName(""script"")[0];r.parentNode.insertBefore(e,r)}();} else {console.log('XXXXXX')}";

            Console.WriteLine($"Typing JS code: {jsCode}");
            simulator.TextEntry(jsCode);
            Thread.Sleep(30); // Give it a moment to type

            Console.WriteLine("Hitting Enter to run JS...");
            simulator.KeyPress(VirtualKeyCode.RETURN);
            Thread.Sleep(30); // Give JS time to execute

            // 4. Close Dev Tools by hitting F12 again
            Console.WriteLine("Hitting F12 to close Dev Tools...");
            simulator.KeyPress(VirtualKeyCode.F12);
            Thread.Sleep(30); // Give Dev Tools time to close


            //while (true)
            //{
            //    Thread.Sleep(30); // Give Dev Tools time to close
            //}

            Console.WriteLine("Automation complete.");
        }
    }
}