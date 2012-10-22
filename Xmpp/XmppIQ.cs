using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using System.Linq;

namespace Base.Xmpp.Core {
	public class XmppIQ : IDisposable {
		XElement xRoot = null;

		~XmppIQ() { Dispose(); }
		public void Dispose() {
			if (xRoot != null) {
				xRoot.RemoveAll();
				xRoot = null;
			}
		}

		private XmppIQ(XElement xElement) {
			xRoot = xElement;
		}
		public XmppIQ(string stream) {
			if (!string.IsNullOrWhiteSpace(stream))
				xRoot = XElement.Parse(stream);
		}
		public static XmppIQ Parse(string stream) {
			return new XmppIQ(stream);
		}
		public List<XmppIQ> Children() {
			if (xRoot == null || !xRoot.HasElements)
				return new List<XmppIQ>(0);

			List<XmppIQ> result = new List<XmppIQ>();
			foreach (XNode node in xRoot.Nodes())
				result.Add(new XmppIQ(node as XElement));

			return result;
		}
		public string GetAttribute(string name) {
			if (xRoot.Attribute(name) != null)
				return xRoot.Attribute(name).Value;

			return string.Empty;
		}
		public XmppIQ FindDescendant(string name, string nameSpace = null) {
			if (xRoot == null || !xRoot.HasElements)
				return null;

			if (string.IsNullOrWhiteSpace(nameSpace))
				nameSpace = string.Empty;
			foreach (XElement xItem in xRoot.Descendants()) {
				if (xItem.Name.LocalName == name && xItem.Name.NamespaceName == nameSpace)
					return new XmppIQ(xItem);
			}

			return null;
		}
		public XmppIQ FindChild(string name, string nameSpace = null) {
			if (xRoot == null || !xRoot.HasElements)
				return null;

			if (string.IsNullOrWhiteSpace(nameSpace))
				nameSpace = string.Empty;
			foreach (XNode node in xRoot.Nodes()) {
				XElement xItem = node as XElement;
				if (xItem.Name.LocalName == name && xItem.Name.NamespaceName == nameSpace)
					return new XmppIQ(xItem);
			}

			return null;
		}
		public bool Equal(string name, string nameSpace) {
			if (xRoot == null)
				return false;
			return xRoot.Name.LocalName == name && xRoot.Name.NamespaceName == nameSpace;
		}
		public string Name {
			get {
				if (xRoot != null)
					return xRoot.Name.LocalName;
				else return null;
			}
		}
		public string Namespace {
			get {
				if (xRoot != null)
					return xRoot.Name.NamespaceName;
				else return null;
			}
		}
		public string Text {
			get {
				if (xRoot != null)
					return xRoot.Value;
				else return null;
			}
		}

		public override string ToString() {
			return xRoot.ToString();
		}
	}

	class XmppStream {
		public static bool IsValid(string stream) {
			Regex r = new Regex(">.*?<");
			string[] tokens = r.Split(stream.Trim());
			if (tokens.Length > 0) {
				if (tokens[0].StartsWith("<"))
					tokens[0] = tokens[0].Remove(0, 1);
				if (tokens[tokens.Length - 1].EndsWith(">"))
					tokens[tokens.Length - 1] = tokens[tokens.Length - 1].Remove(tokens[tokens.Length - 1].Length - 1);
			}

			Stack<string> q = new Stack<string>(tokens.Length);
			foreach (string s in tokens) {
				string token = s;//.Trim();
				int space = token.IndexOf(' ');
				if (space == -1)
					space = token.Length;

				if (token.EndsWith("/"))
					continue;

				token = token.Substring(0, space);
				if (token.StartsWith("/")) {
					if (q.Count == 0)
						return false;
					string popped = q.Pop();
					string tt = token.Remove(0, 1);
					if (popped != tt)
						return false;
				} else
					q.Push(token);
			}

			bool result = (q.Count == 0);
			q.TrimExcess();

			return result;
		}
	}


