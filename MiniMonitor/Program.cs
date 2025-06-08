using Helpers;
using HidSharp.Utility;
using Ical.Net;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using LibreHardwareMonitor.Hardware;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MiniMonitor;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using Photino.NET;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Net.WebSockets;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows;
using WindowsInput;
using WindowsInput.Native;
using static HelloPhotinoApp.Program;
using static Helpers.Win32;
using static System.Collections.Specialized.BitVector32;
using static System.Runtime.InteropServices.JavaScript.JSType;



namespace HelloPhotinoApp
{
    class Program
    {

        static Process process = null;
        static WebDriver driver = null;
        static PhotinoWindow window;
        private class Config
        {


            public int x { get; set; }
            public int y { get; set; }
            public string[]? icsFiles { get; set; }
            public string? openWeatherMapApi { get; set; }

            public string personalMailUserId { get; set; }
            public string personalMailPassword { get; set; }

        }

        public class SensorData
        {
            public string DataType { get; set; }
            public float? temp { get; set; }
            public float? gpuLoad { get; set; }
            public float? coreClock { get; set; }
            public float? memClock { get; set; }
            public float? cpuTotal { get; set; }
        }
        public class MusicData
        {
            public string DataType { get; set; } = "MusicData";
            public bool Success { get; set; }
            public string? Title { get; set; }
            public string? Artist { get; set; }
            public string? Album { get; set; }
            public string? AlbumArtUrl { get; set; }
            public string? Error { get; set; }
            public PlayerState PlayerState { get; set; }

        }
        public enum PlayerState
        {
            UNSTARTED = -1,
            ENDED = 0,
            PLAYING = 1,
            PAUSED = 2,
            BUFFERING = 3,
            CUED = 5
        }


        [STAThread]
        static void Main(string[] args)
        {

            //TestGetWeather();
            //return;

            // Window title declared here for visibility
            string windowTitle = "MiniMonitor";


            // Creating a new PhotinoWindow instance with the fluent API
            window = new PhotinoWindow()
                .SetTitle(windowTitle)
                // Resize to a percentage of the main monitor work area
                .SetUseOsDefaultSize(false)
                .SetUseOsDefaultLocation(false)
                .SetSize(new Size(1920, 720))
                .SetUserAgent("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/136.0.0.0 Safari/537.36 Edg/136.0.3240.64")
                //.SetLocation(new Point(500, 500))
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
                .Load("wwwroot/index-b.html"); // Can be used with relative path strings or "new URI()" instance to load a website.




            //StartChrome();

            ThreadPool.QueueUserWorkItem((x) =>
            {
                Thread.Sleep(1);
                StartServer(window);

                StartCalDataThread(window);

                StartWeatherThread(window);

                StartSensorThread(window);

                StayNormalThread(window);
            }
            );

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

                            var x = calendar.GetOccurrences(DateTime.UtcNow.AddHours(-24), DateTime.UtcNow.AddDays(3)).ToList();
                            var y = x.Where(o => o.Period.StartTime.Date >= DateTime.Today && o.Period.StartTime.HasTime).ToList();
                            var z = y.OrderBy(o => o.Period.StartTime).Take(6).ToList();

                            occurrencesForToday.AddRange(z);
                        }


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

                                calendarData = new
                                {
                                    DataType = "CalendarData",
                                    HasEvents = true,
                                    Summary = originalEvent.Summary,
                                    StartTimeUtc = ev.Period.StartTime.AsUtc.ToString("O"),
                                    WaitOneGotSignal = waitOneGotSignal
                                };

                                window.SendWebMessage(JsonSerializer.Serialize(calendarData));

