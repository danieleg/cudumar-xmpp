using System.Windows;
using Base.Xmpp;
using Cudumar.Utils;

namespace Cudumar {
	public partial class SubscribeRecv : Window {
		private readonly XmppClient client = null;
		public string ToUser { get; private set; }
		public SubscribeRecv(Window owner, XmppClient client, string toUser) {
			InitializeComponent();
			this.Owner = owner;
			this.client = client;
			this.ToUser = toUser;			
			this.Title = toUser;
			lbMessage.Text = toUser + " requested your friendship! ";
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

		private void btRefuse_Click(object sender, RoutedEventArgs e) {
			if (!CheckConnection()) {
				this.Close();
				return;
			}

			client.RefuseSubscriptionRequest(ToUser);
			this.Close();
		}

		private void btApprove_Click(object sender, RoutedEventArgs e) {
			if (!CheckConnection()) {
				this.Close();
				return;
			}

			client.ApproveSubscriptionRequest(ToUser);
			this.Close();
		}
	}
}
