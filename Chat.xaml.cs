using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Base.Xmpp;
using Base.Xmpp.Core;
using System.Windows.Documents;
using Cudumar.Core;
using System.Windows.Media;
using Cudumar.Settings;
using Cudumar.Utils;

namespace Cudumar
{
	public partial class Chat : Window {
		private readonly XmppClient client = null;
		private readonly RosterItem riTo = null;
		public string ToName { get; private set; }
		public string FromName { get; private set; }
		public string Thread { get; private set; }
		public bool IsEmptyChat { get; private set; }

		private DispatcherTimer tipComposing = new DispatcherTimer();
		public Chat(XmppClient client, RosterItem riTo, string thread) {
			InitializeComponent();
			this.client = client;
			this.riTo = riTo;
			ToName = riTo.Text;
			FromName = client.UserName;
			Thread = thread;
			Title = riTo.Text;
			IsEmptyChat = true;			

			tipComposing.Interval = new TimeSpan(0, 0, 5);
			tipComposing.IsEnabled = false;
			tipComposing.Tick += new EventHandler(tipComposing_Tick);

			RefreshPresence();
		}
		public void RefreshPresence() {
			if (riTo == null || riTo.Presence == null || riTo.Presence == XmppPresence.Empty)
				return;

			lbName.Text = riTo.Text == riTo.Jid.Value ? riTo.Text : string.Format("{0} <{1}>", riTo.Text, riTo.Jid.Value);
			lbPresence.Text = string.Format("{0}{1}",
				RosterUtils.Presence2Str(riTo.Presence.Status),
				string.IsNullOrWhiteSpace(riTo.Presence.MessageStatus) ? " " : Environment.NewLine + riTo.Presence.MessageStatus);
			RefreshAvatar();
			RefreshTooltip();
		}
		private void RefreshAvatar() {
			if (Avatar.Exist(riTo.Presence.PhotoHash))
				AvatarImg.AttachSource(Avatar.GetSource(riTo.Presence.PhotoHash), 32, 32);
			else
				AvatarImg.AttachSource(Icons.UserDefaultAvatarSource, 0, 0);
			
			AvatarBorder.BorderThickness = new Thickness(1);
			AvatarBorder.BorderBrush = RosterUtils.Presence2Brush(riTo.Presence.Status);
		}
		private void RefreshTooltip() {
			ToolTip tip = new ToolTip();
			tip.Content = RosterUtils.GetTooltip(riTo);
			tip.Background = UiEnhancer.WhiteBrush;
			tip.BorderBrush = UiEnhancer.DarkGrayBrush;
			tip.BorderThickness = new Thickness(1);
			tip.Placement = System.Windows.Controls.Primitives.PlacementMode.Right;
			tip.PlacementTarget = PresenceGrid;
			PresenceGrid.ToolTip = tip;
		}
	
		public void SignalMessageReceived(XmppMessage msg) {
			AppendMessage(msg, ToName);
			this.FlashWindow(5);
			lbBar.Text = string.Empty;
		}
		public void SetInput(bool enableInput) {
			txtMessage.IsEnabled = enableInput;
		}
		public void SetChatNotification(XmppChatStatus chatNotify) {
			string message = string.Empty;
			switch (chatNotify) {
				case XmppChatStatus.NowComposing:
					message = string.Format("{0} is typing a message", riTo.Jid.Bare); break;
				case XmppChatStatus.StopComposing:
					message = string.Empty; break;
				case XmppChatStatus.Gone:
					message = string.Format("{0} has left the conversation", riTo.Jid.Bare); break;
				//case XmppChatStatus.Inactive:
				//case XmppChatStatus.Active:
			}

			lbBar.Text = message;
		}

		private void AppendMessage(XmppMessage msg, string displayName) {
			int index = displayName.IndexOf('@');
			if (index > 0)
				displayName = displayName.Remove(index);

			IsEmptyChat = false;
			Paragraph para = chatContent.Blocks.LastBlock as Paragraph;
			if (para == null || para.Tag as string != msg.From.Bare) {
				para = new Paragraph();
				para.Padding = new Thickness(1);
				para.Margin = new Thickness(1, 1, 1, 3);
				para.Tag = msg.From.Bare;
				para.Inlines.Add(new Run("[" + DateTime.Now.ToShortTimeString() + "] <"));
				para.Inlines.Add(new Bold(new Run(displayName)));
				para.Inlines.Add(new Run(">"));

				chatContent.Blocks.Add(para);
			} 

			para.Inlines.Add(new Run(Environment.NewLine + msg.Body));
			lstMessages.ScrollToEnd();
		}
		private void SendMessage() {
			string message = txtMessage.Text;
			if (string.IsNullOrWhiteSpace(message)) {
				//@@@
				return;
			}

			XmppMessage msg = new XmppMessage();
			msg.To = new XmppJid(riTo.Jid.Bare);
			msg.From = new XmppJid(client.UserName);
			msg.Body = message;
			msg.Thread = Thread;
			msg.Type = "chat"; //@@@ enum

			client.SendMessage(msg);
			AppendMessage(msg, FromName);
			lbBar.Text = string.Empty;
			txtMessage.Text = string.Empty;
			txtMessage.Focus();
		}
		private void btSend_Click(object sender, RoutedEventArgs e) {
			if (client != null && client.IsConnected)
				SendMessage();
		}
		private void txtMessage_KeyDown(object sender, KeyEventArgs e) {
			if (tipComposing.IsEnabled == false) {
				if (client != null && client.IsConnected)
					client.SendNotification(riTo.Jid, Thread, XmppChatStatus.NowComposing);
				tipComposing.IsEnabled = true;
			} else {
				tipComposing.Stop();
				tipComposing.Start();
			}

			if (e.Key == Key.Return) {
				if (client != null && client.IsConnected)
					SendMessage();
				else {
					DialogUtils.ShowWarning(this, "Not connected :(");
				}
			}
		}
		void tipComposing_Tick(object sender, EventArgs e) {
			tipComposing.IsEnabled = false;
			if (client != null && client.IsConnected)
				client.SendNotification(riTo.Jid, Thread, XmppChatStatus.StopComposing);
		}

		private void Window_GotFocus(object sender, RoutedEventArgs e) {
			txtMessage.Focus();
			//client.SendNotification(ToUser, Thread, XmppChatStatus.Active);
		}
		private void Window_Loaded(object sender, RoutedEventArgs e) {
			txtMessage.Focus();
		}
		private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
			tipComposing.Stop();

			if (client != null && client.IsConnected && !IsEmptyChat)
				client.SendNotification(riTo.Jid, Thread, XmppChatStatus.Gone);

			e.Cancel = true;
			this.Hide();
		}
	}
}
