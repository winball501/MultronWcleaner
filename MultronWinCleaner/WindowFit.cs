using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MultronWinCleaner
{
    public static class WindowFit
    {
        public static void Register()
        {
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent, new RoutedEventHandler(OnLoaded));
        }

        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is Window window && e.OriginalSource == window && window is not AppDialog)
                Fit(window);
        }

        public static void Fit(Window window)
        {
            Rect? found = WorkArea(window);
            if (found is not Rect area || area.Width < 100 || area.Height < 100)
                return;

            window.MaxWidth = Math.Min(window.MaxWidth, area.Width);
            window.MaxHeight = Math.Min(window.MaxHeight, area.Height);
            window.MinWidth = Math.Min(window.MinWidth, area.Width);
            window.MinHeight = Math.Min(window.MinHeight, area.Height);
            if (window.WindowState != WindowState.Normal)
                return;

            bool resized = false;
            if (window.ActualWidth > area.Width)
            {
                window.Width = area.Width;
                resized = true;
            }
            if (window.ActualHeight > area.Height)
            {
                window.Height = area.Height;
                resized = true;
            }

            double width = Math.Min(window.ActualWidth, area.Width);
            double height = Math.Min(window.ActualHeight, area.Height);
            if (resized && window.WindowStartupLocation == WindowStartupLocation.CenterScreen)
            {
                window.Left = area.Left + (area.Width - width) / 2;
                window.Top = area.Top + (area.Height - height) / 2;
                return;
            }
            if (double.IsNaN(window.Left) || double.IsNaN(window.Top))
                return;
            window.Left = Math.Max(area.Left, Math.Min(window.Left, area.Right - width));
            window.Top = Math.Max(area.Top, Math.Min(window.Top, area.Bottom - height));
        }

        private static Rect? WorkArea(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            var source = PresentationSource.FromVisual(window);
            if (hwnd == IntPtr.Zero || source?.CompositionTarget == null)
                return SystemParameters.WorkArea;

            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (monitor == IntPtr.Zero || !GetMonitorInfo(monitor, ref info))
                return SystemParameters.WorkArea;

            var toWpf = source.CompositionTarget.TransformFromDevice;
            Point topLeft = toWpf.Transform(new Point(info.rcWork.Left, info.rcWork.Top));
            Point bottomRight = toWpf.Transform(new Point(info.rcWork.Right, info.rcWork.Bottom));
            return new Rect(topLeft, bottomRight);
        }

        private const int MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public int dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int flags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);
    }
}
