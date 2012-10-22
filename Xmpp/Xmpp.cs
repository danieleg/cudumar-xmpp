using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Windows;
using Base.Xmpp.Core;
using Base.Xmpp.Mechanism;
using Base.Xmpp.Net;
using Base.Xmpp.Utils;

namespace Base.Xmpp
{
	public enum XmppEngineState {
		Initial = 0,	// Initial state.
		Opening,			// Exchanging stream headers, authenticating and so on.
		Open,					// Authenticated and bound.
		Closed				// Session closed (due to error or logout).
	}
	public enum XmppErrorType {
		SocketError = 10,    // TCP Socket error.
		SocketStartSSLError = 11,

		AuthModeNotSupported = 20,
		AuthUserNotAuthorized = 21,
		ClientNotConnected = 30,
		ConnectionClosedByServer = 40,
		RecvTooManyInvalidStream = 50,
		XmppStreamError = 100,    // XMPP Stream error, see protocol Stream Error Definition.
		XmppStanzaError = 200     // XMPP Stanza error, see protocol Stanza Error Definition.
	}

	public class XmppClient : IDisposable
	{
		private readonly string SoftwareVersion = "cudumar-xmpp 0.0.3";
		private readonly string SoftwareNode = "http://code.google.com/p/cudumar-xmpp";

		protected XmppPresenceStatus statePresence = XmppPresenceStatus.Unavailable;
		protected XmppEngineState state = XmppEngineState.Initial;
		protected XmppSocket tcpClient = null;
		protected Thread engine = null;

		private readonly XmppDisco Disco;
		private readonly XmppCaps Caps;

		public XmppJid Jid { get; private set; }
		public string UserName { get; private set; }	
		public string UserPassword { get; private set; }
		public string UserResource { get; private set; }
		public string UserOAuthToken { get; private set; }
		public string ServerName { get; private set; }
		public string HostName { get; private set; }
		public int HostPort { get; private set; }
		public XmppEngineState State { get { return state; } }

		// events
		public event EventHandler<XmppEventArgs> SendStreamCallback;
		public event EventHandler<XmppEventArgs> RecvStreamCallback;
		public event EventHandler<XmppEventArgs> UserConnected;
		public event EventHandler<XmppEventArgs> UserDisconnected;
		public event EventHandler<XmppRosterEventArgs> Roster;
		public event EventHandler<XmppMessageEventArgs> Message;
		public event EventHandler<XmppPresenceEventArgs> Presence;
		public event EventHandler<XmppVCartEventArgs> VCard;
		public event EventHandler<XmppChatNotifyEventArgs> ChatNotify;
		public event EventHandler<XmppStateChangeEventArgs> StateChange;
		public event EventHandler<XmppSubscribeRequestEventArgs> SubscribeRequest;
		public event EventHandler<XmppIqSignalEventArgs> IqSignal;
		public event EventHandler<XmppErrorEventArgs> Error;

		private int iqCount = 0;
		public XmppClient(string hostName, int hostPort) {
			HostName = hostName;
			HostPort = hostPort;
			Jid = XmppJid.Empty;

			Disco = new XmppDisco(SoftwareNode, SoftwareVersion,
				new string[] {					
					"jabber:client", // RFC 6121: XMPP IM
					"jabber:iq:roster", // RFC 6121: XMPP IM
					//"jabber:iq:last", // XEP-0012: Last Activity
					//"jabber:iq:version", // XEP-0092: Software Version
					"http://jabber.org/protocol/disco#info", // XEP-0030: Service Discovery
					"http://jabber.org/protocol/chatstates", // XEP-0085: Chat State Notifications
					"http://jabber.org/protocol/caps", // XEP-0115: Entity Capabilities
					"vcard-temp", // XEP-0054: vcard-temp
					"vcard-temp:x:update", // XEP-0153: vCard-Based Avatars					
					"urn:xmpp:ping", // XEP-0199: XMPP Ping
					//"urn:xmpp:delay", // XEP-0203: Delayed Delivery
				});
			Caps = new XmppCaps(Disco);
		}

