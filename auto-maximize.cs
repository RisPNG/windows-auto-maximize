using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Threading;

public class AutoWindowSizer
{
    #region Native Windows API Declarations

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    static extern bool IsZoomed(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int GetWidth()
        {
            return Right - Left;
        }

        public int GetHeight()
        {
            return Bottom - Top;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public uint cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    const uint MONITOR_DEFAULTTONEAREST = 2;
    const uint SWP_NOZORDER = 0x0004;
    const uint SWP_SHOWWINDOW = 0x0040;
    const int SW_MAXIMIZE = 3;
    const int SW_RESTORE = 9;

    #endregion

    private static IntPtr lastProcessedWindow = IntPtr.Zero;
    private static DateTime lastProcessedTime = DateTime.MinValue;
    private static bool lastWindowWasMaximized = false; // Track if the last checked state was maximized
    private const int PROCESS_COOLDOWN_MS = 200; // Cooldown to prevent rapid resizing

    public static void Main(string[] args)
    {
        Console.WriteLine("Auto Window Sizer started. Monitoring active windows...");
        Console.WriteLine("Press Ctrl+C to exit.\n");

        while (true)
        {
            try
            {
                ProcessActiveWindow();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex.Message);
            }

            Thread.Sleep(100); // Check every 100ms
        }
    }

    private static void ProcessActiveWindow()
    {
        IntPtr hwnd = GetForegroundWindow();

        if (hwnd == IntPtr.Zero)
            return;

        // Check if this is a new window
        bool isNewWindow = (hwnd != lastProcessedWindow);

        // Cooldown check: don't reprocess the same window too quickly (unless it's a new window)
        if (!isNewWindow && (DateTime.Now - lastProcessedTime).TotalMilliseconds < PROCESS_COOLDOWN_MS)
            return;

        // Get the monitor the window is on
        IntPtr hMonitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
        if (hMonitor == IntPtr.Zero)
            return;

        // Get monitor info
        MONITORINFO monitorInfo = new MONITORINFO();
        monitorInfo.cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO));

        if (!GetMonitorInfo(hMonitor, ref monitorInfo))
            return;

        RECT monitorRect = monitorInfo.rcWork; // Use work area (excludes taskbar)
        int monitorWidth = monitorRect.GetWidth();
        int monitorHeight = monitorRect.GetHeight();

        // Get current window size
        RECT windowRect;
        if (!GetWindowRect(hwnd, out windowRect))
            return;

        int windowWidth = windowRect.GetWidth();
        int windowHeight = windowRect.GetHeight();

        // Check if window is currently maximized
        bool isMaximized = IsZoomed(hwnd);

        // Calculate percentage of monitor size
        double widthPercent = (double)windowWidth / monitorWidth;
        double heightPercent = (double)windowHeight / monitorHeight;

        Console.WriteLine("Window: " + windowWidth + "x" + windowHeight + " | Monitor: " + monitorWidth + "x" + monitorHeight);
        Console.WriteLine("Coverage: " + (widthPercent * 100).ToString("F0") + "% width, " + (heightPercent * 100).ToString("F0") + "% height");
        Console.WriteLine("Maximized: " + isMaximized + " | Was Maximized: " + lastWindowWasMaximized);

        // Detect transition from maximized to unmaximized
        bool justUnmaximized = (!isMaximized && lastWindowWasMaximized && !isNewWindow);

        // Decision logic
        if (justUnmaximized && (widthPercent >= 0.95 || heightPercent >= 0.95))
        {
            // Window was just unmaximized and is still >90% - resize to 70%
            int targetWidth = (int)(monitorWidth * 0.70);
            int targetHeight = (int)(monitorHeight * 0.70);

            // Center the window on the monitor
            int targetX = monitorRect.Left + (monitorWidth - targetWidth) / 2;
            int targetY = monitorRect.Top + (monitorHeight - targetHeight) / 2;

            Console.WriteLine("-> Window unmaximized and still >95%, resizing to 70%: " + targetWidth + "x" + targetHeight + "\n");

            SetWindowPos(hwnd, IntPtr.Zero, targetX, targetY, targetWidth, targetHeight,
                       SWP_NOZORDER | SWP_SHOWWINDOW);

            lastProcessedWindow = hwnd;
            lastProcessedTime = DateTime.Now;
            lastWindowWasMaximized = false;
        }
        else if (widthPercent >= 0.95 && heightPercent >= 0.95 && !isMaximized)
        {
            // Window is 90% or more of monitor size - maximize it
            Console.WriteLine("-> Window is >95%, maximizing\n");
            ShowWindow(hwnd, SW_MAXIMIZE);
            lastProcessedWindow = hwnd;
            lastProcessedTime = DateTime.Now;
            lastWindowWasMaximized = true;
        }
        else
        {
            // Just update tracking
            if (isNewWindow)
            {
                lastProcessedWindow = hwnd;
            }
            lastWindowWasMaximized = isMaximized;
            lastProcessedTime = DateTime.Now;
            Console.WriteLine("-> No action needed\n");
        }
    }
}