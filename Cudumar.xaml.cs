using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Base.Xmpp;
using Base.Xmpp.Core;
using Base.Xmpp.Utils;
using Cudumar.Core;
using Cudumar.Settings;
using System.Threading.Tasks;
using System.IO;
using Cudumar.Utils;
using System.Windows.Controls.Primitives;
using Cudumar.Frameworks.OAuth2;
using System.Collections.ObjectModel;

namespace Cudumar
{
	public partial class CudumarMain : Window
	{
		public CudumarMain() {
			InitializeComponent();
			#region InitializeCommunicationHolder
			timerCommHolder = new DispatcherTimer();
			timerCommHolder.Interval = TimeSpan.FromSeconds(59);
			timerCommHolder.Stop();
			timerCommHolder.Tick += (sender, e) => {
				if (client != null) {
					if (client.IsConnected)
						client.SendWhitespace();
						//client.SendPing();
					else {
						Disconnect();
						DialogUtils.ShowInformation(this, "Disconnected from the server. Maybe the TCP connection has been lost in a limbo, or in some nook and cranny of the network. Do not worry, this happens :-( be sad.");
					}
				}
			};
			#endregion

			cmbPresence(cmbConnectPresence);
			cmbPresence(cmbRosterPresence);

			btConnect.Click += (sender, e) => { Connect(); };
			txtPassword.KeyDown += (sender, e) => { if (e.Key == Key.Enter) Connect(); };

			tracer = new Tracer<TraceStream>(null, "Xmpp Stream Viewer", true);
			logger = new Tracer<TraceLog>(null, "Log Viewer");
		}

		private XmppClient client = null;
		private UserSettings UserSetting = UserSettings.Empty;
		private UserSettingsProvider UserSettingProvider = new DummyUserSettingsProvider();
		private readonly RosterManager RosterMgr = new RosterManager();
		private readonly ChatManager ChatMgr = new ChatManager();		
		private readonly Signaller<XmppIqSignalEventArgs> Signals = new Signaller<XmppIqSignalEventArgs>();
		private readonly DispatcherTimer timerCommHolder = null;

		private void Connect() {
			logger.Clear();
			tracer.Clear();

			int port = -1;
			string host = txtServer.Text.Trim();
			int portIndex = host.IndexOf(':');
			if (portIndex <= 0)
				port = 5222;
			else {
				Int32.TryParse(host.Substring(portIndex + 1), out port);
				host = host.Remove(portIndex);
			}

			if (client != null)
				client.Dispose();
			client = new XmppClient(host, port);
			client.RecvStreamCallback += new EventHandler<XmppEventArgs>(client_RecvStreamCallback);
			client.SendStreamCallback += new EventHandler<XmppEventArgs>(client_SendStreamCallback);
			client.IqSignal += new EventHandler<XmppIqSignalEventArgs>(client_IqSignal);
			client.StateChange += new EventHandler<XmppStateChangeEventArgs>(client_StateChange);
			client.UserConnected += new EventHandler<XmppEventArgs>(client_UserConnected);
			client.UserDisconnected += new EventHandler<XmppEventArgs>(client_UserDisconnected);
			client.Roster += new EventHandler<XmppRosterEventArgs>(client_Roster);
			client.Message += new EventHandler<XmppMessageEventArgs>(client_Message);
			client.ChatNotify += new EventHandler<XmppChatNotifyEventArgs>(client_ChatNotify);
			client.SubscribeRequest += new EventHandler<XmppSubscribeRequestEventArgs>(client_SubscribeRequest);
			client.Presence += new EventHandler<XmppPresenceEventArgs>(client_Presence);
			client.VCard += new EventHandler<XmppVCartEventArgs>(client_VCard);
			client.Error += new EventHandler<XmppErrorEventArgs>(client_Error);

			string username = txtUser.Text.Trim();
			string password = txtPassword.Text.Trim();
			string serverName = null;
			string accessToken = null;
			if (host.Contains("facebook.com")) {
				serverName = "chat.facebook.com";
				//accessToken = new FacebookOAuth2Provider().GetAccessToken();
			} else if (host.Contains("hotmail.com") || host.Contains("live.com")) {
				serverName = "messenger.live.com";
				accessToken = new MessengerOAuth2Provider().GetAccessToken();
				if (accessToken == null) {
					ClearUI();
					return;
				}
			}

			client.Connect(username, password, "cudumar-xmpp", serverName, accessToken);
		}
		private void Disconnect() {
			UserSetting = UserSettings.Empty;
			ClearUI();
			if (client != null && client.State != XmppEngineState.Closed)
				client.Disconnect();
			Log("disconnected");
		}