                                break;
                            }
                        }
                        else
                        {
                            calendarData = new
                            {
                                DataType = "CalendarData",
                                HasEvents = false
                            };
                            window.SendWebMessage(JsonSerializer.Serialize(calendarData));
                        }



                        //
                        // Print the events for today
                        if (false)
                        {
                            foreach (var ev in occurrencesForToday)
                            {
                                var originalEvent = (CalendarEvent)ev.Source;
                                Log($"Summary: {originalEvent.Summary}");
                                Log($"Start Time: {ev.Period.StartTime}");
                                Log($"End Time: {ev.Period.EndTime}");
                                Log();
                            }
                        }

                        errorCount = 0;
                        waitOneGotSignal = AutoResetEventForCalendar.WaitOne(5 * 60 * 1000);
                    }
                    catch (Exception ex)
                    {
                        var data = new
                        {
                            DataType = "CalendarData",
                            HasEvents = true,
                            Summary = ex.Message,
                            StartTimeUtc = DateTime.UtcNow,
                            WaitOneGotSignal = waitOneGotSignal
                        };

                        window.SendWebMessage(JsonSerializer.Serialize(data));

                        errorCount++;
                        Log($"Error downloading file: {ex.Message}");
                        waitOneGotSignal = AutoResetEventForCalendar.WaitOne(60000);
                    }

                }

            }

        }

        private static void Log()
        {
            Console.WriteLine();
        }

        private static void Log(string message)
        {
            if (message == null)
            {
                return;
            }


            Console.WriteLine($"{DateTime.Now.ToString("O")}:{message.Trim()}\r\n");
            File.AppendAllText("log.txt", $"{DateTime.Now.ToString("O")}:{message.Trim()}\r\n");
        }

        private static void StartCalDataThread(PhotinoWindow window)
        {
            new Thread(async () =>
            {
                Thread.Sleep(5000);
                await GetCalData(window);

            }).Start();

        }

        private static void StartWeatherThread(PhotinoWindow window)
        {
            var config = LoadConfig();
            var api = config.openWeatherMapApi;
            var wf = new WeatherFetcher(api);

            ThreadPool.QueueUserWorkItem(async (x) =>
            {

                while (true)
                {
                    Thread.Sleep(2000);
                    weatherData = await wf.GetCurrentWeather("30157");

                    if (weatherData != null)
                    {

                        var jsonString = JsonSerializer.Serialize(weatherData);

                        window.SendWebMessage(jsonString);

                        // 10 mintues
                        Thread.Sleep(10 * 60 * 1000);
                    }
                    else
                    {
                        // try again in a minute
                        Thread.Sleep(1 * 60 * 1000);
                    }
                }
            });



        }

        // 
        private static void StayNormalThread(PhotinoWindow window)
        {
            Thread.Sleep(5000);
            new Thread(() =>
            {
                while (true)
                {
                    StayNormal();
                    Thread.Sleep(5000);
                }
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
                        sensorData = GetSensorData();
                        window.SendWebMessage(sensorData);
                        Thread.Sleep(1000);
                    }
                    catch (Exception ex)
                    {
                        Log(ex.ToString());
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

            //Debug.WriteLine("");

            //foreach (var xxx in gpu.Sensors.Where(s => s.Name.Contains("GPU") && s.SensorType == SensorType.Load).ToList())
            //{
            //    Debug.WriteLine($"name {xxx.Name}, val: {xxx.Value}");
            //}

            //return JsonSerializer.Serialize(new
            //{
            //    DataType = "SensorData",
            //    temp = 100,
            //    gpuLoad = 100,
            //    coreClock = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Clock).Value,
            //    memClock = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Memory" && s.SensorType == SensorType.Clock).Value,
            //    //fanPercent = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Fan" && s.SensorType == SensorType.Control).Value,
            //    //fanRpm = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU" && s.SensorType == SensorType.Fan).Value,
            //    cpuTotal = 100
            //});

            return JsonSerializer.Serialize(new
            {
                DataType = "SensorData",
                temp = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Temperature).Value,
                gpuLoad = gpu.Sensors.FirstOrDefault(s => s.Name == "GPU Core" && s.SensorType == SensorType.Load).Value,
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

        public static string yTwebDriverWindow { get; private set; }
        public static string mailwebDriverWindow { get; private set; }



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
                    try
                    {
                        driver.Close();
                    }
                    catch (Exception e)
                    {

                    }

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

            if (message == "SavePosition")
            {
                SavePosition();
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



            const bool useChrome = true;

            if (useChrome)
            {
                var options = new ChromeOptions();
                options.AddArguments(new List<string>() { "window-size=1920,1080" });
                options.AddArguments(new List<string>() { "--disable-info" });

                options.AddArguments(@"user-data-dir=C:\Users\jeff\AppData\Local\Google\Chrome\User Data");
                //options.BinaryLocation = @"C:\Users\jeff\AppData\Local\Chromium\Application\chrome.exe";
                //options.BinaryLocation = @"C:\Users\jeff\AppData\Local\ms-playwright\chromium-1155\chrome-win\chrome.exe";
                options.AddArguments(new List<string>() { $"--app=https://music.youtube.com/" });
                options.AddExcludedArgument("enable-automation");
                options.AddAdditionalOption("useAutomationExtension", false);
                driver = new ChromeDriver(options);
            }
            else
            {
                var options = new FirefoxOptions();
                options.AddArguments(new List<string>() { $"https://music.youtube.com/" });
                driver = new FirefoxDriver(options);
            }

            // options.AddArguments(new List<string>() { "headless" });


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

                Log($"Current Screen: {monitorInfo.rcMonitor}");
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
                var fileFullPath = Path.GetFullPath("config.json");
                string jsonRead = File.ReadAllText(fileFullPath);

                // Deserialize 
                return JsonSerializer.Deserialize<Config>(jsonRead);
            }
            else
            {
                return new Config();
            }

        }

        static private int GetWindowState(IntPtr hWnd)
        {
            WINDOWPLACEMENT placement = new WINDOWPLACEMENT();
            placement.length = Marshal.SizeOf(placement);
            if (GetWindowPlacement(hWnd, ref placement))
            {
                return placement.showCmd;
            }
            return 0; // Error case
        }

        private static void StayNormal()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;


            if (GetWindowState(mainWindowHandle) == SW_SHOWMINIMIZED)
            {
                ShowWindow(mainWindowHandle, Win32.SW_RESTORE);
            }
        }


        private static void SavePosition()
        {
            Process process = Process.GetCurrentProcess();
            IntPtr mainWindowHandle = process.MainWindowHandle;



            // Check if the window is minimized
            var xxx = GetWindowState(mainWindowHandle);

            if (GetWindowState(mainWindowHandle) == SW_SHOWMINIMIZED)
            {
                ShowWindow(mainWindowHandle, Win32.SW_RESTORE);
            }


            //if (AreAllTopLevelWindowsHidden(mainWindowHandle))
            //{
            //    Console.WriteLine("ASDF");
            //}

            Win32.RECT windowRect;
            Win32.GetWindowRect(mainWindowHandle, out windowRect);

            int currentX = windowRect.Left;
            int currentY = windowRect.Top;

            int newX = currentX;
            int newY = currentY;

            SaveWindowLocation(newX, newY);
        }

        static private bool AreAllTopLevelWindowsHidden(IntPtr currentHandle)
        {
            System.Collections.ArrayList windowHandles = new System.Collections.ArrayList();
            Win32.EnumWindows(new Win32.EnumWindowsProc(delegate (IntPtr hWnd, IntPtr lParam)
            {
                if (Win32.IsWindowVisible(hWnd) && Win32.GetWindowTextLength(hWnd) > 0)
                {
                    windowHandles.Add(hWnd);
                }
                return true;
            }), IntPtr.Zero);

            foreach (IntPtr hWnd in windowHandles)
            {
                if (hWnd != currentHandle && Win32.IsWindowVisible(hWnd))
                {
                    var placement = new Win32.WINDOWPLACEMENT();
                    placement.length = Marshal.SizeOf(placement);
                    Win32.GetWindowPlacement(hWnd, ref placement);
                    if (placement.showCmd == Win32.SW_SHOWNORMAL || placement.showCmd == Win32.SW_SHOWMAXIMIZED)
                    {
                        return false; // Another top-level window is still visible
                    }
                }
            }
            return true; // All other top-level windows seem to be hidden/minimized
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
            Win32.SetWindowPos(mainWindowHandle, Win32.HWND_TOP, newX, newY, 0, 0, Win32.SWP_NOSIZE | Win32.SWP_NOZORDER);
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


        private static void StartChrome()
        {

            var result = Chrome();

            var startMail = false;

            if (startMail)
            {
                driver.ExecuteScript(@"window.open(""https://outlook.live.com/mail"")");
            }
            else
            {
                mailwebDriverWindow = "NA";
            }

            while (yTwebDriverWindow == null || mailwebDriverWindow == null)
            {
                foreach (var handle in driver.WindowHandles)
                {
                    driver.SwitchTo().Window(handle);

                    var js = File.ReadAllText(Path.GetFullPath(@"wwwroot\assets\main.js"));
                    driver.ExecuteScript(js);

                    var host = driver.ExecuteScript(@"return window.location.host");

                    Thread.Sleep(100);

                    if (host.Equals("music.youtube.com"))
                    {
                        yTwebDriverWindow = handle;
                    }
                    else if (host.Equals("www.microsoft.com") || host.Equals("outlook.live.com"))
                    {
                        mailwebDriverWindow = handle;
                    }
                    else
                    {
                        mailwebDriverWindow = null;
                    }

                    //Console.WriteLine($"{handle} - {host}");
                }


            }


            var t = new Thread(() =>
            {

                while (true)
                {
                    Thread.Sleep(1000);
                    if (driver != null)
                    {

                        //GetMail();


                        RunYtJs("WatchCurrentSong()");
                        //GetCurrentSong();
                    }
                }

            });
            t.Start();

            YTChromeWindow = result;
        }

        private static object RunYtJs(string method)
        {
            try
            {
                lock (driver)
                {
                    var host = driver.ExecuteScript(@"return window.location.host");
                    Console.WriteLine(host);

                    if (driver.CurrentWindowHandle != yTwebDriverWindow)
                    {
                        driver.SwitchTo().Window(yTwebDriverWindow);
                    }

                    return driver.ExecuteScript($"return Util.{method}");
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
                return null;
            }
        }

        private static void GetCurrentSong()
        {
            try
            {
                lock (driver)
                {
                    var host = driver.ExecuteScript(@"return window.location.host");
                    Console.WriteLine(host);

                    if (driver.CurrentWindowHandle != yTwebDriverWindow)
                    {
                        driver.SwitchTo().Window(yTwebDriverWindow);
                    }

                    var songInfo = driver.ExecuteScript($"return Util.GetCurrentSong()") as string;

                    var data = new
                    {
                        DataType = "MusicUpdate",
                        Success = true,
                        Data = JsonDocument.Parse(songInfo)
                    };
                    window.SendWebMessage(JsonSerializer.Serialize(data));
                }
            }
            catch (Exception e)
            {
                Log(e.ToString());
                var data = new
                {
                    DataType = "MusicUpdate",
                    Success = false,
                    Error = e.Message
                };
                window.SendWebMessage(JsonSerializer.Serialize(data));
            }
        }


        public static async Task<string> ReadBodyAsync(HttpContext context)
        {
            context.Request.EnableBuffering();
            using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
            {
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                return body;
            }
        }

        private static async Task HandleWebSocketConnection(WebSocket webSocket)
        {
            // Example: Send a message every 3 seconds
            var buffer = new byte[1024];
            while (webSocket.State == WebSocketState.Open)
            {

                foreach (string msg in ytMessagesToSend.GetConsumingEnumerable())
                {
                    Debug.WriteLine($"[Consumer] Received: {msg}");
                    var bytes = System.Text.Encoding.UTF8.GetBytes(msg);
                    await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                    Debug.WriteLine($"Sent: {msg}");

                }

                //string message = $"Data from server: {DateTime.Now}";
                //var bytes = System.Text.Encoding.UTF8.GetBytes(message);
                //await webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true, CancellationToken.None);

                //Console.WriteLine($"Sent: {message}");
                //await Task.Delay(3000); // Wait for 3 seconds

                // You might also want to listen for messages from the client
                // var receiveResult = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                // if (receiveResult.MessageType == WebSocketMessageType.Text)
                // {
                //     string receivedMessage = System.Text.Encoding.UTF8.GetString(buffer, 0, receiveResult.Count);
                //     Console.WriteLine($"Received: {receivedMessage}");
                // }
            }
            Console.WriteLine("WebSocket disconnected.");
        }

        public static void StartServer(PhotinoWindow window)
        {

            new Thread(() =>
            {
                var myId = Process.GetCurrentProcess().Id;
                foreach (var process in Process.GetProcessesByName("MiniMonitor"))
                {
                    if (process.Id != myId)
                    {
                        try
                        {
                            process.CloseMainWindow();
                        }
                        catch (Exception e) { }
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception e) { }

                    }
                }

                var builder = WebApplication.CreateBuilder(new string[] { });

                builder.Logging.ClearProviders();
                //builder.Logging.SetMinimumLevel(LogLevel.Debug);

                builder.WebHost.ConfigureKestrel(options =>
                {
                    options.Listen(IPAddress.Loopback, 9191);
                });
                var app = builder.Build();


                // endpoint need to send CORS headers for OPTIONS verb
                app.MapMethods("/mini-monitor", new string[] { "OPTIONS" }, (HttpContext context) =>
                {
                    context.Response.Headers["Access-Control-Allow-Origin"] = "*";
                    context.Response.Headers["Access-Control-Allow-Headers"] = "Content-Type,Accept,isajax";
                    context.Response.Headers["Access-Control-Allow-Methods"] = "GET,POST,OPTIONS";
                    return Results.Ok();
                });


                app.MapPost("/mini-monitor", async (HttpContext context) =>
                {
                    context.Response.Headers["Access-Control-Allow"] = "*";
                    context.Response.Headers["Access-Control-Allow-Origin"] = "*";

                    var action = context.Request.Query["action"];
                    if (action == "putMusicData")
                    {
                        var body = await ReadBodyAsync(context);
                        try
                        {
                            musicData = JsonSerializer.Deserialize<MusicData>(body);
                            // Process musicData as needed
                            Log($"Received music data: {musicData.Title} - {musicData.Artist}");
                            return ConvertJsonToString(new { Success = true });
                        }
                        catch (JsonException ex)
                        {
                            Log($"Error deserializing music data: {ex.Message}");
                            return ConvertJsonToString(new { Error = "Invalid music data format" });
                        }
                    }
                    else if (action == "wireUpYT")
                    {
                        EdgeDevToolsAutomation.EdgeAutomation.Main(new string[] { });
                        return ConvertJsonToString(new { Success = true });
                    }
                    else if (action == "musicControl")
                    {
                        var body = await ReadBodyAsync(context);
                        try
                        {
                            var doc = JsonSerializer.Deserialize<JsonDocument>(body);

                            //var musicAction = doc.RootElement.GetProperty("action").GetString();

                            //if (musicAction == "pause")
                            //{

                            //}
                            //else
                            //{

                            //}

                            ytMessagesToSend.Add(body);

                            return ConvertJsonToString(new { Success = true });
                        }
                        catch (JsonException ex)
                        {
                            Log($"Error deserializing data: {ex.Message}");
                            return ConvertJsonToString(new { Error = ex.Message });
                        }
                    }
                    else
                    {
                        return ConvertJsonToString(new { Error = $"Unknown command {action}" });
                    }




                });


                app.MapGet("/ws", async (HttpContext context) =>
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        Console.WriteLine("WebSocket connected!");

                        // This is where you'll handle sending and receiving data
                        await HandleWebSocketConnection(webSocket);
                    }
                    else
                    {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                        await context.Response.WriteAsync("WebSocket request expected.");
                    }
                });




                app.MapGet("/mini-monitor", async (HttpContext context) =>
                        {
                            context.Response.Headers["Access-Control-Allow"] = "*";
                            context.Response.Headers["Access-Control-Allow-Origin"] = "*";

                            var action = context.Request.Query["action"];
                            if (action == "sensorData")
                            {
                                if (string.IsNullOrEmpty(sensorData))
                                {
                                    return ConvertJsonToString(new { NoData = true });
                                }
                                else
                                {
                                    return ConvertJsonToString(new
                                    {
                                        sensorData = ParseSensorData(sensorData),
                                        calendarData = calendarData,
                                        weatherData = weatherData,
                                        musicData = musicData
                                    });

                                }

                            }
                            else
                            {
                                window.SendWebMessage(context.Request.Query["data"]);
                                return "";
                            }
                        });




                app.UseWebSockets();
                app.Run();

            })
            { IsBackground = true }.Start();


        }

        private static void GetMail()
        {
            try
            {



                lock (driver)
                {
                    var config = LoadConfig();
                    if (driver.CurrentWindowHandle != mailwebDriverWindow)
                    {
                        driver.SwitchTo().Window(mailwebDriverWindow);
                    }

                    var mailIcon = driver.ExecuteScript(@"return document.querySelector(`i[data-icon-name=""MailRegular""]`)");
                    if (mailIcon == null)
                    {
                        var btn = driver.FindElement(By.CssSelector("#mectrl_headerPicture"));
                        btn.Click();


                        IWebElement input = null;

                        input = GetByCss("input[name=\"loginfmt\"]");
                        input.SendKeys(config.personalMailUserId);

                        btn = GetByCss("button[type=\"submit\"]");
                        btn.Click();

                        btn = GetByCss("div[aria-label=\"Personal account\"] > div > button");
                        btn.Click();

                        input = GetByCss("input[name=\"passwd\"]");
                        input.SendKeys(config.personalMailPassword);

                        btn = GetByCss("button[type=\"submit\"]");
                        btn.Click();

                        btn = GetByCss("button[type=\"submit\"]");
                        btn.Click();

                        btn = GetByCss("button[type=\"submit\"]", 1000, true, "No");
                        btn.Click();

                    }

                    driver.FindElement(By.CssSelector("body")).SendKeys("gi");


                    var mailboxInfo = driver.ExecuteScript($"return Util.GetMailboxInfo()") as string;


                    driver.ExecuteScript($"Util.TestDataPass()");





                    var fullPath = Path.GetFullPath("mailboxInfo.json");



                    File.WriteAllText("mailboxInfo.json", mailboxInfo);



                    Console.WriteLine(mailboxInfo);


                    // Unread GovHub Georgia Department of Revenue Payment Confirmation 6:24 AM No items selected


                }
            }
            catch (Exception ex)
            {

            }
        }

        private static IWebElement GetByCss(string css)
        {
            return GetByCss(css, 5000, false, null);
        }
        private static IWebElement GetByCss(string css, int waitTimeMs, bool okToNotFind, string innerText)
        {
            Exception exception = null;

            Exception timeoutException = new Exception($"Failed to find {css} in time.");

            if (okToNotFind)
            {
                timeoutException = null;
            }


            foreach (var x in HasTimeLeft(waitTimeMs, 100, timeoutException))
            {
                try
                {
                    var element = driver.FindElement(By.CssSelector(css));

                    if (innerText != null)
                    {
                        if (element.Text.Contains(innerText))
                        {
                            return element;
                        }
                    }
                    else
                    {
                        return element;
                    }



                }
                catch (NoSuchElementException e)
                {
                    exception = e;
                }
            }

            if (okToNotFind)
            {
                return null;
            }
            else
            {


                if (exception != null)
                {
                    throw exception;
                }
                else
                {
                    throw new Exception("What?");
                }
            }

        }

        private static int HasTimeLeftCount = 0;
        private static string sensorData;
        private static object calendarData;
        private static WeatherData weatherData;
        private static MusicData musicData = new MusicData();
        private static BlockingCollection<string> ytMessagesToSend = new BlockingCollection<string>();

        private static IEnumerable<bool> HasTimeLeft(int timeMs, int waitMs, Exception exception)
        {
            var timeout = DateTime.UtcNow.AddMilliseconds(timeMs);

            while (DateTime.UtcNow < timeout)
            {
                HasTimeLeftCount++;
                Thread.Sleep(waitMs);
                yield return (DateTime.UtcNow < timeout);
            }

            if (exception != null)
            {
                throw exception;
            }
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

        private static string ConvertJsonToString(object jsonObject)
        {
            return JsonSerializer.Serialize(jsonObject);
        }

        private static SensorData ParseSensorData(string sensorData)
        {
            if (string.IsNullOrEmpty(sensorData))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<SensorData>(sensorData);
            }
            catch (JsonException ex)
            {
                Log($"Error parsing sensorData: {ex.Message}");
                return null;
            }
        }


    }
}
