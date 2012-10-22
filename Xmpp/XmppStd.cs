using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Base.Xmpp.Utils;
using System.Xml.Linq;

namespace Base.Xmpp.Core {
	public enum XmppPresenceStatus {
		Online,
		Busy,
		Away,
		Unavailable
	}
	public enum XmppChatStatus {
		NowComposing,     // User is interacting with a message input interface specific to this chat session.
		StopComposing,    // User was composing but has not interacted with the message input interface for a short period of time (e.g., 5 seconds).
		Active,           // User accepts an initial content message, sends a content message, gives focus to the chat session interface, or is otherwise paying attention to the conversation.
		Inactive,         // User has not interacted with the chat session interface for an intermediate period of time (e.g., 30 seconds).
		Gone              // User has not interacted with the chat session interface, system, or device for a relatively long period of time (e.g., 2 minutes), or has terminated the chat session interface (e.g., by closing the chat window).
	}
	public enum XmppIqType {
		Get, Set, Result, Error, Undefined
	}
	public class XmppIq {
		public static string TypeToStr(XmppIqType type) {
			switch (type) {
				case XmppIqType.Get: return "get";
				case XmppIqType.Set: return "set";
				case XmppIqType.Result: return "result";
				case XmppIqType.Error: return "error";
				default: return string.Empty;
			}
		}
		public static XmppIqType StrToType(string type) {
			switch (type.ToLower()) {
				case "get": return XmppIqType.Get;
				case "set": return XmppIqType.Set;
				case "result": return XmppIqType.Result;
				case "error": return XmppIqType.Error;
				default: return XmppIqType.Undefined;
			}
		}
		public static string PresenceToStr(XmppPresenceStatus prs) {
			switch (prs) {
				case XmppPresenceStatus.Busy:
					return "dnd";
				case XmppPresenceStatus.Away:
					return "away";
				case XmppPresenceStatus.Online:
				case XmppPresenceStatus.Unavailable:
				default:
					return string.Empty;
			}
		}
		public static XmppPresenceStatus StrToPresence(string str) {
			switch (str) {
				case "dnd": return XmppPresenceStatus.Busy;
				case "away":
				case "xa":
					return XmppPresenceStatus.Away;
				case "":
				case "chat":
				default:
					return XmppPresenceStatus.Online;
			}
		}

		public XmppIq() {	}
		public string ToStream(XmppJid from, XmppJid to, XmppIqType type, string id, string content) {
			StringBuilder result = new StringBuilder("<iq ");
			if (from != null && !string.IsNullOrWhiteSpace(from.Value))
				result.AppendFormat("from=\"{0}\" ", from.Value);
			if (to != null && !string.IsNullOrWhiteSpace(to.Value))
				result.AppendFormat("to=\"{0}\" ", to.Value);
			result.AppendFormat("id=\"{0}\" ", id);
			result.AppendFormat("type=\"{0}\">{1}</iq>", XmppIq.TypeToStr(type), content);
			return result.ToString();
		}
	}
	public class XmppRosterItem {
		public XmppJid Jid;
		public string Subscription = string.Empty; //@@@enum
		public string Name = string.Empty;
		public string Group = string.Empty;
		public string Ask = string.Empty;
		public XmppRosterItem(string jid, string sub, string name, string group, string ask) {
			Jid = new XmppJid(jid); Subscription = sub; Name = name; Group = group; Ask = ask;
		}
	}
	public class XmppPresence {
		public static XmppPresence Empty { get { return new XmppPresence(); } }
		public XmppJid From = XmppJid.Empty;
		public XmppJid To = XmppJid.Empty;
		public XmppPresenceStatus Status = XmppPresenceStatus.Unavailable;
		public string MessageStatus = string.Empty;
		public string PhotoHash = string.Empty;
		public XmppPresence() { }
	}
	public class XmppMessage {
		public XmppJid From = XmppJid.Empty;
		public XmppJid To = XmppJid.Empty;
		public string Type = string.Empty;
		public string Thread = string.Empty;
		public string Subject = string.Empty;
		public string Body = string.Empty;
		public int ID = -1;
		public XmppMessage() { }
	}
	public class XmppCaps {
		private readonly string Node;
		private const string hash = "sha-1";
		private string ver = string.Empty;
		public XmppCaps(XmppDisco disco) {
			this.Node = disco.Node;
			this.ver = disco.Ver;
		}

