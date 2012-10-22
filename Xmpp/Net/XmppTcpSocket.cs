using System;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;

namespace Base.Xmpp.Net {
	public class XmppTcpSocket : XmppSocket, IDisposable {
		private string hostName = string.Empty;
		private int hostPort = 0;
		private TcpClient tcpClient = null;
		private Stream tcpStream = null;

		private static bool ValidateServerCertificate(
			object sender,
			System.Security.Cryptography.X509Certificates.X509Certificate certificate,
			System.Security.Cryptography.X509Certificates.X509Chain chain,
			SslPolicyErrors sslPolicyErrors) {
			if (sslPolicyErrors == SslPolicyErrors.None || sslPolicyErrors == SslPolicyErrors.RemoteCertificateNameMismatch)
				return true;
			return false;
		}
		
		public XmppTcpSocket(string hostName, int hostPort) {
			this.hostName = hostName;
			this.hostPort = hostPort;
			tcpClient = new TcpClient(hostName, hostPort);
			tcpClient.ReceiveTimeout = 0;// 5000;
			tcpClient.SendTimeout = 5000;
			tcpClient.NoDelay = true;
			tcpStream = tcpClient.GetStream();
		}
		public override void Connect() {
			if (!tcpClient.Connected)
				tcpClient.Connect(hostName, hostPort);
		}
		public override void Disconnect() {
			tcpClient.Close();
		}
		public override bool IsConnected {
			get { return tcpClient.Connected; }
			protected set { }
		}
		public override bool StartSSL() {
			SslStream sslStream = new SslStream(tcpStream, false, new RemoteCertificateValidationCallback (ValidateServerCertificate));
			sslStream.AuthenticateAsClient(hostName);
			tcpStream = sslStream;
			return sslStream.IsAuthenticated;
		}
		public override void WriteStream(string outString) {
			byte[] data = encoder.GetBytes(outString);
			tcpStream.Write(data, 0, data.Length);
			tcpStream.Flush();
		}
		public override string ReadStream() {
			byte[] data = new Byte[8192];
			Int32 bytes = tcpStream.Read(data, 0, data.Length);
			return encoder.GetString(data, 0, bytes);
		}

		~XmppTcpSocket() { Dispose(); }
		public override void Dispose() {
			if (tcpStream != null) {
				tcpStream.Flush();
				tcpStream.Close();
				tcpStream.Dispose();
			}
			if (tcpClient != null)
				tcpClient.Close();
		}
	}
}