		public void Connect(string userName, string userPassword, string userResource, string serverName = null, string oAuthToken = null) {
			Jid = XmppJid.Empty;
			UserName = userName;
			UserPassword = userPassword;
			UserResource = userResource;
			UserOAuthToken = oAuthToken;
			ServerName = string.IsNullOrWhiteSpace(serverName) ? HostName.Substring(HostName.IndexOf('.') + 1) : serverName;

			if (engine == null) {
				engine = new Thread(new ThreadStart(StartEngine));
				engine.IsBackground = true;
				engine.SetApartmentState(ApartmentState.STA);
			}

			engine.Start();
		}
		public void Disconnect() {
			if (engine != null && engine.IsAlive)
				engine.Abort();

			if (state == XmppEngineState.Open || state == XmppEngineState.Opening) {
				if (tcpClient != null && tcpClient.IsConnected) {
					sendStream("</stream:stream>");
					tcpClient.Disconnect();
					tcpClient.Dispose();
				}
			}
			
			Jid = XmppJid.Empty;
			doChangeState(XmppEngineState.Closed);
			if (UserDisconnected != null)
				UserDisconnected(this, new XmppEventArgs());
		}
		public bool IsConnected {
			get {
				if (tcpClient == null || !tcpClient.IsConnected)
					return false;
				return state == XmppEngineState.Open;
			}
		}
		private void StartEngine() {
			while (true) {
				try {
					if (state == XmppEngineState.Initial || state == XmppEngineState.Closed)
						doAuthenticate();
					else {
						string buf = recvStream();
						if (!string.IsNullOrWhiteSpace(buf))
							doParseStream(buf);
					}
				}
				catch (System.IO.IOException) { return; }
				catch (ThreadAbortException) { return; }
			}
		}

