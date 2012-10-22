using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Base.Xmpp.Net {
	public class XmppTcpTraceSocket : XmppTcpSocket {
		public XmppTcpTraceSocket(string hostName, int hostPort) : base (hostName, hostPort) {
			Trace("CONNECT", "Begin");
		}
		public override void Connect() {
			Trace("CONNECT", "Begin");
			base.Connect();
			Trace("CONNECT", "End OK");
		}
		public override void Disconnect() {
			Trace("DISCONNECT", "Begin");
			base.Disconnect();
			Trace("DISCONNECT", "End OK");
		}
		public override bool StartSSL() {
			Trace("STARTSSL", "Begin");
			bool result = base.StartSSL();
			Trace("STARTSSL", "End OK");
			return result;
		}
		public override void WriteStream(string outString) {			
			base.WriteStream(outString);
			Trace("SEND", outString);
		}
		public override string ReadStream() {
			string buf = base.ReadStream();
			Trace("RECV", buf);
			return buf;
		}

		private const string fileName = "trace.txt";
		private void Trace(string type, string buf) {
			string tmp = string.Format("[{0}]{1}: {2}{3}", DateTime.Now, type, buf, Environment.NewLine);
			File.AppendAllText(Path.Combine(Environment.CurrentDirectory, fileName), tmp, encoder);
		}

		~XmppTcpTraceSocket() { Dispose(); }		
	}
}
