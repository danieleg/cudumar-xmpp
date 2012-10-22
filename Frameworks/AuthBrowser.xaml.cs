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
using System.Windows.Shapes;
using System.Runtime.InteropServices;

namespace Cudumar.Frameworks {
	/// <summary>
	/// Interaction logic for AuthBrowser.xaml
	/// </summary>
	public partial class AuthBrowser : Window {
		private Uri returnUrl = null;
		private string username = "";
		private string password = "";

		public AuthBrowser(Uri beginUrl, Uri returnUrl, string username = "", string password = "") {
			InitializeComponent();

			//ChangeUserAgent();
			//myBrowser.Navigate(new Uri("http://whatsmyuseragent.com/"));
			browser.Navigate(beginUrl);			
			this.returnUrl = returnUrl;
			this.username = username;
			this.password = password;

			//txUrl.Visibility = Visibility.Collapsed;
		}		
		public Uri GetCurrentUri() {
			return new Uri(txUrl.Text);
		}
		private void browser_Navigating(object sender, System.Windows.Navigation.NavigatingCancelEventArgs e) {
			txUrl.Text = e.Uri.ToString();
		}
		private void browser_Navigated(object sender, System.Windows.Navigation.NavigationEventArgs e) {
			if (returnUrl != null && e.Uri.ToString().StartsWith(returnUrl.ToString())) {
				Close();
				browser.Dispose();
			}
		}
		~AuthBrowser() {
			browser.Dispose();
		}

		/*[DllImport("urlmon.dll", CharSet = CharSet.Ansi)]
		private static extern int UrlMkSetSessionOption(int dwOption, string pBuffer, int dwBufferLength, int dwReserved);
		public void ChangeUserAgent() {
			string ua = "Mozilla/5.0 (compatible; MSIE 9.0; Windows NT 6.1; Trident/5.0)";
			const int URLMON_OPTION_USERAGENT = 0x10000001;
			UrlMkSetSessionOption(URLMON_OPTION_USERAGENT, ua, ua.Length, 0);
		}*/
	}
}