		public string ToStream() {
			StringBuilder result = new StringBuilder();
			result.AppendFormat("<c xmlns='http://jabber.org/protocol/caps' node='{0}' ", Node);
			if (!string.IsNullOrWhiteSpace(ver)){
				result.AppendFormat("hash='{0}' ", hash);
				result.AppendFormat("ver='{0}' ", ver);
			}

			result.Append("/>");
			return result.ToString();
		}
	}
	public class XmppDisco {
		public readonly string IdentityCategory = "client";
		public readonly string IdentityType = "pc";
		public readonly string IdentityName;
		public readonly string Ver;
		public readonly string Node;
		public IEnumerable<string> FeaturesVar = null;
		public XmppDisco(string node, string identityName, IEnumerable<string> featuresVar) {
			this.Node = node;
			this.IdentityName = identityName;
			this.FeaturesVar = featuresVar;
			this.Ver = VerificationString();
		}
		private string VerificationString() {
			StringBuilder result = new StringBuilder();
			result.AppendFormat("{0}/{1}//{2}<", this.IdentityCategory, this.IdentityType, this.IdentityName);
			foreach (string feature in this.FeaturesVar.OrderBy(f => f))
				result.AppendFormat("{0}<", feature);

			return CryptoUtils.SHA1HashB64(result.ToString());
		}
		public string ToStreamResult() {
			StringBuilder result = new StringBuilder();
			result.AppendFormat("<query xmlns='http://jabber.org/protocol/disco#info' node='{0}#{1}'>", Node, Ver);
			result.AppendFormat("<identity category='client' type='pc'/>");
			foreach (string feature in this.FeaturesVar)
				result.AppendFormat("<feature var='{0}'/>", feature);

			result.Append("</query>");
			return result.ToString();
		}
		public string ToStreamGet() {
			StringBuilder result = new StringBuilder();
			result.AppendFormat("<query xmlns='http://jabber.org/protocol/disco#info'/>");
			return result.ToString();
		}

	}
	public class XmppJid {
		public static XmppJid Empty { get { return new XmppJid(string.Empty, string.Empty); } }
		public string Value {
			get {
				if (string.IsNullOrWhiteSpace(Resource))
					return Bare;
				return Bare + "/" + Resource;
			}
		}
		public string Bare { get; private set; }
		public string Resource { get; private set; }
		public XmppJid(string jid) {
			int pSlash = jid.IndexOf('/');
			if (pSlash < 0)
				Bare = jid;
			else {
				Bare = jid.Substring(0, pSlash);
				Resource = jid.Substring(pSlash + 1);
			}
		}
		public XmppJid(string user, string resource) {
			Bare = user;
			Resource = resource;
		}
		public override bool Equals(object obj) {
			if (obj == null || (obj is XmppJid))
				return false;

			XmppJid cJid = obj as XmppJid;
			return (this.Bare == cJid.Bare && this.Resource == cJid.Resource);
		}
		public override string ToString() {
			return Value;
		}
		public override int GetHashCode() {
			return base.GetHashCode();
		}
	}
	public class XmppVCard {
		private const string ns = "vcard-temp";
		public string PhotoType { get; set; }
		public string PhotoBinVal { get; set; }
		public string FN { get; set; }
		public XmppJid Jid { get; private set; }
		public XmppVCard(XmppJid jid, XmppIQ stream) {
			Jid = jid;

			XmppIQ xFN = stream.FindDescendant("FN", ns);
			if (xFN != null)
				FN = xFN.Text;

			XmppIQ xPhoto = stream.FindDescendant("PHOTO", ns);
			if (xPhoto != null) {
				XmppIQ xPhotoType = xPhoto.FindChild("TYPE", ns);
				if (xPhotoType != null)
					PhotoType = xPhotoType.Text;
				XmppIQ xPhotoBin = xPhoto.FindChild("BINVAL", ns);
				if (xPhotoBin != null)
					PhotoBinVal = xPhotoBin.Text;
			}
		}

		public string ToStream() {
			StringBuilder result = new StringBuilder();
			result.AppendFormat("<FN>{0}</FN>", FN);
			result.AppendFormat("<PHOTO>");
			result.AppendFormat("<TYPE>{0}</TYPE>", PhotoType);
			result.AppendFormat("<BINVAL>{0}</BINVAL>", PhotoBinVal);
			result.AppendFormat("</PHOTO>");

			return result.ToString();
		}
	}


