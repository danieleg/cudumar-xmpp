namespace Cudumar.Utils {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.InteropServices;
  using System.Text;
  using System.Windows;
  using System.Windows.Interop;
  using System.Windows.Media;

  public static class FullScreenHelper {
    #region Fields

    private const int MONITOR_DEFAULTTONEAREST = 2;

    #endregion Fields

    #region Methods

    public static void GoFullScreen(this Window window) {
      window.WindowStyle = WindowStyle.None;
      window.ResizeMode = ResizeMode.NoResize;
      WindowInteropHelper wih = new WindowInteropHelper(window);
      IntPtr hMonitor = MonitorFromWindow(wih.Handle, MONITOR_DEFAULTTONEAREST);
      MONITORINFOEX monitorInfo = new MONITORINFOEX();
      monitorInfo.cbSize = Marshal.SizeOf(monitorInfo);
      GetMonitorInfo(new HandleRef(window, hMonitor), monitorInfo);
      HwndSource source = HwndSource.FromHwnd(wih.Handle);
      if (source != null && source.CompositionTarget != null) {
        Matrix matrix = source.CompositionTarget.TransformFromDevice;
        RECT workingArea = monitorInfo.rcMonitor;
        Point dpiIndependentSize = matrix.Transform(new Point(workingArea.Right - workingArea.Left, workingArea.Bottom - workingArea.Top));
        window.MaxWidth = dpiIndependentSize.X;
        window.MaxHeight = dpiIndependentSize.Y;
        window.WindowState = WindowState.Maximized;
      }
    }

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(HandleRef hmonitor, [In, Out] MONITORINFOEX monitorInfo);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, int dwFlags);

    #endregion Methods

    #region Nested Types

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT {
      public int Left;
      public int Top;
      public int Right;
      public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private class MONITORINFOEX {
      public int cbSize;
      public RECT rcMonitor;
      public RECT rcWork;
      public int dwFlags;
      [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0x20)]
      public char[] szDevice;
    }

    #endregion Nested Types
  }
}