		#region XMPP Stream Tracker & Logging
		private readonly Tracer<TraceStream> tracer = null;
		private readonly Tracer<TraceLog> logger = null;
		private void Trace(string stream, bool recv) {
			string type = (recv ? "RECV" : "SEND");
			Dispatcher.BeginInvoke(new Action(() => {
				tracer.Add(new TraceStream(type, stream));
			}), DispatcherPriority.Normal);
		}
		private void Log(string message) {
			Dispatcher.BeginInvoke(new Action(() => {
				logger.Add(new TraceLog(message));
			}), DispatcherPriority.Normal);
		}
		#endregion

		#region XMPP Event Handlers
		void client_SendStreamCallback(object sender, XmppEventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				Trace(e.Message, false);
			}), DispatcherPriority.Normal);
		}
		void client_RecvStreamCallback(object sender, XmppEventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				Trace(e.Message, true);
			}), DispatcherPriority.Normal);
		}
		void client_IqSignal(object sender, XmppIqSignalEventArgs e) {
			//Log(string.Format("ID {0} signal {1} {2}", e.ID, Environment.NewLine, UIHelper.XmlIndent(e.xStream.ToString())));
			Signals.Signal(e.ID, e);			
		}
		void client_StateChange(object sender, XmppStateChangeEventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				switch (e.NewState) {
					case XmppEngineState.Opening:
						btConnect.IsEnabled = false;
						break;
					case XmppEngineState.Open:
						btConnect.IsEnabled = false;
						break;
					case XmppEngineState.Initial:
					case XmppEngineState.Closed:
						btConnect.IsEnabled = true;
						break;
				}
			}), DispatcherPriority.Normal);
		}
		void client_UserConnected(object sender, XmppEventArgs e) {
			if (client == null || !client.IsConnected)
				return;

			UserSettingProvider = new CryptedLocalUserSettingsProvider(client.ServerName, client.Jid.Bare);
			UserSetting = UserSettingProvider.Load();
			Dispatcher.BeginInvoke(new Action(() => {
				UserSetting.PresenceStatus = cmbGetSelectedPresence(cmbConnectPresence);
				cmbSetPresence(cmbRosterPresence, UserSetting.PresenceStatus);

				ChatMgr.SetConversationInput(true);
				miLogout.IsEnabled = true;
				timerCommHolder.Start();
			}), DispatcherPriority.Normal);

			int id = client.GetRoster();
			Signals.Register(id, (par) => {
				Dispatcher.BeginInvoke(new Action(() => {
					SendPresence(cmbGetSelectedPresence(cmbConnectPresence), txtPresenceMessage.Text.Trim());
				}), DispatcherPriority.Normal);
				Dispatcher.DelayInvoke(TimeSpan.FromSeconds(8), () => {
					if (client != null && client.IsConnected) {
						if (!client.HostName.ToLower().Contains("facebook"))	// @@@ do not working with facebook... bug?
							client.SendVCardRequest(client.Jid);
					}
				});
			});
			
			Log("connected");
		}
		void client_UserDisconnected(object sender, XmppEventArgs e) { 
		}
		void client_Roster(object sender, XmppRosterEventArgs e) {
			if (e.Type != XmppIqType.Result)
				return;

			Log("roster received");
			bool flagRosterRecv = RosterMgr.Count() > 0;
			/*for (int i = 0; i < 1000; i++) {
				XmppRosterItem item = new XmppRosterItem(i.ToString() + "@test.com", "", "", "");
				e.Roster.Add(item);
			}*/

			Parallel.ForEach(e.Roster, item => {
				Dispatcher.BeginInvoke(new Action(() => {
					if ((item.Subscription == "none" || item.Subscription == "from") && item.Ask != "subscribe" && string.IsNullOrWhiteSpace(item.Name))
						return;

					RosterItem ri = new RosterItem(client, item, UserSetting.RosterVwrMode);					
					RosterMgr.Update(item.Jid, ri);
				}), DispatcherPriority.Normal);
			});			

			Dispatcher.BeginInvoke(new Action(() => {
				lstRoster.ItemsSource = RosterMgr.UpdateRosterView();

				if (flagRosterRecv == false)
					this.FadeIn(panelRoster.Name, TimeSpan.FromMilliseconds(500));
				
				panelConnect.Visibility = Visibility.Hidden;
				panelRoster.Visibility = Visibility.Visible;
				Log(string.Format("roster now contain {0} contacts", RosterMgr.Count()));
			}), DispatcherPriority.Normal);			
		}
		void client_Message(object sender, XmppMessageEventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				XmppMessage msg = e.Message;
				Chat chat = SingletonChat(msg.From, msg.Thread);
				if (chat != null) {
					chat.WindowState = (chat.IsEmptyChat && !chat.IsVisible ? WindowState.Minimized : WindowState.Normal);
					chat.Show();
					chat.SignalMessageReceived(msg);
				}
			}), DispatcherPriority.Normal);
		}
		void client_Presence(object sender, XmppPresenceEventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {				
				RosterItem ri = RosterMgr.Get(e.Presence.From);
				if (ri != null) {
					ri.UpdatePresence(e.Presence);

					Chat ci = ChatMgr.Get(e.Presence.From, null);
					if (ci != null)
						ci.RefreshPresence();

					RosterMgr.UpdateRosterView();
					Log(string.Format("{0} is {2} {3}", ri.Text, e.Presence.From.Bare, e.Presence.Status.ToString().ToLower(), e.Presence.MessageStatus));
				}
				
				string hash = e.Presence.PhotoHash;
				if (e.Presence.From.Bare == client.Jid.Bare) {
					if (!Avatar.Exist(hash))
						client.SendVCardRequest(client.Jid);
					else
						UpdateAvatar(hash);
				} else {
					if (e.Presence.Status != XmppPresenceStatus.Unavailable && !string.IsNullOrWhiteSpace(hash) && !Avatar.Exist(hash))
						if (!client.HostName.ToLower().Contains("facebook"))	// @@@ do not working with facebook... bug?
							client.SendVCardRequest(e.Presence.From);
				}
			}), DispatcherPriority.Normal);
		}
		void client_VCard(object sender, XmppVCartEventArgs e) {
			if (e == null || e.VCard == null)
				return;
			if (!string.IsNullOrWhiteSpace(e.VCard.PhotoBinVal)) {
				string data = Base64Utils.Decode(e.VCard.PhotoBinVal, System.Text.Encoding.Default);
				string hash = Avatar.ComputeHash(data);
				if (!Avatar.Exist(hash)) {
					Avatar.Save(hash, data);
					Dispatcher.BeginInvoke(new Action(() => {
						RosterItem ri = RosterMgr.Get(e.VCard.Jid);
						if (ri == null)
							return;

						ri.RefreshPresence();
						Chat ci = ChatMgr.Get(e.VCard.Jid, null);
						if (ci != null)
							ci.RefreshPresence();
					}), DispatcherPriority.Normal);
				}

				if (e.VCard.Jid != null) {
					if (string.IsNullOrWhiteSpace(e.VCard.Jid.Bare) || e.VCard.Jid.Bare == client.Jid.Bare) {
						UpdateAvatar(hash);
					}
				}
			}

			UpdateVCard(e.VCard);
		}
		void client_ChatNotify(object sender, XmppChatNotifyEventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				Chat ci = ChatMgr.Get(e.From, e.Thread);
				if (ci != null)
					ci.SetChatNotification(e.ChatStatus);
			}), DispatcherPriority.Normal);
		}
		void client_SubscribeRequest(object sender, XmppSubscribeRequestEventArgs e) {
			Dispatcher.BeginInvoke(new Action(() => {
				new SubscribeRecv(this, client, e.From.Bare).ShowDialog();
				Log("friend request received from " + e.From.Bare);
			}), DispatcherPriority.Normal);
		}
		void client_Error(object sender, XmppErrorEventArgs e) {
			string uiMessage;
			switch (e.Type) {
				case XmppErrorType.SocketError:
					uiMessage = "Unable to contact server. ";
					break;
				case XmppErrorType.AuthUserNotAuthorized:
					uiMessage = "Unauthorized. Probably the username and password you entered are incorrect. ";
					break;
				case XmppErrorType.AuthModeNotSupported:
					uiMessage = "Unable to authenticate: no methods available. ";
					break;
				case XmppErrorType.ClientNotConnected:
					uiMessage = "Hey you're not connected :-( ";
					break;
				case XmppErrorType.RecvTooManyInvalidStream:
					uiMessage = "Ouch ouch ouch stream was messed up :-( ";
					break;
				case XmppErrorType.ConnectionClosedByServer:
					uiMessage = "Ouch ouch ouch server closed stream, end of games :-( ";
					break;
				case XmppErrorType.SocketStartSSLError:
					uiMessage = "Ouch ouch ouch SSL was messed up :-( ";
					break;
				case XmppErrorType.XmppStreamError:
					uiMessage = "Ohu nooo I've received a stream error for you :-( ";
					break;
				default:
					uiMessage = "Unknown error :-( Something has happened ... ";
					break;
			}

			if (!string.IsNullOrWhiteSpace(e.ErrorDescriptor))
				uiMessage += Environment.NewLine + Environment.NewLine + e.ErrorDescriptor;
			if (!string.IsNullOrWhiteSpace(e.Message))
				uiMessage += Environment.NewLine + Environment.NewLine + e.Message;

			DialogUtils.ShowError(this, uiMessage);
			Disconnect();
		}
		#endregion

		#region Roster Management
		private void ClearUI() {
			timerCommHolder.Stop();

			Dispatcher.BeginInvoke(new Action(() => {
				ChatMgr.SetConversationInput(false);
				AvatarImg.Source = null;
				Avatar.ClearCache();

				if (panelConnect.Visibility != Visibility.Visible)
					this.FadeIn(panelConnect.Name, new TimeSpan(0, 0, 0, 0, 500));
				panelConnect.Visibility = Visibility.Visible;
				panelRoster.Visibility = Visibility.Hidden;
				miLogout.IsEnabled = false;
				btConnect.IsEnabled = true;
			}), DispatcherPriority.Normal);

			ChatMgr.Clear();
			RosterMgr.Clear();
			Signals.Clear();
		}
		private Chat SingletonChat(XmppJid to, string thread) {
			Chat ci = ChatMgr.Get(to, thread);
			if (ci == null) {
				RosterItem ri = RosterMgr.Get(to);
				ci = new Chat(client, ri, thread);
				ci.Owner = this;
				ChatMgr.Update(to, ci);
			}

			return ci;
		}
		private void UpdateRosterView(RosterViewerMode mode) {
			UserSetting.RosterVwrMode = mode;
			UserSettingProvider.Save(UserSetting);
			Dispatcher.BeginInvoke(new Action(() => {
				RosterMgr.UpdateViewMode(UserSetting.RosterVwrMode);
			}), DispatcherPriority.Normal);
		}
		private void UpdateAvatar(string hash) {
			if (!Avatar.Exist(hash))
				return;
			UserSetting.VCardPhotoHash = hash;
			UserSettingProvider.Save(UserSetting);
			Dispatcher.BeginInvoke(new Action(() => {
				AvatarImg.AttachSource(Avatar.GetSource(hash), 32, 32);
			}), DispatcherPriority.Normal);
		}
		private void UpdateVCard(XmppVCard vCard) {
			if (vCard != null) {
				UserSetting.VCard = vCard;
				UserSettingProvider.Save(UserSetting);
			}
		}
		private void lstRoster_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
			RosterItem ri = lstRoster.SelectedItem as RosterItem;
			if (ri == null)
				return;

			Chat conv = SingletonChat(ri.Jid, Guid.NewGuid().ToString());
			if (conv != null) {
				conv.Show();
				conv.Focus();
			}			
		}
		private void AvatarImg_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
			XmppVCard vCard = UserSetting.VCard;
			if (Guard.AlertIfNull(vCard, this, "Unable to set avatar now, vCard not retrieved yet from the server. If the problem persists, the server may not support this feature."))
				return;

			string fileName = DialogUtils.SelectFileToOpen("All Avatar types (*.jpg,*.jpeg,*.gif,*.png)|*.jpg;*.jpeg;*.gif;*.png", null);
			if (string.IsNullOrWhiteSpace(fileName))
				return;

			string hash = null;
			try {
				Avatar.GenerateFromFile(fileName, out hash);
				vCard.PhotoType = "image/png";
				vCard.PhotoBinVal = Avatar.GetData(hash);
			}
			catch (Exception ex) {
				DialogUtils.ShowException(this, "Unable to set avatar :-( Internal error.", ex);
				return;
			}

			Log("avatar updating");
			int id = client.SendVCardUpdate(vCard);
			Signals.Register(id, (par) => {
				if (par.Type == XmppIqType.Result) {					
					UpdateAvatar(hash);
					Log("avatar updated");
				} else if (par.Type == XmppIqType.Error) {
					DialogUtils.ShowError(this, string.Format("Unable to set avatar :-( Server error, be sad! {0} {0} {1}", Environment.NewLine, ParseStreamError(par.xStream)));
				}
			});

		}		
		private void btSendSubRequest_Click(object sender, RoutedEventArgs e) {
			new SubscribeSend(this, client).ShowDialog();
		}
		#endregion

		#region Presence Management
		private void SendPresenceIfChanged(XmppPresenceStatus status, string message) {
			if (UserSetting.PresenceMessage != message || UserSetting.PresenceStatus != status)
				SendPresence(status, message);
		}
		private void SendPresence(XmppPresenceStatus status, string message) {
			if (client == null || !client.IsConnected)
				return;

			const int MaxMessageLength = 200;
			if (message.Length > MaxMessageLength)
				message = message.Remove(MaxMessageLength);
			message = System.Text.RegularExpressions.Regex.Replace(message, @"[\f\n\r\t\v<>&]", "");
			cmbSetPresence(cmbRosterPresence, status);
			txtPresenceMessage.Text = message;

			UserSetting.PresenceMessage = message;
			UserSetting.PresenceStatus = status;
			client.SetPresence(UserSetting.PresenceStatus, UserSetting.PresenceMessage, UserSetting.VCardPhotoHash);

			// 
			const int MaxResultToSave = 6;
			if (UserSetting.PastPresenceMessages == null)
				UserSetting.PastPresenceMessages = new List<string>();
			if (!string.IsNullOrWhiteSpace(UserSetting.PresenceMessage) && !UserSetting.PastPresenceMessages.Contains(UserSetting.PresenceMessage)) {
				UserSetting.PastPresenceMessages.Add(UserSetting.PresenceMessage);
				while (UserSetting.PastPresenceMessages.Count > MaxResultToSave)
					UserSetting.PastPresenceMessages.RemoveAt(0);
			}

			// save user presence setting
			UserSettingProvider.Save(UserSetting);

			// 
			this.GotParentFocus(txtPresenceMessage);
			Log(string.Format("you're {1} {2}", client.Jid.Bare, UserSetting.PresenceStatus.ToString().ToLower(), UserSetting.PresenceMessage));
		}
		private void cmbPresence(ComboBox cmb) {
			cmb.Items.Add(cmbPresenceItem(XmppPresenceStatus.Online, "Online", Icons.UserOnlineSource));
			cmb.Items.Add(cmbPresenceItem(XmppPresenceStatus.Away, "Away", Icons.UserAwaySource));
			cmb.Items.Add(cmbPresenceItem(XmppPresenceStatus.Busy, "Busy", Icons.UserBusySource));
			cmb.Items.Add(cmbPresenceItem(XmppPresenceStatus.Unavailable, "Invisible", Icons.UserUnavailableSource));
		}
		private ComboBoxItem cmbPresenceItem(XmppPresenceStatus status, string text, BitmapImage icon) {
			Image ico = new Image() { Source = icon, Margin = new Thickness(4, 0, 4, 0) };
			TextBlock label = new TextBlock() { Text = text };
			StackPanel sp = new StackPanel() { Orientation = Orientation.Horizontal, Tag = status };
			sp.Children.Add(ico);
			sp.Children.Add(label);
			return new ComboBoxItem() { Tag = status, Content = sp };
		}
		private XmppPresenceStatus cmbGetSelectedPresence(ComboBox cmb) {
			ComboBoxItem cmbItem = cmb.SelectedItem as ComboBoxItem;
			if (cmbItem == null || cmbItem.Tag == null || cmbItem.Tag.GetType() != typeof(XmppPresenceStatus))
				return XmppPresenceStatus.Online;

			return (XmppPresenceStatus)cmbItem.Tag;
		}
		private void cmbSetPresence(ComboBox cmb, XmppPresenceStatus status) {
			for (int i = 0; i < cmb.Items.Count; i++) {
				ComboBoxItem cmbItem = cmb.Items[i] as ComboBoxItem;
				if (cmbItem == null || cmbItem.Tag == null || cmbItem.Tag.GetType() != typeof(XmppPresenceStatus))
					return;

				if ((XmppPresenceStatus)cmbItem.Tag == status) {
					cmb.SelectedIndex = i;
					return;
				}
			}
		}
		private void cmbRosterPresence_SelectionChanged(object sender, SelectionChangedEventArgs e) {
			SendPresenceIfChanged(cmbGetSelectedPresence(cmbRosterPresence), txtPresenceMessage.Text.Trim());
		}
		#endregion

		#region ContextMenu Server Management
		private void txtServer_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			if (e.RightButton == MouseButtonState.Pressed) {
				ContextMenu ctxServer = ctxMenuServer();
				ctxServer.IsOpen = true;
			}
		}
		private void txtServer_IconClick(object sender, RoutedEventArgs e) {
			ContextMenu ctxServer = ctxMenuServer();
			ctxServer.IsOpen = true;
		}
		private ContextMenu ctxMenuServer() {
			MenuItem mGoogle = CreateServerMenuItemEntry(OnClickServerItem, "Google Talk (talk.google.com)", "talk.google.com", "Enter your gTalk username.");
			MenuItem mFacebook = CreateServerMenuItemEntry(OnClickServerItem, "Facebook Chat (chat.facebook.com)", "chat.facebook.com", "Enter your facebook username.");
			MenuItem mMsn = CreateServerMenuItemEntry(OnClickServerItemAndConnect, "Windows Live Messenger (xmpp.messenger.live.com)", "xmpp.messenger.live.com", "Enter your Live username.");
			//MenuItem mIcq = CreateServerMenuItemEntry(OnClickServerItem, "ICQ (xmpp.oscar.aol.com)", "xmpp.oscar.aol.com", "Enter your gTalk username.");
			
			ContextMenu result = new ContextMenu() { PlacementTarget = txtServer, Placement = PlacementMode.Bottom, VerticalOffset = 2 };			
			result.Items.Add(mGoogle);
			result.Items.Add(mFacebook);
			result.Items.Add(mMsn);
			//result.Items.Add(mIcq);
			return result;
		}
		private void OnClickServerItem(MenuItem menu) {
			if (menu != null && menu.Tag != null)
				txtServer.Text = menu.Tag.ToString();
		}
		private void OnClickServerItemAndConnect(MenuItem menu) {
			OnClickServerItem(menu);
			Connect();
		}
		private MenuItem CreateServerMenuItemEntry(Action<MenuItem> onClick, string header, string server, string tooltip) {
			MenuItem result = new MenuItem() { Header = header, Tag = server, ToolTip = tooltip };
			result.Click += (sender, e) => { onClick(result); };
			return result;
		}
		#endregion

		#region ContextMenu Presence Message Management
		private void txtPresenceMessage_PreviewMouseDown(object sender, MouseButtonEventArgs e) {
			if (e.RightButton == MouseButtonState.Pressed) {
				ContextMenu ctxPresenceMessage = ctxMenuPresenceMessage();
				if (ctxPresenceMessage != null)
					ctxPresenceMessage.IsOpen = true;
			}
		}
		private void txtPresenceMessage_IconClick(object sender, RoutedEventArgs e) {
			ContextMenu ctxPresenceMessage = ctxMenuPresenceMessage();
			if (ctxPresenceMessage != null && !ctxPresenceMessage.IsOpen)
				ctxPresenceMessage.IsOpen = true;
		}
		private void txtPresenceMessage_LostFocus(object sender, RoutedEventArgs e) {
			SendPresenceIfChanged(cmbGetSelectedPresence(cmbRosterPresence), txtPresenceMessage.Text.Trim());
		}
		private void txtPresenceMessage_KeyDown(object sender, KeyEventArgs e) {
			if (e.Key == Key.Return)
				this.GotParentFocus(txtPresenceMessage);
		}
		private ContextMenu ctxMenuPresenceMessage() {
			const int MaxResultToShow = 6;
			if (UserSetting.PastPresenceMessages == null || UserSetting.PastPresenceMessages.Count == 0)
				return null;

			ContextMenu result = new ContextMenu() { PlacementTarget = txtPresenceMessage, Placement = PlacementMode.Bottom, VerticalOffset = 2 };			
			for (int i = 0; i < UserSetting.PastPresenceMessages.Count; i++) {
				string message = UserSetting.PastPresenceMessages[i];
				MenuItem item = new MenuItem() { Header = message };
				item.Click += (sender, e) => {
					txtPresenceMessage.Text = message;
					SendPresenceIfChanged(cmbGetSelectedPresence(cmbRosterPresence), txtPresenceMessage.Text.Trim());
				};

				result.Items.Insert(0, item);
				if (i > MaxResultToShow)
					break;
			}

			return result;
		}
		#endregion
		
		#region MainMenu Management
		private void miLogout_Click(object sender, RoutedEventArgs e) {	Disconnect(); }
		private void miAbout_Click(object sender, RoutedEventArgs e) { new About().ShowDialog(); }
		private void miTracer_Click(object sender, RoutedEventArgs e) { tracer.Show(); }
		private void miClose_Click(object sender, RoutedEventArgs e) { Close();	}
		private void miLog_Click(object sender, RoutedEventArgs e) { logger.Show(); }
		private void miViewHideAvatars_Click(object sender, RoutedEventArgs e) {
			UpdateRosterView(RosterViewerMode.Default);
			miViewHideAvatars.IsChecked = true;
			miViewShowAvatars.IsChecked = false;
		}
		private void miViewShowAvatars_Click(object sender, RoutedEventArgs e) {
			UpdateRosterView(RosterViewerMode.BigAvatar);
			miViewHideAvatars.IsChecked = false;
			miViewShowAvatars.IsChecked = true;
		}
		private void miDiscoInfo_Click(object sender, RoutedEventArgs e) {
			if (client != null && client.IsConnected) {
				int id = client.SendDiscoInfoRequest();
				Signals.Register(id, (par) => {
					if (par.Type == XmppIqType.Result) {
						ViewXStream(par.xStream, "server disco#info");
					} else if (par.Type == XmppIqType.Error) {
						DialogUtils.ShowWarning(this, string.Format("There was an error recovering disco#info from the server. {0} {0} {1}", Environment.NewLine, ParseStreamError(par.xStream)));
					}
				});
			}
		}

		private void miSendPing_Click(object sender, RoutedEventArgs e) {
			if (client != null && client.IsConnected) {
				int id = client.SendPing();
				Signals.Register(id, (par) => {
					if (par.Type == XmppIqType.Error)
						DialogUtils.ShowWarning(this, string.Format("Eww, a server error was occured during ping response. {0} {0} {1}", Environment.NewLine, ParseStreamError(par.xStream)));
					else
						DialogUtils.ShowInformation(this, "Pong :-)");
				});
			} else DialogUtils.ShowInformation(this, "Not connected :-(");
		}

		private void miCheckLine_Click(object sender, RoutedEventArgs e) {
			if (client != null && client.IsConnected)
				DialogUtils.ShowInformation(this, string.Format("Connected to {0}:{1} | {2}", client.HostName, client.HostPort, client.ServerName));
			else DialogUtils.ShowInformation(this, "Not connected :-(");
		}
		private void miViewUserVCard_Click(object sender, RoutedEventArgs e) {
			if (client != null && client.IsConnected) {
				if (UserSetting.VCard == null) {
					if (DialogUtils.ShowConfirmation(this, "The vCard is not available. Request it to server?")) {
						RefreshAndViewVCard(client.Jid);
					}
				} else {
					ViewVCard(UserSetting.VCard);
				}
			} else
				DialogUtils.ShowWarning(this, "User not connected :-)");
		}
		private void miRequestUserVCard_Click(object sender, RoutedEventArgs e) {
			if (client != null && client.IsConnected) {
				RefreshAndViewVCard(client.Jid);
			} else
				DialogUtils.ShowWarning(this, "User not connected :-)");
		}
		private void ViewVCard(XmppVCard vCard) {
			Dispatcher.BeginInvoke(new Action(() => {
				string title = vCard.Jid.Bare + " vCard";
				string content = UIHelper.XmlIndent("<vCard>" + vCard.ToStream() + "</vCard>");
				DialogUtils.ShowIndentedContent(this, content, title);
			}), DispatcherPriority.Normal);
		}
		private void ViewXStream(XmppIQ xStream, string title = "") {
			Dispatcher.BeginInvoke(new Action(() => {
				string content = UIHelper.XmlIndent(xStream.ToString());
				DialogUtils.ShowIndentedContent(this, content, title);
			}), DispatcherPriority.Normal);
		}
		private void RefreshAndViewVCard(XmppJid jid) {
			int id = client.SendVCardRequest(jid);
			Signals.Register(id, (par) => {
				if (par.Type == XmppIqType.Error)
					DialogUtils.ShowWarning(this, string.Format("There was an error recovering vCard from the server. {0} {0} {1}", Environment.NewLine, ParseStreamError(par.xStream)));
				else
					ViewVCard(new XmppVCard(jid, par.xStream));
			});
		}

		private string ParseStreamError(XmppIQ xStream) {
			string tag, message;
			XmppError.Parse(xStream, out tag, out message);
			return tag + " " + message;
		}
		#endregion

		private void Window_Closing(object sender, CancelEventArgs e) {
			if (ChatMgr.CheckIfPendingChat()) {
				if (!DialogUtils.ShowQuestion(this, "You have open windows, are you sure you want to close the application?")) {
					e.Cancel = true;
					return;
				}
			}

			Disconnect();
			foreach (Window owned in OwnedWindows)
				owned.Close();

			ChatMgr.Clear();
			RosterMgr.Clear();

			if (tracer != null)
				tracer.Close();
			if (logger != null)
				logger.Close();
		}
	}
}
