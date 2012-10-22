using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Reflection;

namespace Cudumar.Settings {
	static class Icons {
		public static readonly BitmapImage UserDefaultAvatarSource = Create("../../resources/user-no-avatar.png");
		public static readonly BitmapImage UserOnlineSource = Create("../../resources/dot-online.png");
		public static readonly BitmapImage UserAwaySource = Create("../../resources/dot-away.png");
		public static readonly BitmapImage UserBusySource = Create("../../resources/dot-busy.png");
		public static readonly BitmapImage UserUnavailableSource = Create("../../resources/dot-invisible.png");
		private static BitmapImage Create(string relPath) {
			BitmapImage result = LoadBitmapFromResource(relPath);
			if (!result.IsFrozen)
				result.Freeze();
			return result;
		}

		private static BitmapImage LoadBitmapFromResource(string pathInApplication, Assembly assembly = null) {
			if (assembly == null)
				assembly = Assembly.GetCallingAssembly();
			if (pathInApplication[0] == '/')
				pathInApplication = pathInApplication.Substring(1);
			return new BitmapImage(new Uri(@"pack://application:,,,/" + assembly.GetName().Name + ";component/" + pathInApplication, UriKind.Absolute));
		}
	}

	static class UiEnhancer {
		public static SolidColorBrush GetSolidBrush(Color c) {
			SolidColorBrush result = new SolidColorBrush(c);
			result.Freeze();
			return result;
		}

		public static void AttachSource(this Image image, ImageSource source, int? w, int? h) {
			if (source.CanFreeze)
				source.Freeze();
			image.Source = source;
			image.Width = w.HasValue ? w.Value : source.Width;
			image.Height = h.HasValue ? h.Value : source.Height;
		}

		public static readonly SolidColorBrush TransparentBrush = GetSolidBrush(Colors.Transparent);
		public static readonly SolidColorBrush WhiteBrush = GetSolidBrush(Colors.White);
		public static readonly SolidColorBrush DarkGrayBrush = GetSolidBrush(Colors.DarkGray);
		public static readonly SolidColorBrush LightGrayBrush = GetSolidBrush(Colors.LightGray);
		public static readonly SolidColorBrush RedBrush = GetSolidBrush(Colors.Red);
		public static readonly SolidColorBrush GreenBrush = GetSolidBrush(Colors.Green);
		public static readonly SolidColorBrush YellowBrush = GetSolidBrush(Colors.Gold);
	}
}
