﻿using System.Diagnostics;
using System.Drawing;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

namespace Helpers
{
    internal static class Win32
    {

        internal const int SC_RESTORE = 0xF120; // Optional: To detect when it's shown again


        internal const int HWND_TOPMOST = -1;
        internal const int SW_HIDE = 0;
        internal const int SW_SHOWNORMAL = 1;
        internal const int SW_SHOWMINIMIZED = 2;
        internal const int SW_RESTORE = 9;
        internal const int SW_SHOW = 5;
        internal const int SWP_NOSIZE = 0x0001;
        internal const int SWP_NOMOVE = 0x0002;
        internal const int SWP_NOZORDER = 0x0004;
        internal const int SWP_SHOWWINDOW = 0x0040;
        internal const int HORZRES = 8;
        internal const int VERTRES = 10;


        internal const int WM_SYSCOMMAND = 0x0112;
        internal const int SC_MINIMIZE = 0xF020;

        private const uint WINEVENT_OUTOFCONTEXT = 0;
        private const uint EVENT_SYSTEM_MINIMIZESTART = 0x0016;

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

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        internal static readonly IntPtr HWND_TOP = IntPtr.Zero;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hDC, int nIndex);

        [DllImport("user32.dll")]
        internal static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        internal static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool IsIconic(IntPtr hWnd);


        // Delegate for the hook callback
        internal delegate void WinEventDelegate(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);


        public static Process FindProcessByCommandLineWMI(string processName, string cmdLine)
        {

            if (!processName.EndsWith(".exe"))
            {
                processName += ".exe";
            }
            string query = string.Format("SELECT CommandLine, ProcessId FROM Win32_Process WHERE Name = '{0}'", processName);
            ManagementObjectSearcher searcher = new ManagementObjectSearcher("root\\cimv2", query);

            foreach (ManagementObject process in searcher.Get())
            {
                if (process["CommandLine"] != null)
                {
                    string commandLine = process["CommandLine"].ToString();

                    if (commandLine.Contains(cmdLine, StringComparison.OrdinalIgnoreCase))
                    {
                        return Process.GetProcessById((int)(uint)process["ProcessId"]);
                    }
                }

            }

            return null; // Modify to return the process if found
        }

        internal static (IntPtr mainWindowHandle, Process process) FindWindowByTitle(string title, string name, string cmdLine)
        {
            IntPtr hWnd = IntPtr.Zero;
            Process process = null;

            for (var i = 0; i < 1; i++)
            {
                hWnd = FindWindow(null, title);
                if (hWnd != IntPtr.Zero)
                {

                    try
                    {
                        uint processId;
                        GetWindowThreadProcessId(hWnd, out processId);
                        process = Process.GetProcessById((int)processId);
                        return (hWnd, process);
                    }
                    catch (Exception ex)
                    {

                        Console.WriteLine(ex.Message);
                    }

                }
                else
                {
                    var processes = new Process[] { };
                    if (name == null)
                    {
                        processes = Process.GetProcesses();
                    }
                    else
                    {
                        processes = Process.GetProcessesByName(name);
                    }

                    foreach (Process pList in processes)
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
                                return (hWnd, process);
                            }
                        }
                    }
                }
            }

            return (IntPtr.Zero, null);
        }

        #region P/Invoke Declarations


        internal delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        internal static extern int GetWindowTextLength(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        internal const int SW_SHOWMAXIMIZED = 3;

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct WINDOWPLACEMENT
        {
            public int length;
            public int flags;
            public int showCmd;
            public POINT ptMinPosition;
            public POINT ptMaxPosition;
            public RECT rcNormalPosition;
        }

        #endregion
    }
}