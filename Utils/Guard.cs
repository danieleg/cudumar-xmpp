using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace Cudumar.Utils {
	public static class Guard {
		public static bool AlertIfNull(object obj, DependencyObject owner, string message) {
			if (obj == null) {
				DialogUtils.ShowInformation(owner, message);
				return true;
			}

			return false;
		}
		public static T AlertIfInvalidAndConvert<T>(object obj, DependencyObject owner, string message) where T : class {
			T result = GetValueOrNull<T>(obj);
			if (result == null)
				DialogUtils.ShowInformation(owner, message);

			return result;
		}

		public static Nullable<T> SafeConvert<T>(object obj) where T : struct {
			if (obj == null || !(obj is T))
				return null;

			return (T)obj;
		}
		public static T GetValueOrNull<T>(object obj) where T : class {
			if (obj == null || !(obj is T))
				return null;

			return obj as T;
		}
		public static T GetValueOrDefault<T>(object obj, T defaultValue) where T : class {
			if (obj == null || !(obj is T))
				return defaultValue;

			return obj as T;
		}
	}

}