	/* stanza
			<xs:element ref='bad-request'/>
			<xs:element ref='conflict'/>
			<xs:element ref='feature-not-implemented'/>
			<xs:element ref='forbidden'/>
			<xs:element ref='gone'/>
			<xs:element ref='internal-server-error'/>
			<xs:element ref='item-not-found'/>
			<xs:element ref='jid-malformed'/>
			<xs:element ref='not-acceptable'/>
			<xs:element ref='not-allowed'/>
			<xs:element ref='payment-required'/>
			<xs:element ref='recipient-unavailable'/>
			<xs:element ref='redirect'/>
			<xs:element ref='registration-required'/>
			<xs:element ref='remote-server-not-found'/>
			<xs:element ref='remote-server-timeout'/>
			<xs:element ref='resource-constraint'/>
			<xs:element ref='service-unavailable'/>
			<xs:element ref='subscription-required'/>
			<xs:element ref='undefined-condition'/>
			<xs:element ref='unexpected-request'/>
	 * */
	/*
	public class XmppError
	{
		public static XmppErrorStreamInfo ParseStreamError(XmppIQ stream)
		{
			if (stream == null)
				return null;

			string name = stream.Name;			
			if (name != "error")
				return null;

			string message = string.Empty;
			XmppIQ xText = stream.FindChild("text", "urn:ietf:params:xml:ns:xmpp-streams");
			if (xText != null)
				message = xText.Text;

			return new XmppErrorStreamInfo(
				parseStreamErrorName(name),
				name,
				message);
		}
		private static ErrorStreamCondition parseStreamErrorName(string name)
		{
			switch (name)
			{
				case "bad-format": return ErrorStreamCondition.BadFormat;
				case "bad-namespace-prefix": return ErrorStreamCondition.BadNamespacePrefix;
				case "conflict": return ErrorStreamCondition.Conflict;
				case "connection-timeout": return ErrorStreamCondition.ConnectionTimeout;
				case "host-gone": return ErrorStreamCondition.HostGone;
				case "host-unknown": return ErrorStreamCondition.HostUnknown;
				case "improper-addressing": return ErrorStreamCondition.ImproperAddressing;
				case "internal-server-error": return ErrorStreamCondition.InternalServerError;
				case "invalid-from": return ErrorStreamCondition.InvalidFrom;
				case "invalid-id": return ErrorStreamCondition.InvalidId;
				case "invalid-namespace": return ErrorStreamCondition.InvalidNamespace;
				case "invalid-xml": return ErrorStreamCondition.InvalidXml;
				case "not-authorized": return ErrorStreamCondition.NotAuthorized;
				case "policy-violation": return ErrorStreamCondition.PolicyViolation;
				case "remote-connection-failed": return ErrorStreamCondition.RemoteConnectionFailed;
				case "resource-constraint": return ErrorStreamCondition.ResourceConstraint;
				case "restricted-xml": return ErrorStreamCondition.RestrictedXml;
				case "see-other-host": return ErrorStreamCondition.SeeOtherHost;
				case "system-shutdown": return ErrorStreamCondition.SystemShutdown;
				case "undefined-condition": return ErrorStreamCondition.UndefinedCondition;
				case "unsupported-encoding": return ErrorStreamCondition.UnsupportedEncoding;
				case "unsupported-stanza-type": return ErrorStreamCondition.UnsupportedStanzaType;
				case "unsupported-version": return ErrorStreamCondition.UnsupportedVersion;
				case "xml-not-well-formed": return ErrorStreamCondition.XmlNotWellFormed;
			}

			return ErrorStreamCondition.NULL;
		}
	}


	public class XmppErrorStreamInfo
	{
		public ErrorStreamCondition Type { get; private set; }
		public string Error { get; private set; }
		public string Message { get; private set; }
		public XmppErrorStreamInfo(ErrorStreamCondition type, string error, string message)
		{
			Error = error;
			Message = message;
			Type = type;
		}
	}
	public enum ErrorStreamCondition
	{
		NULL = -1,
		BadFormat = 0,
		BadNamespacePrefix = 1,
		Conflict = 2,
		ConnectionTimeout = 3,
		HostGone = 4,
		HostUnknown = 5,
		ImproperAddressing = 6,
		InternalServerError = 7,
		InvalidFrom = 8,
		InvalidId = 9,
		InvalidNamespace = 10,
		InvalidXml = 11,
		NotAuthorized = 12,
		PolicyViolation = 13,
		RemoteConnectionFailed = 14,
		ResourceConstraint = 15,
		RestrictedXml = 0x10,
		SeeOtherHost = 0x11,
		SystemShutdown = 0x12,
		UndefinedCondition = 0x13,
		UnsupportedEncoding = 20,
		UnsupportedStanzaType = 0x15,
		UnsupportedVersion = 0x16,
		XmlNotWellFormed = 0x17
	}*/
}