	public class XmppEventArgs : EventArgs {
		public string Message { get; private set; }
		public XmppEventArgs(string message) {
			Message = message;
		}
		public XmppEventArgs() {
			Message = string.Empty;
		}
	}
	public class XmppErrorEventArgs : EventArgs {
		public XmppErrorType Type { get; private set; }
		public string ErrorDescriptor { get; private set; }
		public string Message { get; private set; }
		public XmppErrorEventArgs(XmppErrorType type, string errorDescriptor = null, string message = null) {
			Type = type;
			ErrorDescriptor = string.IsNullOrWhiteSpace(errorDescriptor) ? string.Empty : errorDescriptor;
			Message = string.IsNullOrWhiteSpace(message) ? string.Empty : message;
		}
	}
	public class XmppStateChangeEventArgs : EventArgs {
		public XmppEngineState OldState { get; private set; }
		public XmppEngineState NewState { get; private set; }
		public XmppStateChangeEventArgs(XmppEngineState oldState, XmppEngineState newState) {
			OldState = oldState;
			NewState = newState;
		}
	}
	public class XmppRosterEventArgs : EventArgs {
		public List<XmppRosterItem> Roster { get; private set; }
		public XmppIqType Type { get; private set; }
		public XmppRosterEventArgs(List<XmppRosterItem> roster, XmppIqType type) {
			Roster = roster;
			Type = type;
		}
	}
	public class XmppPresenceEventArgs : EventArgs {
		public XmppPresence Presence { get; private set; }
		public XmppPresenceEventArgs(XmppPresence presence) {
			Presence = presence;
		}
	}
	public class XmppVCartEventArgs : EventArgs {
		public XmppVCard VCard { get; private set; }
		public XmppVCartEventArgs(XmppVCard vcard) {
			VCard = vcard;
		}
	}
	public class XmppMessageEventArgs : EventArgs {
		public XmppMessage Message { get; private set; }
		public XmppMessageEventArgs(XmppMessage message) {
			Message = message;
		}
	}
	public class XmppChatNotifyEventArgs : EventArgs {
		public string Thread { get; private set; }
		public XmppJid From { get; private set; }
		public XmppChatStatus ChatStatus { get; private set; }
		public XmppChatNotifyEventArgs(XmppJid jid, string thread, XmppChatStatus chatStatus) {
			From = jid;
			Thread = thread;
			ChatStatus = chatStatus;
		}
	}
	public class XmppSubscribeRequestEventArgs : EventArgs {
		public XmppJid From { get; private set; }
		public XmppSubscribeRequestEventArgs(XmppJid jid) {
			From = jid;
		}
	}
	public class XmppIqSignalEventArgs : EventArgs {
		public int ID { get; private set; }
		public XmppIqType Type { get; private set; }
		public XmppIQ xStream { get; private set; }
		public XmppIqSignalEventArgs(int id, XmppIqType type, XmppIQ xStream) {
			this.ID = id;
			this.Type = type;
			this.xStream = xStream;
		}
	}

