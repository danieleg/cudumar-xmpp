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
	/// Interaction logic for TextBoxAdorner.xaml
	/// </summary>
	public partial class TextBoxAdorner : UserControl {
		public TextBoxAdorner() {
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
				DependencyProperty.Register("Placeholder", typeof(string), typeof(TextBoxAdorner),
				new UIPropertyMetadata(string.Empty, new PropertyChangedCallback(OnChangePlaceholder)));
		private static void OnChangePlaceholder(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TextBoxAdorner elem = d as TextBoxAdorner;
			if (elem != null)
				elem.hold();
		}

		public ImageSource IconSource {
			get { return (ImageSource)GetValue(IconSourceProperty); }
			set { SetValue(IconSourceProperty, value); }
		}
		public static readonly DependencyProperty IconSourceProperty =
				DependencyProperty.Register("IconSource", typeof(ImageSource), typeof(TextBoxAdorner), new UIPropertyMetadata(null, new PropertyChangedCallback(OnChangeSource)));
		private static void OnChangeSource(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TextBoxAdorner elem = d as TextBoxAdorner;
			elem.imgIcon.Source = elem.IconSource;
		}

		public double IconWidth {
			get { return (double)GetValue(IconWidthProperty); }
			set { SetValue(IconWidthProperty, value); }
		}
		public static readonly DependencyProperty IconWidthProperty =
				DependencyProperty.Register("IconWidth", typeof(double), typeof(TextBoxAdorner), new UIPropertyMetadata(0.0, new PropertyChangedCallback(OnChangeIconWidth)));
		private static void OnChangeIconWidth(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TextBoxAdorner elem = d as TextBoxAdorner;
			elem.imgIcon.Width = elem.IconWidth;
		}

		public double IconHeight {
			get { return (double)GetValue(IconHeightProperty); }
			set { SetValue(IconHeightProperty, value); }
		}
		public static readonly DependencyProperty IconHeightProperty =
				DependencyProperty.Register("IconHeight", typeof(double), typeof(TextBoxAdorner), new UIPropertyMetadata(0.0, new PropertyChangedCallback(OnChangeIconHeight)));
		private static void OnChangeIconHeight(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			TextBoxAdorner elem = d as TextBoxAdorner;
			elem.imgIcon.Height = elem.IconHeight;
		}

		public event RoutedEventHandler IconClick {
			add { AddHandler(IconClickEvent, value); }
			remove { RemoveHandler(IconClickEvent, value); }
		}
		public static readonly RoutedEvent IconClickEvent =
			EventManager.RegisterRoutedEvent("IconClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TextBoxAdorner));


		public string Text {
			get {
				return txtBox.Text;
			}
			set {
				txtBox.Text = value;
			}
		}

		private void hold() {
			if (txtBox.Text == string.Empty) {
				lbPlaceholder.Text = Placeholder;
				lbPlaceholder.Visibility = System.Windows.Visibility.Visible;
			} else {
				lbPlaceholder.Visibility = System.Windows.Visibility.Hidden;
			}
		}

		private void txtBox_TextChanged(object sender, TextChangedEventArgs e) {
			hold();
		}
		private void Image_MouseDown(object sender, MouseButtonEventArgs e) {
			RaiseEvent(new RoutedEventArgs(IconClickEvent, null));
		}
		private void UserControl_GotFocus(object sender, RoutedEventArgs e) {
			txtBox.Focus();
		}
	}
}
