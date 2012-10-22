using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Cudumar.Settings;

namespace Cudumar {
	/// <summary>
	/// Interaction logic for PasswordBoxAdorner.xaml
	/// </summary>
	public partial class PasswordBoxAdorner : UserControl {
		public PasswordBoxAdorner() {
			InitializeComponent();
			lbPlaceholder.FontSize = txtBox.FontSize - 1;
			lbPlaceholder.FontStyle = FontStyles.Italic;
			lbPlaceholder.Foreground = UiEnhancer.GetSolidBrush(Color.FromRgb(0xAA, 0xAA, 0xAA));
		}

		public string Placeholder {
			get { return (string)GetValue(PlaceholderProperty); }
			set { SetValue(PlaceholderProperty, value); }
		}
		public static readonly DependencyProperty PlaceholderProperty =
				DependencyProperty.Register("Placeholder", typeof(string), typeof(PasswordBoxAdorner),
				new UIPropertyMetadata(string.Empty, new PropertyChangedCallback(OnChangePlaceholder)));
		private static void OnChangePlaceholder(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PasswordBoxAdorner elem = d as PasswordBoxAdorner;
			if (elem != null)
				elem.hold();
		}

		public ImageSource IconSource {
			get { return (ImageSource)GetValue(IconSourceProperty); }
			set { SetValue(IconSourceProperty, value); }
		}
		public static readonly DependencyProperty IconSourceProperty =
				DependencyProperty.Register("IconSource", typeof(ImageSource), typeof(PasswordBoxAdorner), new UIPropertyMetadata(null, new PropertyChangedCallback(OnChangeSource)));
		private static void OnChangeSource(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PasswordBoxAdorner elem = d as PasswordBoxAdorner;
			elem.imgIcon.Source = elem.IconSource;
		}

		public double IconWidth {
			get { return (double)GetValue(IconWidthProperty); }
			set { SetValue(IconWidthProperty, value); }
		}
		public static readonly DependencyProperty IconWidthProperty =
				DependencyProperty.Register("IconWidth", typeof(double), typeof(PasswordBoxAdorner), new UIPropertyMetadata(0.0, new PropertyChangedCallback(OnChangeIconWidth)));
		private static void OnChangeIconWidth(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PasswordBoxAdorner elem = d as PasswordBoxAdorner;
			elem.imgIcon.Width = elem.IconWidth;
		}

		public double IconHeight {
			get { return (double)GetValue(IconHeightProperty); }
			set { SetValue(IconHeightProperty, value); }
		}
		public static readonly DependencyProperty IconHeightProperty =
				DependencyProperty.Register("IconHeight", typeof(double), typeof(PasswordBoxAdorner), new UIPropertyMetadata(0.0, new PropertyChangedCallback(OnChangeIconHeight)));
		private static void OnChangeIconHeight(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			PasswordBoxAdorner elem = d as PasswordBoxAdorner;
			elem.imgIcon.Height = elem.IconHeight;
		}

		public event RoutedEventHandler IconClick {
			add { AddHandler(IconClickEvent, value); }
			remove { RemoveHandler(IconClickEvent, value); }
		}
		public static readonly RoutedEvent IconClickEvent =
			EventManager.RegisterRoutedEvent("IconClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(PasswordBoxAdorner));


		public string Text {
			get {
				return txtBox.Password;
			}
			set {
				txtBox.Password = value;
			}
		}

		private void hold() {
			if (txtBox.Password == string.Empty) {
				lbPlaceholder.Text = Placeholder;
				lbPlaceholder.Visibility = System.Windows.Visibility.Visible;
			} else {
				lbPlaceholder.Visibility = System.Windows.Visibility.Hidden;
			}
		}

		private void Image_MouseDown(object sender, MouseButtonEventArgs e) {
			RaiseEvent(new RoutedEventArgs(IconClickEvent, null));
		}
		private void UserControl_GotFocus(object sender, RoutedEventArgs e) {
			txtBox.Focus();
		}
		private void txtBox_PasswordChanged(object sender, RoutedEventArgs e) {
			hold();
		}
	}
}
