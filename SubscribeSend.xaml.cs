using System.Windows;
using Base.Xmpp;
using Cudumar.Utils;

namespace Cudumar {
	public partial class SubscribeSend : Window {
		private readonly XmppClient client = null;
		public SubscribeSend(Window owner, XmppClient client) {
			InitializeComponent();
			this.client = client;
			this.Title = "Add friend";
			this.Owner = owner;
		}

		private void btClose_Click(object sender, RoutedEventArgs e) {
			this.Close();
		}

		private bool CheckConnection() {
			if (client == null || !client.IsConnected) {
				DialogUtils.ShowWarning(this, "You are no longer connected :(");
				return false;
			}

			return true;
		}
		private void btSend_Click(object sender, RoutedEventArgs e) {
			if (!CheckConnection()) {
				this.Close();
				return;
			}

			string to = txtTo.Text.Trim();
			if (string.IsNullOrWhiteSpace(to)) {
				DialogUtils.ShowWarning(this, "Please enter a valid name!");
				return;
			}

			client.SendSubscriptionRequest(to);
			this.Close();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e) {
			txtTo.Focus();
		}
	}
}
