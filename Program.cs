using PhotinoNET;
using System.Diagnostics;
using System.Drawing;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using WindowsInput;
using WindowsInput.Native;


namespace HelloPhotinoApp
{
    class Program
    {
        private class Config
        {
            public int x { get; set; }
            public int y { get; set; }
        }




        [STAThread]
        static void Main(string[] args)
        {
            // Window title declared here for visibility
            string windowTitle = "Photino for .NET Demo App";

            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                .SetTitle(windowTitle)
                // Resize to a percentage of the main monitor work area
                .SetUseOsDefaultSize(false)
                .SetSize(new Size(1940, 490))
                // Center window in the middle of the screen
                .Center()
                .SetIconFile(Path.GetFullPath("wwwroot\\assets\\dino.ico"))
                .SetChromeless(true)
                // Users can resize windows by default.
                // Let's make this one fixed instead.
                .SetResizable(false)
                .RegisterCustomSchemeHandler("app", (object sender, string scheme, string url, out string contentType) =>
                {
                    contentType = "text/javascript";

                    return new MemoryStream(Encoding.UTF8.GetBytes(@"
                        (() =>{
                            window.setTimeout(() => {
                                
                                //alert(`🎉 Dynamically inserted JavaScript.`);
                            }, 1000);
                        })();
                    "));
                })
                // Most event handlers can be registered after the
                // PhotinoWindow was instantiated by calling a registration 
                // method like the following RegisterWebMessageReceivedHandler.
                // This could be added in the PhotinoWindowOptions if preferred.
                .RegisterWebMessageReceivedHandler((object sender, string message) =>
                {
                    HandleMessage(sender, message);

                })
                .Load("wwwroot/index.html"); // Can be used with relative path strings or "new URI()" instance to load a website.


            new Thread(() =>
            {
                Thread.Sleep(100);
                ResoreWindowPosition();


                //HookWindowsEvents();

                Thread.Sleep(1000);

                //ResoreWindowPosition();


                //Process process = Process.GetCurrentProcess();
                //IntPtr mainWindowHandle = process.MainWindowHandle;
                //while (true)
                //{
                //    if (mainWindowHandle != IntPtr.Zero)
                //    {
                //        SetWindowPos(mainWindowHandle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);
                //        break;
                //    }
                //    Thread.Sleep(1000);
                //}

            }).Start();





            window.WaitForClose(); // Starts the application event loop
        }

        static IntPtr windowHandle = IntPtr.Zero;
        static Process windowProcess = null;

        //public static void HookWindowsEvents()
        //{
        //    // Hook the event
        //    SetWinEventHook(EVENT_SYSTEM_MINIMIZESTART, EVENT_SYSTEM_MINIMIZESTART,
        //                    IntPtr.Zero, WindowMinimized, 0, 0, WINEVENT_OUTOFCONTEXT);
        //}

        //public static void WindowMinimized(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
        //{
        //    // Minimize event - just return to ignore it
        //    return;
        //}




        private static void HandleMessage(object sender, string message)
        {
            var window = (PhotinoWindow)sender;


            if (message == "Close")
            {

                if (windowProcess != null)
                {
                    windowProcess.Kill();
                }
                Environment.Exit(0);
            }


            if (message == "PlayPause")
            {
                YT_SendKey(VirtualKeyCode.SPACE);
                return;
            }
            if (message == "NextSong")
            {
                YT_SendKey(VirtualKeyCode.VK_J);
                return;
            }
            if (message == "LastSong")
            {
                YT_SendKey(VirtualKeyCode.VK_K);
                return;
            }

            if (message == "MoveLeft")
            {
                MoveLeft();
                return;
            }
            if (message == "MoveRight")
            {
                MoveRight();
                return;
            }
            if (message == "MoveUp")
            {
                MoveUp();
                return;
            }
            if (message == "MoveDown")
            {
                MoveDown();
                return;
            }
            if (message == "ShowYTM")
            {
                ShowYTM();
                return;
            }
            if (message == "FindYTM")
            {
                FindYTM();
                return;
            }


            //window.Width = window.Width + 1;

            // The message argument is coming in from sendMessage.
            // "window.external.sendMessage(message: string)"
            string response = $"Received message: \"{message}\"";

            // Send a message back the to JavaScript event handler.
            // "window.external.receiveMessage(callback: Function)"
            window.SendWebMessage(response);

            response = $"Received message: \"{message}\"xxx";
        }

        //static Screen GetScreenFromPoint(Point point)
        //{
        //    foreach (Screen screen in Screen.AllScreens)
        //    {
        //        if (screen.Bounds.Contains((int)point.X, (int)point.Y))
        //        {
        //            return screen;
        //        }
        //    }
        //    return null;
        //}

        //static void MoveWindowToScreen(Window window, Screen targetScreen)
        //{
        //    // Move the window to the target screen
        //    window.Left = targetScreen.Bounds.X + 10;
        //    window.Top = targetScreen.Bounds.Y + 10;

        //    window.Show();
        //}

        private static void MoveLeft()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            {
                var monitorInfo = new Win32.MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);

                IntPtr monitor = Win32.MonitorFromWindow(mainWindowHandle, 0);
                Win32.GetMonitorInfo(monitor, ref monitorInfo);

                Console.WriteLine($"Current Screen: {monitorInfo.rcMonitor}");
            }
            //Screen currentScreen = GetScreenFromPoint(Mouse.GetPosition(null));





            Win32.RECT windowRect;
            Win32.GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX - 1;
            int newY = currentY;
            Win32.SetWindowPos(mainWindowHandle, Win32.HWND_TOP, newX, newY, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);
            SaveWindowLocation(newX, newY);
        }

