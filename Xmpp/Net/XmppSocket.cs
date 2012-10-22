using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Base.Xmpp.Net {
	public abstract class XmppSocket : IDisposable {
		protected Encoding encoder = Encoding.UTF8;

		public XmppSocket() { }
		public abstract void Connect();
		public abstract void Disconnect();
		public abstract bool IsConnected { get; protected set; }
		public abstract bool StartSSL();
		public abstract void WriteStream(string outString);
		public abstract string ReadStream();
		public abstract void Dispose();
	}
}
