using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Base.Xmpp;
using Base.Xmpp.Core;
using Cudumar.Settings;
using Cudumar.Core;
using System.Windows.Threading;

namespace Cudumar {
	public partial class RosterItem : UserControl {
		private readonly XmppClient client = null;
		public XmppPresence Presence { get; private set; }
		public DateTime? PresenceUpdated { get; private set; }
		public XmppJid Jid { get; private set; }
		public string Text { get; private set; }
		public bool IsOnline { get { return Presence.Status != XmppPresenceStatus.Unavailable; } }
		public RosterViewerMode ViewMode = RosterViewerMode.Default;

		public RosterItem(XmppClient client, XmppRosterItem item, RosterViewerMode viewMode) {
			InitializeComponent();
			#region InitializeContextMenu
			/*
			MenuItem miVCard = new MenuItem() { Header = "Get vCard" };
			MenuItem miPing = new MenuItem() { Header = "Send Ping" };
			MenuItem miDInfo = new MenuItem() { Header = "Request disco#info" };
			miVCard.Click += (sender, e) => {	client.SendVCardRequest(Jid); };
			miPing.Click += (sender, e) => { client.SendPing(Jid); };
			miDInfo.Click += (sender, e) => { client.SendDiscoInfoRequest(Jid); };

			ContextMenu cm = new ContextMenu();
			cm.Items.Add(miVCard);
			cm.Items.Add(miPing);
			cm.Items.Add(miDInfo);
			this.ContextMenu = cm;
			*/
			#endregion

			this.client = client;
			this.Presence = XmppPresence.Empty;
			this.Jid = item.Jid;
			this.ViewMode = viewMode;
			this.Text = string.IsNullOrWhiteSpace(item.Name) ? item.Jid.Bare : item.Name;
			
			lbName.Text = Text;

			RefreshAvatar();
			RefreshTooltip();
		}

		public void UpdatePresence(XmppPresence presence) {
			Presence = presence;
			PresenceUpdated = DateTime.Now;
			RefreshPresence();
		}
		public void UpdateViewMode(RosterViewerMode viewMode) {
			ViewMode = viewMode;
			RefreshPresence();
		}
		public void RefreshPresence() {			
			if (Presence == null || Presence == XmppPresence.Empty)
				return;

			string more = RosterUtils.Presence2Str(Presence.Status);
			if (ViewMode == RosterViewerMode.BigAvatar && !string.IsNullOrWhiteSpace(Presence.MessageStatus))
				more += Environment.NewLine + Presence.MessageStatus;
			lbPresence.Text = more;

			RefreshAvatar();
			RefreshTooltip();
		}
		private void RefreshAvatar() {
			if (ViewMode == RosterViewerMode.Default) {
				AvatarImg.AttachSource(RosterUtils.Presence2Ico(Presence.Status), null, null);
				AvatarBorder.BorderThickness = new Thickness(0);
			} else if (ViewMode == RosterViewerMode.BigAvatar) {
				ImageSource avatar = Avatar.Exist(Presence.PhotoHash) ?
					Avatar.GetSource(Presence.PhotoHash) :
					Icons.UserDefaultAvatarSource;
				AvatarImg.AttachSource(avatar, 32, 32);
				AvatarBorder.BorderThickness = new Thickness(1);
				AvatarBorder.BorderBrush = RosterUtils.Presence2Brush(Presence.Status);
			}
		}
		public void RefreshTooltip() {
			ToolTip tip = new ToolTip();
			tip.Content = RosterUtils.GetTooltip(this);
			tip.Background = UiEnhancer.WhiteBrush;
			tip.BorderBrush = UiEnhancer.DarkGrayBrush;
			tip.BorderThickness = new Thickness(1);
			tip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
			tip.PlacementTarget = this;
			this.ToolTip = tip;
		}
		

		private void wrapper_MouseEnter(object sender, MouseEventArgs e) {
			this.Background = UiEnhancer.GetSolidBrush(Color.FromRgb(0xf0, 0xf0, 0xf0));
		}
		private void wrapper_MouseLeave(object sender, MouseEventArgs e) {
			this.Background = UiEnhancer.TransparentBrush;
		}
	}
}