	public class NS {
		/// <summary>
		/// Namespace for the prefix "xmlns".
		/// </summary>
		public const string XMLNS = "http://www.w3.org/2000/xmlns/";
		/// <summary>
		/// Namespace for the prefix "xml", like xml:lang.
		/// </summary>
		public const string XML = "http://www.w3.org/XML/1998/namespace";
		/// <summary>
		/// XHTML namespace, for &lt;body&gt; element
		/// </summary>
		public const string XHTML = "http://www.w3.org/1999/xhtml";
		/// <summary>
		/// XHTML-IM namespace, for &lt;html&gt; element
		/// </summary>
		public const string XHTML_IM = "http://jabber.org/protocol/xhtml-im";
		/// <summary>
		/// stream:stream
		/// </summary>
		public const string STREAM = "http://etherx.jabber.org/streams";
		/// <summary>
		/// Start-TLS feature namespace
		/// </summary>
		public const string XMPP_TLS = "urn:ietf:params:xml:ns:xmpp-tls";
		/// <summary>
		/// XEP-138 compression feature namespace.  Not the same as for the protocol!
		/// </summary>
		public const string FEATURE_COMPRESS = "http://jabber.org/features/compress";
		/// <summary>
		/// XEP-138 compression protocol namespace.  Not the same as the feature!
		/// </summary>
		public const string PROTOCOL_COMPRESS = "http://jabber.org/protocol/compress";
		/// <summary>
		/// SASL feature namespace
		/// </summary>
		public const string XMPP_SASL = "urn:ietf:params:xml:ns:xmpp-sasl";
		/// <summary>
		/// Start a session
		/// </summary>
		public const string XMPP_SESSION = "urn:ietf:params:xml:ns:xmpp-session";
		/// <summary>
		/// Bind a resource
		/// </summary>
		public const string XMPP_BIND = "urn:ietf:params:xml:ns:xmpp-bind";
		/// <summary>
		/// Stanza errors.  See RFC 3920, section 9.3.
		/// </summary>
		public const string XMPP_STANZAS = "urn:ietf:params:xml:ns:xmpp-stanzas";
		/// <summary>
		/// Stream errors.  See RFC 3920, section 4.7.
		/// </summary>
		public const string XMPP_STREAMS = "urn:ietf:params:xml:ns:xmpp-streams";
		/// <summary>
		/// Jabber client connections
		/// </summary>
		public const string CLIENT = "jabber:client";
		/// <summary>
		/// Jabber HTTP Binding connections
		/// </summary>
		public const string HTTP_BIND = "http://jabber.org/protocol/httpbind";
		/// <summary>
		/// Jabber component connections
		/// </summary>
		public const string COMPONENT_ACCEPT = "jabber:component:accept";
		/// <summary>
		/// Jabber component connections, from the router
		/// </summary>
		public const string COMPONENT_CONNECT = "jabber:component:connect";
		/// <summary>
		/// S2S connection
		/// </summary>
		public const string SERVER = "jabber:server";
		/// <summary>
		/// S2S dialback
		/// </summary>
		public const string SERVER_DIALBACK = "jabber:server:dialback";
		// IQ
		/// <summary>
		/// Authentication
		/// </summary>
		public const string IQ_AUTH = "jabber:iq:auth";
		/// <summary>
		/// Roster manipulation
		/// </summary>
		public const string IQ_ROSTER = "jabber:iq:roster";
		/// <summary>
		/// Register users
		/// </summary>
		public const string IQ_REGISTER = "jabber:iq:register";
		/// <summary>
		/// Out-of-band (file transfer)
		/// </summary>
		public const string OOB = "jabber:iq:oob";
		/// <summary>
		/// Server agents
		/// </summary>
		public const string IQ_AGENTS = "jabber:iq:agents";
		/// <summary>
		/// Client or server current time
		/// </summary>
		public const string IQ_TIME = "jabber:iq:time";
		/// <summary>
		/// Last activity
		/// </summary>
		public const string IQ_LAST = "jabber:iq:last";
		/// <summary>
		/// Client or server version
		/// </summary>
		public const string IQ_VERSION = "jabber:iq:version";
		/// <summary>
		/// Jabber Browsing
		/// </summary>
		public const string IQ_BROWSE = "jabber:iq:browse";
		/// <summary>
		/// Profile information
		/// </summary>
		public const string VCARD_TEMP = "vcard-temp";

		/// <summary>
		/// Geographic locaiotn (lat/long).
		/// See XEP-80 (http://www.xmpp.org/extensions/xep-0080.html)
		/// </summary>
		public const string PROTOCOL_GEOLOC = "http://jabber.org/protocol/geoloc";

		/// <summary>
		/// Discover items from an entity.
		/// </summary>
		public const string PROTOCOL_DISCO_ITEMS = "http://jabber.org/protocol/disco#items";
		/// <summary>
		/// Discover info about an entity item.
		/// </summary>
		public const string PROTOCOL_DISCO_INFO = "http://jabber.org/protocol/disco#info";

		// X
		/// <summary>
		/// Offline message timestamping.
		/// </summary>
		public const string X_DELAY = "jabber:x:delay";
		/// <summary>
		/// Modern, XEP-0203 delay.
		/// </summary>
		public const string XMPP_DELAY = "urn:xmpp:delay";
		/// <summary>
		/// Out-of-band (file transfer)
		/// </summary>
		public const string X_OOB = "jabber:x:oob";
		/// <summary>
		/// Send roster entries to another user.
		/// </summary>
		public const string X_ROSTER = "jabber:x:roster";
		/// <summary>
		/// The jabber:x:event namespace qualifies extensions used to request and respond to
		/// events relating to the delivery, display, and composition of messages.
		/// </summary>
		public const string X_EVENT = "jabber:x:event";
		/// <summary>
		/// jabber:x:data, as described in XEP-0004.
		/// </summary>
		public const string X_DATA = "jabber:x:data";

		/// <summary>
		/// jabber:iq:search.
		/// See XEP-55 (http://www.xmpp.org/extensions/xep-0055.html)
		/// </summary>
		public const string IQ_SEARCH = "jabber:iq:search";

