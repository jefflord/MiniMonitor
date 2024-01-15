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

        const int HWND_TOPMOST = -1;
        const int SW_HIDE = 0;
        const int SW_SHOWNORMAL = 1;
        const int SW_SHOW = 5;
        const int SWP_NOSIZE = 0x0001;
        const int SWP_NOMOVE = 0x0002;
        const int SWP_NOZORDER = 0x0004;
        const int SWP_SHOWWINDOW = 0x0040;
        const int HORZRES = 8;
        const int VERTRES = 10;


        const int WM_SYSCOMMAND = 0x0112;
        const int SC_MINIMIZE = 0xF020;


        [StructLayout(LayoutKind.Sequential)]
        public struct WindowMessage
        {
            public IntPtr hWnd;
            public uint msg;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public Point p;
        }

        static readonly IntPtr HWND_TOP = IntPtr.Zero;

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

        [DllImport("user32.dll")]
        static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        private static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;

        // Delegate for the hook callback
        private delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);


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
                MONITORINFO monitorInfo = new MONITORINFO();
                monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);

                IntPtr monitor = MonitorFromWindow(mainWindowHandle, 0);
                GetMonitorInfo(monitor, ref monitorInfo);

                Console.WriteLine($"Current Screen: {monitorInfo.rcMonitor}");
            }
            //Screen currentScreen = GetScreenFromPoint(Mouse.GetPosition(null));





            RECT windowRect;
            GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX - 1;
            int newY = currentY;

            SetWindowPos(mainWindowHandle, HWND_TOP, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
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
            SetWindowPos(mainWindowHandle, HWND_TOP, config.x, config.y + 0, 0, 0, SWP_NOSIZE | SWP_NOZORDER);

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
            RECT windowRect;
            GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX + 1;
            int newY = currentY;

            SetWindowPos(mainWindowHandle, HWND_TOP, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
            SaveWindowLocation(newX, newY);
        }

        private static void MoveUp()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            RECT windowRect;
            GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX;
            int newY = currentY - 1;

            SetWindowPos(mainWindowHandle, HWND_TOP, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
            SaveWindowLocation(newX, newY);
        }



        private static void MoveDown()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;
            RECT windowRect;
            GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX;
            int newY = currentY + 1;

            SetWindowPos(mainWindowHandle, HWND_TOP, newX, newY, 0, 0, SWP_NOSIZE | SWP_NOZORDER);
        }

        private static void ShowYTM()
        {
            SetForegroundWindow(windowHandle);
            ShowWindow(windowHandle, SW_SHOW);
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


                        SetForegroundWindow(windowHandle);
                        //SetWindowPos(windowHandle, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

                        //ShowWindow(windowHandle, SW_SHOWNORMAL);

                        //SetWindowPos(windowHandle, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

                        ShowWindow(windowHandle, SW_HIDE);

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
                GetWindowText(h, windowText, 256);

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
