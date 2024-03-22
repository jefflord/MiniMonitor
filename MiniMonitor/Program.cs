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
using HidSharp.Utility;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using static System.Collections.Specialized.BitVector32;
using System.Reflection;

namespace HelloPhotinoApp
{
    class Program
    {

        static Process process = null;
        static ChromeDriver driver = null;
        static PhotinoWindow window;
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
            window = new PhotinoWindow()
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

            StartChrome();

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
                Thread.Sleep(2000);
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

        //static IntPtr windowHandle = IntPtr.Zero;
        //static Process windowProcess = null;
        static (IntPtr mainWindowHandle, Process process) YTChromeWindow;



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
                if (YTChromeWindow.process != null)
                {
                    YTChromeWindow.process.Kill();
                }

                if (driver != null)
                {
                    driver.Close();
                }

                CleanupChrome();

                Environment.Exit(0);
            }


            if (message == "UpdateCalendar")
            {
                AutoResetEventForCalendar.Set();
                return;
            }
            if (message == "PlayPause")
            {
                YT_SendKey(Keys.Space);
                return;
            }
            if (message == "NextSong")
            {
                YT_SendKey("j");
                return;
            }
            if (message == "LastSong")
            {
                YT_SendKey("k");
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

            //if (message == "TestWebDriver")
            //{
            //    TestWebDriver();
            //    return;
            //}

            if (message == "ToggleYTM")
            {
                ToggleYTM();
                return;
            }

            //if (message == "FindYTM")
            //{
            //    ShowYTM();
            //    return;
            //}


            //window.Width = window.Width + 1;

            // The message argument is coming in from sendMessage.
            // "window.external.sendMessage(message: string)"
            string response = $"Received message: \"{message}\"";

            // Send a message back the to JavaScript event handler.
            // "window.external.receiveMessage(callback: Function)"
            window.SendWebMessage(response);

            response = $"Received message: \"{message}\"xxx";
        }

