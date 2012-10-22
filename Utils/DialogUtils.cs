namespace Cudumar.Utils {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Text;
  using System.Windows;
using Microsoft.Win32;
using System.Windows.Threading;
	using System.Windows.Controls;

  public static class DialogUtils {
		public static string SelectFileToOpen(string filter, string initialDirectory) {
			ICollection<string> result = SelectFilesToOpenInternal(filter, false, initialDirectory);
			if (result == null || result.Count == 0)
				return null;
			return result.ElementAt(0);
		}
		public static ICollection<string> SelectFilesToOpen(string filter, string initialDirectory) {
			return SelectFilesToOpenInternal(filter, true, initialDirectory);
		}
		private static ICollection<string> SelectFilesToOpenInternal(string filter, bool multiselect, string initialDirectory) {
      var dialog = new OpenFileDialog() { Filter = filter, Multiselect = multiselect };
			if (!string.IsNullOrWhiteSpace(initialDirectory))
				dialog.InitialDirectory = initialDirectory;
      if (dialog.ShowDialog() == true)
        return dialog.FileNames;
      return null;
    }

		public static string SelectFileToSave(string defaultFileName, string filter, string defaultExt, string initialDirectory) {
      var dialog = new SaveFileDialog() {
        FileName = defaultFileName,
        InitialDirectory = initialDirectory != null ? initialDirectory : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
      };
      if (filter != null)
        dialog.Filter = filter;
      if (defaultExt != null)
        dialog.DefaultExt = defaultExt;
      if (dialog.ShowDialog() == true)
        return dialog.FileName;
       return null;
    }

    public static bool? ShowCancellableQuestion(DependencyObject owner, string message) {
      var result = ShowMessage(owner, message, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
      switch (result) {
        case MessageBoxResult.Yes:
          return true;
        case MessageBoxResult.No:
          return false;
        default:
          return null;
      }
    }
    public static bool ShowConfirmation(DependencyObject owner, string message) {
      var result = ShowMessage(owner, message, MessageBoxButton.OKCancel, MessageBoxImage.Warning);
      return result == MessageBoxResult.OK;
    }
    public static void ShowError(DependencyObject owner, string message) {
      ShowMessage(owner, message, MessageBoxButton.OK, MessageBoxImage.Error);
    }
    public static void ShowWarning(DependencyObject owner, string message) {
      ShowMessage(owner, message, MessageBoxButton.OK, MessageBoxImage.Warning);
    }
    public static void ShowException(DependencyObject owner, string message, Exception ex) {
      ShowMessage(owner, message + Environment.NewLine + Environment.NewLine + ex.GetType().ToString() + ": " + ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
    }
		public static void ShowException(DependencyObject owner, Exception ex) {
			ShowMessage(owner, ex.GetType().ToString() + ": " + ex.Message, MessageBoxButton.OK, MessageBoxImage.Error);
		}

    public static void ShowInformation(DependencyObject owner, string message) {
      ShowMessage(owner, message, MessageBoxButton.OK, MessageBoxImage.Information);
    }
		public static bool ShowQuestion(DependencyObject owner, string message) {
			return MessageBoxResult.Yes == ShowMessage(owner, message, MessageBoxButton.YesNo, MessageBoxImage.Question);
		}

    public static MessageBoxResult ShowMessage(DependencyObject owner, string message, MessageBoxButton button, MessageBoxImage icon) {
			Window window = null;
			if (owner != null)
				owner.Dispatcher.BeginInvoke(new Action(() => {	
					window = WindowsUtils.FindVisualParent<Window>(owner);
				}), DispatcherPriority.Normal);

			if (window == null) {
				return MessageBox.Show(message, "", button, icon);
			} else {
				return MessageBox.Show(window, message, "", button, icon);
			}			
    }

		public static void ShowIndentedContent(DependencyObject owner, string content, string title) {
			Window window = null;
			if (owner != null)
				owner.Dispatcher.BeginInvoke(new Action(() => {
					window = WindowsUtils.FindVisualParent<Window>(owner);
				}), DispatcherPriority.Normal);

			TextBox txt = new TextBox();
			txt.TextWrapping = TextWrapping.Wrap;
			txt.AppendText(UIHelper.XmlIndent(content));
			ScrollViewer sv = new ScrollViewer() { Content = txt };
			Window w = new Window() { Owner = window, Title = title };
			w.Content = sv;
			w.Height = 300;
			w.Width = 500;
			w.Show();
		}
  }
}