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
using Helpers;
using System;
using LibreHardwareMonitor.Hardware;
using System.Net;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace HelloPhotinoApp
{
    class Program
    {

        static Process process = null;
        private class Config
        {
            public int x { get; set; }
            public int y { get; set; }
            public string[]? icsFiles { get; set; }
        }




        [STAThread]
        static void Main(string[] args)
        {


            // Window title declared here for visibility
            string windowTitle = "MiniMonitor";

            // Creating a new PhotinoWindow instance with the fluent API
            var window = new PhotinoWindow()
                .SetTitle(windowTitle)
                // Resize to a percentage of the main monitor work area
                .SetUseOsDefaultSize(false)
                .SetSize(new Size(1940, 490))
                // Center window in the middle of the screen
                .Center()
                .SetIconFile(Path.GetFullPath("wwwroot\\assets\\Monitor-Tablet-icon.ico"))
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


            StartCalDataThread(window);

            StartSensorThread(window);

            window.WaitForClose(); // Starts the application event loop
        }

        private static int maxErrors = 10;
        private static int errorCount = 0;

        private static AutoResetEvent AutoResetEventForCalendar = new AutoResetEvent(false);

        private static async Task GetCalData(PhotinoWindow window)
        {

            var config = LoadConfig();



            using (var client = new WebClient())
            {
                // Provide the URL of the file to download

                // Provide the local path where the file will be saved
                string localFilePath = "calendar.ics";

                bool waitOneGotSignal = false;

                while (true)
                {
                    try
                    {
                        List<Occurrence> occurrencesForToday = new List<Occurrence>();
                        foreach (var icalUrl in config.icsFiles)
                        {
                            await client.DownloadFileTaskAsync(new Uri(icalUrl), localFilePath);

                            var calendar = Calendar.Load(File.ReadAllText(localFilePath));

                            occurrencesForToday.AddRange(calendar.GetOccurrences(DateTime.Now.AddMinutes(-10), DateTime.Now.AddDays(3)).Where(o => o.Period.StartTime.Date >= DateTime.Today).OrderBy(o => o.Period.StartTime).Take(6).ToList());
                        }



                        object data;
                        if (occurrencesForToday.Count() > 0)
                        {
                            foreach (var ev in occurrencesForToday)
                            {
                                var originalEvent = (CalendarEvent)ev.Source;

                                if (ev.Period.StartTime.AsUtc < DateTime.UtcNow.AddMinutes(-10))
                                {
                                    // too old, skip
                                    continue;
                                }

                                data = new object { };

                                data = new
                                {
                                    DataType = "CalendarData",
                                    HasEvents = true,
                                    Summary = originalEvent.Summary,
                                    StartTimeUtc = ev.Period.StartTime.AsUtc.ToString("O"),
                                    WaitOneGotSignal = waitOneGotSignal
                                };

                                window.SendWebMessage(JsonSerializer.Serialize(data));

                                break;
                            }
                        }
                        else
                        {
                            data = new
                            {
                                DataType = "CalendarData",
                                HasEvents = false
                            };
                            window.SendWebMessage(JsonSerializer.Serialize(data));
                        }



                        //
                        // Print the events for today
                        if (false)
                        {
                            foreach (var ev in occurrencesForToday)
                            {
                                var originalEvent = (CalendarEvent)ev.Source;
                                Console.WriteLine($"Summary: {originalEvent.Summary}");
                                Console.WriteLine($"Start Time: {ev.Period.StartTime}");
                                Console.WriteLine($"End Time: {ev.Period.EndTime}");
                                Console.WriteLine();
                            }
                        }
                        errorCount = 0;
                        waitOneGotSignal = AutoResetEventForCalendar.WaitOne(5 * 60 * 1000);

                    }
                    catch (Exception ex)
                    {
                        errorCount++;
                        Console.WriteLine($"Error downloading file: {ex.Message}");
                        Thread.Sleep(60000);
                    }
                }

            }

        }

        private static void StartCalDataThread(PhotinoWindow window)
        {
            new Thread(async () =>
            {
                await GetCalData(window);

            }).Start();

        }

        private static void StartSensorThread(PhotinoWindow window)
        {
            new Thread(() =>
            {
                Thread.Sleep(1);
                ResoreWindowPosition();

                Thread.Sleep(100);
                ResoreWindowPosition();


                if (false && File.Exists("OpenHardwareMonitorServer.exe"))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = "OpenHardwareMonitorServer.exe",
                        CreateNoWindow = true,        // Do not create a window for the process
                        UseShellExecute = false,      // Do not use the system shell to start the process
                        RedirectStandardOutput = true, // Redirect standard output so it won't block
                        RedirectStandardError = true   // Redirect standard error so it won't block
                    };

                    process = new Process
                    {
                        StartInfo = startInfo
                    };

                    process.Start(); // Start the process
                }

                // without _some_ delay it will crash on start 
                Thread.Sleep(1000);

                computer.Open();
                computer.IsGpuEnabled = true;
                computer.IsCpuEnabled = true;

                while (true)
                {
                    try
                    {
                        var data = GetSensorData();

                        window.SendWebMessage(data);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex);
                        Thread.Sleep(1000);
                    }

                }


                //HookWindowsEvents();
                //Thread.Sleep(1000);

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
        }

        static Computer computer = new Computer();

        private static string GetSensorData()
        {

            var gpu = computer.Hardware.First(x => x.HardwareType == HardwareType.GpuNvidia);
            var cpu = computer.Hardware.First(x => x.HardwareType == HardwareType.Cpu);

            gpu.Update();
            cpu.Update();

            var cpuTotal = cpu.Sensors.FirstOrDefault(y => y.Name == "CPU Total");

            return JsonSerializer.Serialize(new
            {
                DataType = "SensorData",
                temp = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Temperature).Value,
                load = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Load).Value,
                coreClock = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Clock).Value,
                memClock = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Memory" && s.SensorType == SensorType.Clock).Value,
                //fanPercent = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Fan" && s.SensorType == SensorType.Control).Value,
                //fanRpm = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU" && s.SensorType == SensorType.Fan).Value,
                cpuTotal = cpuTotal.Value
            });
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
                if (process != null && !process.HasExited)
                {
                    process.CloseMainWindow();
                    try
                    {
                        process.Kill();
                    }
                    catch { }

                }
                if (windowProcess != null)
                {
                    windowProcess.Kill();
                }
                Environment.Exit(0);
            }


            if (message == "UpdateCalendar")
            {
                AutoResetEventForCalendar.Set();
                return;
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
            if (message == "ToggleYTM")
            {
                ToggleYTM();
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

            var config = LoadConfig();
            config.x = x;
            config.y = y;

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
            Win32.SetWindowPos(mainWindowHandle, Win32.HWND_TOP, newX, newY, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);
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
            Win32.SetWindowPos(mainWindowHandle, Win32.HWND_TOP, newX, newY, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);
        }


        private static void ToggleYTM()
        {

            if (Win32.IsWindowVisible(windowHandle))
            {
                Win32.ShowWindow(windowHandle, Win32.SW_HIDE);
            }
            else
            {
                Win32.SetForegroundWindow(windowHandle);
                //Win32.ShowWindow(windowHandle, Win32.SW_SHOW);
                Win32.ShowWindow(windowHandle, Win32.SW_SHOWNORMAL);
            }
        }

        private static void FindYTM()
        {
            new Thread((x) =>
            {
                Thread.Sleep(250);
                try
                {
                    if (windowHandle == IntPtr.Zero)
                    {
                        var result = Win32.FindWindowByTitle("YouTube Music");
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
                        var result = Win32.FindWindowByTitle("YouTube Music");
                        windowHandle = result.mainWindowHandle;
                        windowProcess = result.process;
                    }

                    if (windowHandle != IntPtr.Zero)
                    {
                        //SetWindowPos(windowHandle, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

                        //ShowWindow(windowHandle, SW_SHOWNORMAL);

                        //SetWindowPos(windowHandle, IntPtr.Zero, -10000, -10000, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);


                        Win32.SetForegroundWindow(windowHandle);

                        //Win32.ShowWindow(windowHandle, Win32.SW_HIDE);
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


    }
}