		///////////////////////////////////////////////////////////////////////////////
		public int GetDiscoInfo(XmppJid to) {
			return SendIq(Jid, to, XmppIqType.Get, "<query xmlns='http://jabber.org/protocol/disco#info'/>");
		}
		///////////////////////////////////////////////////////////////////////////////
		public int GetRoster() {
			return SendIq(Jid, null, XmppIqType.Get, "<query xmlns='jabber:iq:roster'/>");
		}
		///////////////////////////////////////////////////////////////////////////////
		public void SetPresence(XmppPresenceStatus newStatus, string customMessage, string photoHash)
		{
			if (newStatus == XmppPresenceStatus.Unavailable)
				sendStream("<presence type=\"unavailable\"></presence>");
			else
			{
				StringBuilder stream = new StringBuilder();
				if (!string.IsNullOrEmpty(customMessage))
					customMessage = string.Format("<status>{0}</status>", customMessage);

				stream.Append("<presence><priority>1</priority>");
				stream.Append(Caps.ToStream());
				stream.Append(customMessage);
				stream.AppendFormat("<show>{0}</show>", XmppIq.PresenceToStr(newStatus));
				stream.AppendFormat("<x xmlns=\"vcard-temp:x:update\">");
				if (!string.IsNullOrWhiteSpace(photoHash))
					stream.AppendFormat("<photo>{0}</photo>", photoHash);
				stream.AppendFormat("</x>");

				stream.Append("</presence>");
				sendStream(stream.ToString());
			}

			statePresence = newStatus;
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendMessage(XmppMessage msg)
		{
			if (!string.IsNullOrEmpty(msg.Subject))
				msg.Subject = "<subject>" + msg.Subject.HtmlEncode() + "</subject>";
			//if (!string.IsNullOrEmpty(msg.Thread))
				//msg.Thread = "<thread>" + msg.Thread + "</thread>";
			msg.Thread = "";
			if (!string.IsNullOrEmpty(msg.Type))
				msg.Type = " type=\"" + msg.Type + "\"";
			
			string buf = string.Format("<message to=\"{0}\"{1} id=\"{2}\" from=\"{3}\">{4}<body>{5}</body>{6}<active xmlns=\"http://jabber.org/protocol/chatstates\"/></message>",
				msg.To.Bare, msg.Type, iqCount, Jid.Value, msg.Thread, msg.Body.HtmlEncode(), msg.Subject);
			sendStream(buf);
			return iqCount++;
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendNotification(XmppJid to, string thread, XmppChatStatus notify) {
			//if (!string.IsNullOrEmpty(thread))
				//thread = "<thread>" + thread + "</thread>";
			thread = "";

			string bufNotify = string.Empty;
			switch (notify) {
				case XmppChatStatus.NowComposing:
					bufNotify = "<composing xmlns=\"http://jabber.org/protocol/chatstates\"/>"; break;
				case XmppChatStatus.StopComposing:
					bufNotify = "<paused xmlns=\"http://jabber.org/protocol/chatstates\"/>"; break;
				case XmppChatStatus.Active:
					bufNotify = "<active xmlns=\"http://jabber.org/protocol/chatstates\"/>"; break;
				case XmppChatStatus.Inactive:
					bufNotify = "<inactive xmlns=\"http://jabber.org/protocol/chatstates\"/>"; break;
				case XmppChatStatus.Gone:
					bufNotify = "<gone xmlns=\"http://jabber.org/protocol/chatstates\"/>"; break;
			}

			string buf = string.Format("<message to=\"{0}\" type=\"chat\" id=\"{1}\" from=\"{2}\">{3}{4}</message>", to.Value, iqCount, Jid.Value, bufNotify, thread);
			sendStream(buf);
			return iqCount++;
		}
		///////////////////////////////////////////////////////////////////////////////
		public void SendSubscriptionRequest(string to) {
			sendStream(string.Format("<presence to=\"{0}\" type=\"subscribe\"/>", to));
		}
		///////////////////////////////////////////////////////////////////////////////
		public void ApproveSubscriptionRequest(string to) {
			sendStream(string.Format("<presence to=\"{0}\" type=\"subscribed\"/>", to));
		}
		///////////////////////////////////////////////////////////////////////////////
		public void RefuseSubscriptionRequest(string to) {
			sendStream(string.Format("<presence to=\"{0}\" type=\"unsubscribed\"/>", to));
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendDiscoInfoRequest() {
			string buf = string.Format("<iq from=\"{0}\" to=\"{1}\" id=\"{2}\" type=\"get\"><query xmlns=\"http://jabber.org/protocol/disco#info\"/></iq>", Jid.Value, ServerName, iqCount);
			sendStream(buf);
			return iqCount++;
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendDiscoInfoRequest(XmppJid to) {
			string buf = string.Format("<iq from=\"{0}\" to=\"{1}\" id=\"{2}\" type=\"get\"><query xmlns=\"http://jabber.org/protocol/disco#info\"/></iq>", Jid.Value, to.Value, iqCount);
			sendStream(buf);
			return iqCount++;
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendVCardRequest(XmppJid to) {
			XmppJid from = Jid;
			if (to.Bare == Jid.Bare)
				from = to = null;
			//to = new XmppJid(to.Bare, string.Empty);
			return SendIq(from, to, XmppIqType.Get, "<vCard xmlns=\"vcard-temp\"/>");
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendVCardUpdate(XmppVCard vCard) {
			string content = string.Format("<vCard xmlns=\"vcard-temp\">{0}</vCard>", vCard.ToStream());
			return SendIq(Jid, null, XmppIqType.Set, content);
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendPing() {
			string buf = string.Format("<iq from=\"{0}\" to=\"{1}\" id=\"{2}\" type=\"get\"><ping xmlns=\"urn:xmpp:ping\"/></iq>", Jid.Value, ServerName, iqCount);
			sendStream(buf);
			return iqCount++;
		}
		///////////////////////////////////////////////////////////////////////////////
		public int SendPing(XmppJid to) {
			string buf = string.Format("<iq from=\"{0}\" to=\"{1}\" id=\"{2}\" type=\"get\"><ping xmlns=\"urn:xmpp:ping\"/></iq>", Jid.Value, to.Value, iqCount);
			sendStream(buf);
			return iqCount++;
		}

		///////////////////////////////////////////////////////////////////////////////
		public void SendWhitespace() {
			sendStream(" ");
		}

		private int SendIq(XmppJid from, XmppJid to, XmppIqType type, string content) {
			sendStream(new XmppIq().ToStream(from, to, type, iqCount.ToString(), content));
			return iqCount++;
		}

		private void doParseStream(string stream)
		{
			XmppIQ xStream;
			try {
				xStream = XmppIQ.Parse(stream);
			}	catch (Exception) {
				//MessageBox.Show("Invalid stream :(\n\n" + stream);
				return;
			}
			
			switch (xStream.Name)
			{
				#region PRESENCE
				case "presence": {
						XmppPresence presence = new XmppPresence();
						string type = xStream.GetAttribute("type");
						presence.From = new XmppJid(xStream.GetAttribute("from"));
						presence.To = new XmppJid(xStream.GetAttribute("to"));

						// # type == ""
						if (string.IsNullOrEmpty(type)) {
							XmppIQ xStatus = xStream.FindDescendant("status");
							if (xStatus != null)
								presence.MessageStatus = xStatus.Text;

							XmppIQ xShow = xStream.FindDescendant("show");
							presence.Status = xShow != null ? XmppIq.StrToPresence(xShow.Text) : XmppPresenceStatus.Online;

							XmppIQ xCard = xStream.FindDescendant("x", "vcard-temp:x:update");
							if (xCard != null) {
								XmppIQ xPhoto = xCard.FindDescendant("photo", "vcard-temp:x:update");
								if (xPhoto != null)
									presence.PhotoHash = xPhoto.Text;
							}

							if (Presence != null)
								Presence(this, new XmppPresenceEventArgs(presence));
						}
							// # type == "unavailable"
						else if (type == "unavailable") {
							presence.Status = XmppPresenceStatus.Unavailable;
							if (Presence != null)
								Presence(this, new XmppPresenceEventArgs(presence));
						}
							// # type == "probe"	// probe request from server 
						else if (type == "probe") {
							SetPresence(statePresence, string.Empty, null);
						}
							// # type == "subscribe"	// new subscription request
						else if (type == "subscribe") {
							if (SubscribeRequest != null)
								SubscribeRequest(this, new XmppSubscribeRequestEventArgs(presence.From));
						}
							// # type == "error"	// presence stanza error
						else if (type == "error") {
							// @@@
						}
					}
					break;
				#endregion
				#region MESSAGE
				case "message": {
						XmppMessage message = new XmppMessage();
						message.Type = xStream.GetAttribute("type");
						message.From = new XmppJid(xStream.GetAttribute("from"));
						message.To = new XmppJid(xStream.GetAttribute("to"));
						Int32.TryParse(xStream.GetAttribute("id"), out message.ID);

						if (message.Type != "error") {
							// # user composing new message
							if (xStream.FindDescendant("composing", "http://jabber.org/protocol/chatstates") != null) {
								if (ChatNotify != null)
									ChatNotify(this, new XmppChatNotifyEventArgs(message.From, string.Empty, XmppChatStatus.NowComposing));
							}
								// # user stop composing
							else if (xStream.FindDescendant("paused", "http://jabber.org/protocol/chatstates") != null) {
								if (ChatNotify != null)
									ChatNotify(this, new XmppChatNotifyEventArgs(message.From, string.Empty, XmppChatStatus.StopComposing));
							}
								// # user is inactive
							else if (xStream.FindDescendant("inactive", "http://jabber.org/protocol/chatstates") != null) {
								if (ChatNotify != null)
									ChatNotify(this, new XmppChatNotifyEventArgs(message.From, string.Empty, XmppChatStatus.Inactive));
							}
								// # user has left conversation
							else if (xStream.FindDescendant("gone", "http://jabber.org/protocol/chatstates") != null) {
								if (ChatNotify != null)
									ChatNotify(this, new XmppChatNotifyEventArgs(message.From, string.Empty, XmppChatStatus.Gone));
							}
								// # user is active ;)
							else if (xStream.FindDescendant("active", "http://jabber.org/protocol/chatstates") != null) {
								if (ChatNotify != null)
									ChatNotify(this, new XmppChatNotifyEventArgs(message.From, string.Empty, XmppChatStatus.Active));
							}

							// # check for new message
							XmppIQ xBody = xStream.FindDescendant("body");
							if (xBody != null) {
								message.Body = xBody.Text;

								XmppIQ xThread = xStream.FindDescendant("thread");
								if (xThread != null)
									message.Thread = xThread.Text;

								XmppIQ xSubject = xStream.FindDescendant("subject");
								if (xSubject != null)
									message.Subject = xSubject.Text;

								if (Message != null)
									Message(this, new XmppMessageEventArgs(message));
							}
						}
					}
					break;
				#endregion
				#region IQ
				case "iq": {
					XmppIQ xQuery = null;
					string type = xStream.GetAttribute("type");

					// Roster
					xQuery = xStream.FindDescendant("query", "jabber:iq:roster");
					if (xQuery != null) {
						List<XmppRosterItem> roster = new List<XmppRosterItem>();
						foreach (XmppIQ xItem in xQuery.Children()) {
							if (xItem.Name != "item")
								continue;

							string jid = xItem.GetAttribute("jid");
							string sub = xItem.GetAttribute("subscription");
							string ask = xItem.GetAttribute("ask");
							string name = xItem.GetAttribute("name");
							string group = xItem.GetAttribute("group");							
							roster.Add(new XmppRosterItem(jid, sub, name, group, ask));
						}

						if (Roster != null)
							Roster(this, new XmppRosterEventArgs(roster, XmppIq.StrToType(type)));
					}

					// Disco
					xQuery = xStream.FindDescendant("query", "http://jabber.org/protocol/disco#info");
					if (xQuery != null) {
						if (type == "get") {
							XmppJid from = new XmppJid(xStream.GetAttribute("from"));
							string id = xStream.GetAttribute("id");
							sendStream(new XmppIq().ToStream(Jid, from, XmppIqType.Result, id, Disco.ToStreamResult()));// @@@@@ ottimizza SendIq(...)
							//SendIq(from, XmppIqType.Result, Disco.ToStreamResult());// @@@@@ ottimizza SendIq(...)
						}
					}

					// vCard
					xQuery = xStream.FindDescendant("vCard", "vcard-temp");
					if (xQuery != null) {
						if (type == "result") {
							XmppJid from = new XmppJid(xStream.GetAttribute("from"));
							XmppVCard vCard = new XmppVCard(from, xQuery);
							if (VCard != null)
								VCard(this, new XmppVCartEventArgs(vCard));
						}
					}

					// Ping
					xQuery = xStream.FindDescendant("ping", "urn:xmpp:ping");
					if (xQuery != null) {
						if (type == "get") {
							XmppJid from = new XmppJid(xStream.GetAttribute("from"));
							string id = xStream.GetAttribute("id");
							sendStream(new XmppIq().ToStream(Jid, from, XmppIqType.Result, id, string.Empty));// @@@@@ ottimizza SendIq(...)
						}
					}
				}

				break;
				#endregion
			}

			Signal(xStream);
		}

		private void Signal(XmppIQ xStream) {
			if (IqSignal != null) {
				string typeStr = xStream.GetAttribute("type");
				string idStr = xStream.GetAttribute("id");

				int id = -1;
				if (Int32.TryParse(idStr, out id) && id >= 0)
					IqSignal(this, new XmppIqSignalEventArgs(id, XmppIq.StrToType(typeStr), xStream));
			}
		}
		private void doAuthenticate() {
			doChangeState(XmppEngineState.Opening);
			if (tcpClient != null) {
				tcpClient.Dispose();
				tcpClient = null;
			}

			try {
				tcpClient = new XmppTcpSocket(HostName, HostPort);
			}
			catch (Exception ex) {
				if (Error != null)
					Error(this, new XmppErrorEventArgs(XmppErrorType.SocketError, string.Empty, ex.GetType().ToString() + ": " + ex.Message));
				if (tcpClient != null) {
					tcpClient.Dispose();
					tcpClient = null;
				}

				return;
			}

			string stream;
			XmppIQ xStream;
			// ==========================================================================
			// === START TLS LAYER ======================================================
			stream = doRequestFeatures();
			if (stream.Contains("<starttls")) {
				sendStream("<starttls xmlns=\"urn:ietf:params:xml:ns:xmpp-tls\"/>");
				stream = recvStream();
				if (stream.Contains("<proceed")) {
					bool sslStarted = false;
					string message = string.Empty;
					try {
						sslStarted = tcpClient.StartSSL();
						sslStarted = true;
					}
					catch (Exception ex) {
						sslStarted = false;
						message = string.Format("{0}: {1}", ex.GetType(), ex.Message);
					}
					 
					if (!sslStarted) {
						if (Error != null)
							Error(this, new XmppErrorEventArgs(XmppErrorType.SocketStartSSLError, string.Empty, message));

						return;
					}

					stream = doRequestFeatures();
				}
			}

			if (stream.Contains("<mechanism>DIGEST-MD5</mechanism>")) {
				// ==========================================================================
				// === SASL DIGEST-MD5 AUTHENTICATION PROCESS ===============================
				DigestMD5MechanismProvider mech = new DigestMD5MechanismProvider(UserName, UserPassword);
				sendStream(mech.GetAuthStream());

				stream = recvStream();
				xStream = XmppIQ.Parse(stream);
				if (!xStream.Equal("challenge", "urn:ietf:params:xml:ns:xmpp-sasl")) {
					if (Error != null) {
						string tag, message;
						XmppError.Parse(XmppIQ.Parse(stream), out tag, out message);
						Error(this, new XmppErrorEventArgs(XmppErrorType.AuthUserNotAuthorized, tag, message));
					}

					return;
				}

				sendStream(mech.GetChallengeResponse(xStream.Text));
				stream = recvStream();
				xStream = XmppIQ.Parse(stream);
				if (!xStream.Equal("challenge", "urn:ietf:params:xml:ns:xmpp-sasl")) {
					if (Error != null) {
						string tag, message;
						XmppError.Parse(XmppIQ.Parse(stream), out tag, out message);
						Error(this, new XmppErrorEventArgs(XmppErrorType.AuthUserNotAuthorized, tag, message));
					}

					return;
				}

				sendStream("<response xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\"/>");
			} else if (stream.Contains("<mechanism>PLAIN</mechanism>")) {
				// ==========================================================================
				// === SASL PLAIN AUTHENTICATION PROCESS ====================================
				PlainMechanismProvider mech = new PlainMechanismProvider(UserName, UserPassword);
				sendStream(mech.GetAuthStream());
			} else if (stream.Contains("<mechanism>X-GOOGLE-TOKEN</mechanism>")) {
				// ==========================================================================
				// === SASL X-GOOGLE-TOKEN AUTHENTICATION PROCESS ====================================
				XGoogleTokenMechanismProvider mech = new XGoogleTokenMechanismProvider(UserName, UserPassword, UserResource);
				sendStream(mech.GetAuthStream());
			} else if (stream.Contains("<mechanism>X-MESSENGER-OAUTH2</mechanism>")) {
				// ==========================================================================
				// === SASL X-MESSENGER-OAUTH2 AUTHENTICATION PROCESS ====================================
				XMessengerOAuth2MechanismProvider mech = new XMessengerOAuth2MechanismProvider(UserName, UserOAuthToken);
				sendStream(mech.GetAuthStream());
			} else if (stream.Contains("<mechanism>X-FACEBOOK-PLATFORM</mechanism>")) {
				// ==========================================================================
				// === SASL X-FACEBOOK-PLATFORM AUTHENTICATION PROCESS ====================================
				XFacebookPlatformMechanismProvider mech = new XFacebookPlatformMechanismProvider(UserName, UserOAuthToken);
				sendStream(mech.GetAuthStream());

				stream = recvStream();
				xStream = XmppIQ.Parse(stream);
				if (!xStream.Equal("challenge", "urn:ietf:params:xml:ns:xmpp-sasl")) {
					if (Error != null) {
						string tag, message;
						XmppError.Parse(XmppIQ.Parse(stream), out tag, out message);
						Error(this, new XmppErrorEventArgs(XmppErrorType.AuthUserNotAuthorized, tag, message));
					}

					return;
				}

				sendStream(mech.GetChallengeResponse(xStream.Text));
			} else {
				if (Error != null)
					Error(this, new XmppErrorEventArgs(XmppErrorType.AuthModeNotSupported));

				return;
			}

			stream = recvStream();
			if (stream.Contains("<failure")) {
				if (Error != null) {
					string tag, message;
					XmppError.Parse(XmppIQ.Parse(stream), out tag, out message);
					Error(this, new XmppErrorEventArgs(XmppErrorType.AuthUserNotAuthorized, tag, message));
				}

				return;
			}

			// ==========================================================================
			// === BIND RESOURCE ========================================================
			stream = doRequestFeatures();
			SendIq(Jid, null, XmppIqType.Set, "<bind xmlns=\"urn:ietf:params:xml:ns:xmpp-bind\"><resource>" + UserResource + "</resource></bind>");
			stream = recvStream();
			xStream = new XmppIQ(stream);
			XmppIQ xJid = xStream.FindDescendant("jid", "urn:ietf:params:xml:ns:xmpp-bind");
			if (xJid != null)
				Jid = new XmppJid(xJid.Text);

			// ==========================================================================
			// === START SESSION ========================================================
			SendIq(Jid, null, XmppIqType.Set, "<session xmlns=\"urn:ietf:params:xml:ns:xmpp-session\"/>");
			stream = recvStream();

			// ==========================================================================
			// === OK: AUTHENTICATED IN JABBER NETWORK, You rocks! ======================
			//tcpClient.SetReadTimeout(0);
			doChangeState(XmppEngineState.Open);

			if (UserConnected != null)
				UserConnected(this, new XmppEventArgs(Jid.Value));
		}
		private void doChangeState(XmppEngineState newState) {
			XmppEngineState oldState = state;
			state = newState;
			if (StateChange != null)
				StateChange(this, new XmppStateChangeEventArgs(oldState, newState));
		}
		private string doRequestFeatures() {
			sendStream(string.Format("<stream:stream to=\"{0}\" xmlns=\"jabber:client\" xmlns:stream=\"http://etherx.jabber.org/streams\" version=\"1.0\">", ServerName));
			string stream = recvStream();

			if (!stream.Contains("<stream:stream "))
				return string.Empty;

			int iFeatures = stream.IndexOf("<stream:features");
			if (iFeatures != -1)
				return stream.Substring(iFeatures);

			stream = recvStream();
			if (stream.Contains("<stream:features"))
				return stream;

			return string.Empty;
		}

		private void sendStream(string msg) {
			if (tcpClient == null)
				return;

			if (SendStreamCallback != null)
				SendStreamCallback(this, new XmppEventArgs(msg));

			tcpClient.WriteStream(msg);
		}
		private string recvStream()
		{
			if (tcpClient == null)
			{
				if (Error != null)
					Error(this, new XmppErrorEventArgs(XmppErrorType.ClientNotConnected, string.Empty, string.Empty));
				return string.Empty;
			}

			int iteration = 0;
			StringBuilder buf = new StringBuilder();
			while (true)
			{
				string internalBuf = tcpClient.ReadStream();
				if (string.IsNullOrWhiteSpace(internalBuf))
					break;

				if (internalBuf.Contains("<stream:error"))
				{
					if (RecvStreamCallback != null)
						RecvStreamCallback(this, new XmppEventArgs(buf.Append(internalBuf).ToString()));
					if (Error != null) {
						string tag, message;
						XmppError.Parse(XmppIQ.Parse(internalBuf), out tag, out message);
						Error(this, new XmppErrorEventArgs(XmppErrorType.XmppStreamError, tag, message));
					}
					Disconnect();
					break;
				}
				else if (internalBuf.Contains("</stream:stream>"))
				{
					if (RecvStreamCallback != null)
						RecvStreamCallback(this, new XmppEventArgs(buf.Append(internalBuf).ToString()));
					if (Error != null)
						Error(this, new XmppErrorEventArgs(XmppErrorType.ConnectionClosedByServer, string.Empty, string.Empty));

					Disconnect();
					break;
				}

				buf.Append(internalBuf);
				if (state == XmppEngineState.Open)
				{
					if (XmppStream.IsValid(buf.ToString()))
						break;
				}
				else
					break;

				iteration++;
				if (iteration > 100)
				{
					if (Error != null)
						Error(this, new XmppErrorEventArgs(XmppErrorType.RecvTooManyInvalidStream, string.Empty, ""));

					Disconnect();
					return string.Empty;
				}
			}

			if (RecvStreamCallback != null)
				RecvStreamCallback(this, new XmppEventArgs(buf.ToString()));

			// flush string buffer 
			string stringBuffer = buf.ToString();
			buf.EnsureCapacity(0);

			// it's ok, return a valid stream
			return stringBuffer;
		}




		~XmppClient() { Dispose(); }
		public void Dispose() {
			if (engine != null && engine.IsAlive) {
				engine.Abort();
				engine = null;
			}
			if (tcpClient != null) {
				tcpClient.Dispose();
			}
		}		
	}
}


