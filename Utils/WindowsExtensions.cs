
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cudumar {
	public static class WindowExtensions {
		#region Window Flashing API Stuff

		private const UInt32 FLASHW_STOP = 0; //Stop flashing. The system restores the window to its original state.
		private const UInt32 FLASHW_CAPTION = 1; //Flash the window caption.
		private const UInt32 FLASHW_TRAY = 2; //Flash the taskbar button.
		private const UInt32 FLASHW_ALL = 3; //Flash both the window caption and taskbar button.
		private const UInt32 FLASHW_TIMER = 4; //Flash continuously, until the FLASHW_STOP flag is set.
		private const UInt32 FLASHW_TIMERNOFG = 12; //Flash continuously until the window comes to the foreground.

		[StructLayout(LayoutKind.Sequential)]
		private struct FLASHWINFO {
			public UInt32 cbSize; //The size of the structure in bytes.
			public IntPtr hwnd; //A Handle to the Window to be Flashed. The window can be either opened or minimized.
			public UInt32 dwFlags; //The Flash Status.
			public UInt32 uCount; // number of times to flash the window
			public UInt32 dwTimeout; //The rate at which the Window is to be flashed, in milliseconds. If Zero, the function uses the default cursor blink rate.
		}

		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

		#endregion

		public static void FlashWindow(this Window win, UInt32 count = UInt32.MaxValue) {
			//Don't flash if the window is active
			if (win.IsActive) return;

			WindowInteropHelper h = new WindowInteropHelper(win);

			FLASHWINFO info = new FLASHWINFO {
				hwnd = h.Handle,
				dwFlags = FLASHW_ALL | FLASHW_TIMER,
				uCount = count,
				dwTimeout = 0
			};

			info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
			FlashWindowEx(ref info);
		}
		public static void StopFlashingWindow(this Window win) {
			WindowInteropHelper h = new WindowInteropHelper(win);

			FLASHWINFO info = new FLASHWINFO();
			info.hwnd = h.Handle;
			info.cbSize = Convert.ToUInt32(Marshal.SizeOf(info));
			info.dwFlags = FLASHW_STOP;
			info.uCount = UInt32.MaxValue;
			info.dwTimeout = 0;

			FlashWindowEx(ref info);
		}

		public static void FadeIn(this Window win, string controlName, TimeSpan duration) {
			Storyboard storyboard = new Storyboard();

			// Create a DoubleAnimation to fade the not selected option control
			DoubleAnimation animation = new DoubleAnimation();

			animation.From = 0.0;
			animation.To = 1.0;
			animation.Duration = new Duration(duration);
			// Configure the animation to target de property Opacity
			Storyboard.SetTargetName(animation, controlName);
			Storyboard.SetTargetProperty(animation, new PropertyPath(Control.OpacityProperty));
			// Add the animation to the storyboard
			storyboard.Children.Add(animation);

			// Begin the storyboard
			storyboard.Begin(win);
		}

		public static void GotParentFocus(this Window win, FrameworkElement element) {
			FrameworkElement parent = (FrameworkElement)element.Parent;
			while (parent != null && parent is IInputElement && !((IInputElement)parent).Focusable) {
				parent = (FrameworkElement)parent.Parent;
			}

			DependencyObject scope = FocusManager.GetFocusScope(element);
			FocusManager.SetFocusedElement(scope, parent as IInputElement);
		}
	}

}