        private static void KillNamedProcseses(string[] processNames)
        {



            foreach (var name in processNames)
            {
                var pids = "";

                var quitTime = DateTime.UtcNow.AddSeconds(10);
                while (quitTime > DateTime.UtcNow && Process.GetProcessesByName(name).Length > 0)
                {

                    foreach (var process in Process.GetProcessesByName(name))
                    {

                        pids += $"/PID {process.Id} ";
                    }

                    if (pids.Length <= 0)
                    {
                        break;
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F {pids}",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    })?.WaitForExit();

                    if (Process.GetProcessesByName(name).Length > 0)
                    {
                        Thread.Sleep(100);
                    }
                    else
                    {
                        break;
                    }
                }
            }
        }

        private static void TestWebDriver()
        {
            //Edge();

            Chrome();

        }

        private static (IntPtr mainWindowHandle, Process process) Chrome()
        {
            CleanupChrome();

            var options = new ChromeOptions();

            // options.AddArguments(new List<string>() { "headless" });
            options.AddArguments(new List<string>() { "window-size=1920,1080" });
            options.AddArguments(new List<string>() { "--disable-info" });

            options.AddArguments(@"user-data-dir=C:\Users\jeff\AppData\Local\Google\Chrome\User Data");

            options.AddArguments(new List<string>() { $"--app=https://music.youtube.com/" });
            options.AddExcludedArgument("enable-automation");
            options.AddAdditionalOption("useAutomationExtension", false);

            driver = new ChromeDriver(options);
            //Debugger.Launch();
            var chromedriverFindWindowByTitleResult = Win32.FindWindowByTitle("chromedriver.exe", "chromedriver", null);
            if (chromedriverFindWindowByTitleResult.process != null)
            {
                if (Win32.IsWindowVisible(chromedriverFindWindowByTitleResult.process.MainWindowHandle))
                {
                    Win32.ShowWindow(chromedriverFindWindowByTitleResult.process.MainWindowHandle, Win32.SW_HIDE);
                }
            }

            while (YTChromeWindow.process == null)
            {
                YTChromeWindow = Win32.FindWindowByTitle("YouTube Music", "chrome", null);
                Thread.Sleep(100);
            }

            var js = File.ReadAllText(Path.GetFullPath(@"wwwroot\assets\main.js"));
            driver.ExecuteScript(js);

            return YTChromeWindow;

        }

        private static void CleanupChrome()
        {
            var p = Path.GetFullPath("chromedriver.exe");
            while (true)
            {
                var result = Win32.FindWindowByTitle(p, "chrome", null);
                if (result.process == null)
                {
                    break;
                }
                result.process.Kill();
                Thread.Sleep(100);
            }

            KillNamedProcseses(new string[] { "chromedriver", "chrome" });
        }

        private static void Edge()
        {
            var p = Path.GetFullPath("msedgedriver.exe");



            while (true)
            {
                var result = Win32.FindWindowByTitle(p, "msedge", null);
                if (result.process == null)
                {
                    break;
                }
                result.process.Kill();
                Thread.Sleep(100);
            }

            KillNamedProcseses(new string[] { "msedgedriver" });

            var options = new EdgeOptions();

            options.AddArguments(new List<string>() { "window-size=1280,800" });
            options.AddArguments(new List<string>() { "--disable-info" });


            //options.AddArguments("user-data-dir=C:\\Users\\jeff\\AppData\\Local\\Microsoft\\Edge\\User Data");

            //options.AddArguments("--user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36 Edg/122.0.0.0 Config/90.2.1101.2");

            options.AddArguments(new List<string>() { $"--app=https://music.youtube.com/" });
            //options.AddExcludedArgument("enable-automation");
            //options.AddAdditionalOption("useAutomationExtension", true);

            var driver = new EdgeDriver(options);

            try
            {
                //driver.Url = "https://music.youtube.com/";
                //--app=https://music.youtube.com/

                //var element = driver.FindElement(By.Id("sb_form_q"));
                //element.SendKeys("WebDriver");
                //element.Submit();


            }
            finally
            {
                //driver.Quit();

            }
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

        private static string GetCurrentSong()
        {
            var js = $"return Util.GetCurrentSong()";
            var song = driver.ExecuteScript(js) as string;
            return song;
        }

        private static void StartChrome()
        {

            var result = Chrome();


            var t = new Thread(() =>
            {

                while (true)
                {
                    Thread.Sleep(1000);
                    if (driver != null)
                    {
                        try
                        {
                            var song = GetCurrentSong();

                            var data = new
                            {
                                DataType = "MusicUpdate",
                                Song = song
                            };
                            window.SendWebMessage(JsonSerializer.Serialize(data));
                        }
                        catch (Exception e)
                        {
                            var data = new
                            {
                                DataType = "MusicUpdate",
                                Song = e.Message
                            };
                            window.SendWebMessage(JsonSerializer.Serialize(data));
                        }
                    }
                }

            });
            t.Start();

            YTChromeWindow = result;
        }
        private static void ToggleYTM()
        {

            if (YTChromeWindow.process == null)
            {
                return;
            }

            if (Win32.IsWindowVisible(YTChromeWindow.process.MainWindowHandle))
            {
                //Win32.ShowWindow(windowHandle, Win32.SW_HIDE);
                Win32.ShowWindow(YTChromeWindow.process.MainWindowHandle, Win32.SW_HIDE);
            }
            else
            {
                //Win32.SetForegroundWindow(windowHandle);
                Win32.SetForegroundWindow(YTChromeWindow.process.MainWindowHandle);
                //Win32.ShowWindow(windowHandle, Win32.SW_SHOW);
                //Win32.ShowWindow(windowHandle, Win32.SW_SHOWNORMAL);
                Win32.ShowWindow(YTChromeWindow.process.MainWindowHandle, Win32.SW_SHOWNORMAL);


                //Thread.Sleep(100);
                //var xx = Win32.IsWindowVisible(YTChromeWindow.process.MainWindowHandle);
                //if (!Win32.IsWindowVisible(YTChromeWindow.process.MainWindowHandle))
                //{
                //    // should be!
                //    YTChromeWindow = Chrome();
                //}
            }


        }

        //private static void ShowYTM()
        //{
        //    new Thread((x) =>
        //    {
        //        try
        //        {
        //            if (windowProcess == null)
        //            {
        //                var result = Win32.FindWindowByTitle("YouTube Music", "msedge", "--app=https://music.youtube.com/");

        //                if (result.process == null)
        //                {

        //                    StartYTMusicProcess();
        //                    Thread.Sleep(250);
        //                    result = Win32.FindWindowByTitle("YouTube Music", "msedge", "--app=https://music.youtube.com/");
        //                }


        //                windowHandle = result.mainWindowHandle;
        //                windowProcess = result.process;




        //            }
        //        }
        //        catch (Exception e)
        //        {
        //        }
        //    }).Start();

        //}

        //private static void StartYTMusicProcess()
        //{

        //    // not found, try to find old process with this cmd and kill it. 
        //    var process = Win32.FindProcessByCommandLineWMI("msedge", "--app=https://music.youtube.com/");
        //    if (process != null)
        //    {
        //        try
        //        {
        //            process.Kill();
        //        }
        //        catch (Exception ex) { }

        //    }


        //    ProcessStartInfo startInfo = new ProcessStartInfo
        //    {
        //        FileName = @"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe",
        //        Arguments = "--app=https://music.youtube.com/",
        //        //CreateNoWindow = true,        // Do not create a window for the process
        //        UseShellExecute = true,      // Do not use the system shell to start the process
        //                                     //RedirectStandardOutput = true, // Redirect standard output so it won't block
        //                                     //RedirectStandardError = true   // Redirect standard error so it won't block
        //    };

        //    process = new Process
        //    {
        //        StartInfo = startInfo
        //    };

        //    process.Start(); // Start the process


        //    windowHandle = 0;
        //    while (windowHandle == 0)
        //    {

        //        Thread.Sleep(50);
        //        try
        //        {
        //            var result = Win32.FindWindowByTitle("YouTube Music", "msedge", "--app=https://music.youtube.com/");
        //            windowHandle = result.mainWindowHandle;
        //            windowProcess = result.process;
        //        }
        //        catch (Exception ex)
        //        {

        //        }
        //    }


        //}

        private static void YT_SendKey(string keys)
        {

            if (driver != null)
            {
                //var getstring = GetStaticFieldValue(typeof(Keys), "Space");
                driver.FindElement(By.TagName("body")).SendKeys(keys);
            }

        }

        public static string GetStaticFieldValue(Type type, string propertyName)
        {
            FieldInfo field = type.GetField(propertyName, BindingFlags.Public | BindingFlags.Static);
            if (field != null)
            {
                return (string)field.GetValue(null);
            }
            else
            {
                throw new ArgumentException($"Property '{propertyName}' not found in class '{type.Name}'.");
            }
        }


    }
}