        private static void SaveWindowLocation(int x, int y)
        {
            var config = new Config { x = x, y = y };
            string json = JsonSerializer.Serialize(config);

            // Write JSON to file 
            File.WriteAllText("config.json", json);
        }
        private static void ResoreWindowPosition()
        {
            var config = LoadConfig();
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            Win32.SetWindowPos(mainWindowHandle, Win32.HWND_TOP, config.x, config.y + 0, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);

        }




        private static Config LoadConfig()
        {
            if (File.Exists("config.json"))
            {
                string jsonRead = File.ReadAllText("config.json");

                // Deserialize 
                return JsonSerializer.Deserialize<Config>(jsonRead);
            }
            else
            {
                return new Config();
            }

        }

        private static void MoveRight()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            Win32.RECT windowRect;
            Win32.GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX + 1;
            int newY = currentY;
            Win32.
                        SetWindowPos(mainWindowHandle, Win32.HWND_TOP, newX, newY, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);
            SaveWindowLocation(newX, newY);
        }

        private static void MoveUp()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            Win32.RECT windowRect;
            Win32.GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX;
            int newY = currentY - 1;
            Win32.
                        SetWindowPos(mainWindowHandle, Win32.HWND_TOP, newX, newY, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);
            SaveWindowLocation(newX, newY);
        }



        private static void MoveDown()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            Win32.RECT windowRect;
            Win32.GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX;
            int newY = currentY + 1;
            Win32.
                        SetWindowPos(mainWindowHandle, Win32.HWND_TOP, newX, newY, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);
        }


        private static void ShowYTM()
        {
            Win32.SetForegroundWindow(windowHandle);
            Win32.ShowWindow(windowHandle, Win32.SW_SHOW);
        }

        private static void FindYTM()
        {
            new Thread((x) =>
            {
                try
                {
                    if (windowHandle == IntPtr.Zero)
                    {
                        var result = FindWindowByTitle("YouTube");
                        windowHandle = result.mainWindowHandle;
                        windowProcess = result.process;
                    }
                }
                catch (Exception e)
                {
                }
            }).Start();

        }

        private static void YT_SendKey(VirtualKeyCode key)
        {
            new Thread((x) =>
            {
                try
                {
                    var inputSimulator = new InputSimulator();


                    if (windowHandle == IntPtr.Zero)
                    {
                        var result = FindWindowByTitle("YouTube");
                        windowHandle = result.mainWindowHandle;
                        windowProcess = result.process;
                    }

                    if (windowHandle != IntPtr.Zero)
                    {
                        Win32.

                                                SetForegroundWindow(windowHandle);
                        Win32.
                                                //SetWindowPos(windowHandle, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

                                                //ShowWindow(windowHandle, SW_SHOWNORMAL);

                                                //SetWindowPos(windowHandle, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

                                                ShowWindow(windowHandle, Win32.SW_HIDE);

                        // Bring the window to the foreground



                        // Send keys to the active window
                        //inputSimulator.Keyboard.TextEntry("Hello, World!");
                        inputSimulator.Keyboard.KeyPress(key);

                    }
                }
                catch (Exception e)
                {

                }

            }).Start();
        }

        static (IntPtr mainWindowHandle, Process process) FindWindowByTitle(string title)
        {
            IntPtr hWnd = IntPtr.Zero;
            Process process = null;

            foreach (Process pList in Process.GetProcesses())
            {
                IntPtr h = pList.MainWindowHandle;
                process = pList;
                StringBuilder windowText = new StringBuilder(256);
                Win32.GetWindowText(h, windowText, 256);

                if (!string.IsNullOrEmpty(windowText.ToString()))
                {
                    Debug.WriteLine($"checking {windowText.ToString()}");
                    if (windowText.ToString().Contains(title, StringComparison.OrdinalIgnoreCase))
                    {
                        hWnd = h;
                        break;
                    }
                }
            }

            return (hWnd, process);
        }
    }
}