		/// <summary>
		/// Multi-user chat.
		/// See XEP-45 (http://www.xmpp.org/extensions/xep-0045.html)
		/// </summary>
		public const string PROTOCOL_MUC = "http://jabber.org/protocol/muc";
		/// <summary>
		/// Multi-user chat user functions.
		/// See XEP-45 (http://www.xmpp.org/extensions/xep-0045.html)
		/// </summary>
		public const string PROTOCOL_MUC_USER = "http://jabber.org/protocol/muc#user";
		/// <summary>
		/// Multi-user chat admin functions.
		/// See XEP-45 (http://www.xmpp.org/extensions/xep-0045.html)
		/// </summary>
		public const string PROTOCOL_MUC_ADMIN = "http://jabber.org/protocol/muc#admin";
		/// <summary>
		/// Multi-user chat owner functions.
		/// See XEP-45 (http://www.xmpp.org/extensions/xep-0045.html)
		/// </summary>
		public const string PROTOCOL_MUC_OWNER = "http://jabber.org/protocol/muc#owner";
		/// <summary>
		/// Multi-user chat; request a unique room name.
		/// See XEP-45 (http://www.xmpp.org/extensions/xep-0045.html)
		/// </summary>
		public const string PROTOCOL_MUC_UNIQUE = "http://jabber.org/protocol/muc#unique";

		/// <summary>
		/// Entity Capabilities.
		/// See XEP-115 (http://www.xmpp.org/extensions/xep-0115.html)
		/// </summary>
		public const string PROTOCOL_CAPS = "http://jabber.org/protocol/caps";

		/// <summary>
		/// Publish/Subscribe
		/// See XEP-0060 (http://www.xmpp.org/extensions/xep-0060.html)
		/// </summary>
		public const string PROTOCOL_PUBSUB = "http://jabber.org/protocol/pubsub";

		/// <summary>
		/// Publish/Subscribe, Owner use cases
		/// See XEP-0060 (http://www.xmpp.org/extensions/xep-0060.html)
		/// </summary>
		public const string PROTOCOL_PUBSUB_OWNER = "http://jabber.org/protocol/pubsub#owner";

		/// <summary>
		/// Pub/Sub node configuration.
		/// See XEP-0060 (http://www.xmpp.org/extensions/xep-0060.html)
		/// </summary>
		public const string PROTOCOL_PUBSUB_NODE_CONFIG = "http://jabber.org/protocol/pubsub#node_config";

		/// <summary>
		/// Publish/Subscribe Event
		/// See XEP-0060 (http://www.xmpp.org/extensions/xep-0060.html)
		/// </summary>
		public const string PROTOCOL_PUBSUB_EVENT = "http://jabber.org/protocol/pubsub#event";

		/// <summary>
		/// Publish/Subscribe Errors
		/// See XEP-0060 (http://www.xmpp.org/extensions/xep-0060.html)
		/// </summary>
		public const string PROTOCOL_PUBSUB_ERRORS = "http://jabber.org/protocol/pubsub#errors";

	}


	public class XmppError {
		public static void Parse(XmppIQ xStream, out string tag, out string message) {
			tag = string.Empty;
			message = string.Empty;

			// sasl auth error
			if (xStream.Equal("failure", "urn:ietf:params:xml:ns:xmpp-sasl")) {
				XmppIQ xTag = xStream.Children().FirstOrDefault();
				if (xTag != null) {
					tag = xTag.Name;
					message = xTag.Text;
					return;
				}
			}

			// stream error
			/* from messenger
			<stream:error xmlns:stream="http://etherx.jabber.org/streams">
				<not-well-formed xmlns="urn:ietf:params:xml:ns:xmpp-streams" />
				<text xmlns="urn:ietf:params:xml:ns:xmpp-streams">XmlException due to: Unexpected end of file has occurred. The following elements are not closed: BINVAL, PHOTO, vCard, iq. Line 1, position 16344.</text>
			</stream:error>
			*/
			if (xStream.Equal("error", "http://etherx.jabber.org/streams")) {
				XmppIQ xTag = xStream.Children().FirstOrDefault();
				if (xTag != null)
					tag = xTag.Name;
				XmppIQ xText = xStream.FindDescendant("text", "urn:ietf:params:xml:ns:xmpp-streams");
				if (xText != null)
					message = xText.Text;
				return;
			}

			// stanza error
			XmppIQ xError = xStream.FindDescendant("error");
			if (xError != null) {
				XmppIQ xTag = xError.Children().FirstOrDefault();
				if (xTag != null)
					tag = xTag.Name;
				XmppIQ xText = xStream.FindDescendant("text", "urn:ietf:params:xml:ns:xmpp-streams");
				if (xText != null)
					message = xText.Text;
				return;
			}
		}
	}


}
