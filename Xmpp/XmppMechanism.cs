using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Base.Xmpp.Utils;
using System.Security.Cryptography;
using System.Net;
using System.IO;
using System.Collections.Specialized;

//MECHANISMS           USAGE    REFERENCE   OWNER
//----------           -----    ---------   -----
//KERBEROS_V4          LIMITED  [RFC2222]   IESG <iesg@ietf.org>
//GSSAPI               COMMON   [RFC2222]   IESG <iesg@ietf.org> 
//SKEY                 OBSOLETE [RFC2444]   IESG <iesg@ietf.org>
//EXTERNAL             COMMON   [RFC2222]   IESG <iesg@ietf.org>
//CRAM-MD5             LIMITED  [RFC2195]   IESG <iesg@ietf.org> 
//ANONYMOUS            COMMON   [RFC2245]   IESG <iesg@ietf.org>
//OTP                  COMMON   [RFC2444]   IESG <iesg@ietf.org>
//GSS-SPNEGO           LIMITED  [Leach]     Paul Leach <paulle@microsoft.com>
//PLAIN                COMMON   [RFC2595]   IESG <iesg@ietf.org>
//SECURID              COMMON   [RFC2808]   Magnus Nystrom <magnus@rsasecurity.com>
//NTLM                 LIMITED  [Leach]     Paul Leach <paulle@microsoft.com>
//NMAS_LOGIN           LIMITED  [Gayman]    Mark G. Gayman <mgayman@novell.com>
//NMAS_AUTHEN          LIMITED  [Gayman]    Mark G. Gayman <mgayman@novell.com>
//DIGEST-MD5           COMMON   [RFC2831]   IESG <iesg@ietf.org>
//9798-U-RSA-SHA1-ENC  COMMON    [RFC3163]  robert.zuccherato@entrust.com
//9798-M-RSA-SHA1-ENC  COMMON   [RFC3163]   robert.zuccherato@entrust.com
//9798-U-DSA-SHA1      COMMON   [RFC3163]   robert.zuccherato@entrust.com
//9798-M-DSA-SHA1      COMMON   [RFC3163]   robert.zuccherato@entrust.com
//9798-U-ECDSA-SHA1    COMMON   [RFC3163]   robert.zuccherato@entrust.com
//9798-M-ECDSA-SHA1    COMMON   [RFC3163]   robert.zuccherato@entrust.com
//KERBEROS_V5          COMMON   [Josefsson] Simon Josefsson <simon@josefsson.org>
//NMAS-SAMBA-AUTH      LIMITED  [Brimhall]  Vince Brimhall <vbrimhall@novell.com>

namespace Base.Xmpp.Mechanism {
	abstract class MechanismProvider {
		public abstract string GetAuthStream();
	}

	#region ANONYMOUS
	class AnonimousMechanismProvider : MechanismProvider {
		public AnonimousMechanismProvider() { }
		public override string GetAuthStream() {
			return "<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"ANONYMOUS\"/>";
		}
	}
	#endregion
	
	#region PLAIN
	class PlainMechanismProvider : MechanismProvider {
		private string username, password;
		public PlainMechanismProvider(string username, string password) {
			this.username = username;
			this.password = password;
		}
		public override string GetAuthStream() {
			return "<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"PLAIN\">" +
					Base64Utils.Encode("\0" + username + "\0" + password, Encoding.UTF8) + "</auth>";
		}
	}
	#endregion

	#region DIGEST-MD5
	class DigestMD5MechanismProvider : MechanismProvider {
		private Encoding encoder = Encoding.UTF8;
		private string username, password;
		public DigestMD5MechanismProvider(string username, string password) {
			this.username = username;
			this.password = password;
		}
		public override string GetAuthStream() {
			return "<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"DIGEST-MD5\"/>";
		}
		public string GetChallengeResponse(string challengeToken) {
			string challenge = Base64Utils.Decode(challengeToken, encoder);
			string[] challengeSplitted = challenge.Split(new char[] { ',' });

			string realm = string.Empty;
			string nonce = string.Empty;
			string qop = string.Empty;
			foreach (string pair in challengeSplitted) {
				int indexEqual = pair.IndexOf('=');
				if (indexEqual < 0)
					continue;

				string key = pair.Remove(indexEqual);
				string value = pair.Substring(indexEqual + 1);

				if (key == "realm")
					realm = value.Substring(1, value.Length - 2);
				else if (key == "nonce")
					nonce = value.Substring(1, value.Length - 2);
				else if (key == "qop")
					qop = value.Substring(1, value.Length - 2);
			}

			string digest = string.Format("xmpp/{0}", realm);
			string cnonce = Guid.NewGuid().ToString().Replace("-", string.Empty).ToLowerInvariant();
			string nc = "00000001";

			HashAlgorithm md5 = MD5.Create();
			string x = string.Format("{0}:{1}:{2}", username, realm, password);
			byte[] y1 = CryptoUtils.HashByte(md5, x);
			byte[] y2 = encoder.GetBytes(string.Format(":{0}:{1}", nonce, cnonce));
			byte[] a1 = new byte[y1.Length + y2.Length];
			y1.CopyTo(a1, 0);
			y2.CopyTo(a1, y1.Length);

			string a2 = string.Format("AUTHENTICATE:{0}", digest);

			string ha1 = ConvertToBase16String(CryptoUtils.HashByte(md5, a1));
			string ha2 = ConvertToBase16String(CryptoUtils.HashByte(md5, a2));
			string kd = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", ha1, nonce, nc, cnonce, "auth", ha2);
			string z = ConvertToBase16String(CryptoUtils.HashByte(md5, kd));

			StringBuilder response = new StringBuilder();
			response.AppendFormat("username=\"{0}\",", username);
			response.AppendFormat("realm=\"{0}\",", realm);
			response.AppendFormat("nonce=\"{0}\",", nonce);
			response.AppendFormat("cnonce=\"{0}\",", cnonce);
			response.AppendFormat("nc={0},", nc);
			response.AppendFormat("qop=auth,");
			response.AppendFormat("digest-uri=\"{0}\",", digest);
			response.AppendFormat("response={0},", z);
			response.AppendFormat("charset=utf-8");

			return string.Format("<response xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\">{0}</response>",
				Base64Utils.Encode(response.ToString(), Encoding.UTF8));
		}

		private static string ConvertToBase16String(byte[] bytes) {
			StringBuilder buffer = new StringBuilder();
			foreach (byte b in bytes)
				buffer.Append(Convert.ToString(b, 16).PadLeft(2, '0'));

			return buffer.ToString();
		}
	}
	#endregion

	#region X-GOOGLE-TOKEN
	class XGoogleTokenMechanismProvider : MechanismProvider {
		private string username, password, resource;
		public XGoogleTokenMechanismProvider(string username, string password, string resource) {
			this.username = username;
			this.password = password;
			this.resource = resource;
		}
		public override string GetAuthStream() {
			return "<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"X-GOOGLE-TOKEN\">" +
					Base64Utils.Encode("\0" + username + "\0" + RequestToken(), Encoding.UTF8) + "</auth>";
		}
		private string RequestToken() {
			string _sid = string.Empty, _lsid = string.Empty, _auth = string.Empty;
			HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.google.com/accounts/ClientAuth");
			StringBuilder requestString = new StringBuilder();
			request.Method = "POST";
			request.ContentType = "application/x-www-form-urlencoded";

			Stream stream = request.GetRequestStream();
			requestString.AppendFormat("Email={0}", username);
			requestString.AppendFormat("&Passwd={0}", password);
			requestString.AppendFormat("&source={0}", resource);
			requestString.AppendFormat("&service={0}", "mail");
			requestString.AppendFormat("&PersistentCookie={0}", false);

			byte[] buffer = Encoding.UTF8.GetBytes(requestString.ToString());
			stream.Write(buffer, 0, buffer.Length);
			stream.Dispose();

			HttpWebResponse response = (HttpWebResponse)request.GetResponse();
			
			if (response.StatusCode == HttpStatusCode.OK) {
				Stream responseStream = response.GetResponseStream();
				if (responseStream != null) {
					StreamReader responseReader = new StreamReader(responseStream, true);

					while (responseReader.Peek() != -1) {
						var data = responseReader.ReadLine();
						if (data != null) {
							if (data.StartsWith("SID=")) {
								_sid = data.Replace("SID=", "");
							} else if (data.StartsWith("LSID=")) {
								_lsid = data.Replace("LSID=", "");
							} else if (data.StartsWith("Auth=")) {
								_auth = data.Replace("Auth=", "");
							}
						}
					}

					responseStream.Dispose();
					responseReader.Dispose();
				}
			}

			return _auth;
		}
	}
	#endregion

	#region X-MESSENGER-OAUTH2
	class XMessengerOAuth2MechanismProvider : MechanismProvider {
		private string username, oAuthToken;
		public XMessengerOAuth2MechanismProvider(string username, string oAuthToken) {
			this.username = username;
			this.oAuthToken = oAuthToken;
		}
		public override string GetAuthStream() {
			return "<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"X-MESSENGER-OAUTH2\">" + RequestToken() + "</auth>";
		}
		private string RequestToken() {
			return oAuthToken;
		}
	}
	#endregion

	#region X-FACEBOOK-PLATFORM
		class XFacebookPlatformMechanismProvider : MechanismProvider {
		private string username, oAuthToken;
		public XFacebookPlatformMechanismProvider(string username, string oAuthToken) {
			this.username = username;
			this.oAuthToken = oAuthToken;
		}
		public override string GetAuthStream() {
			return "<auth xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\" mechanism=\"X-FACEBOOK-PLATFORM\" />";
		}
		public string GetChallengeResponse(string challengeToken) {
			NameValueCollection query = Base64Utils.Decode(challengeToken, Encoding.UTF8).ParseQueryString();
			string version = query["version"];
			string method = query["method"];
			string nonce = query["nonce"];
			string response = string.Format("method={0}&api_key={1}&access_token={2}&call_id={3}&v={4}&nonce={5}",
				method, new Cudumar.Frameworks.OAuth2.FacebookOAuth2Provider().ClientID, RequestToken(), Environment.TickCount, version, nonce);
			return string.Format("<response xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\">{0}</response>",
				Base64Utils.Encode(response, Encoding.UTF8));
		}

		private string RequestToken() {
			return oAuthToken;
		}
	}

	#endregion

	#region X-OAUTH2

	#endregion
	

	/*
	public enum MechanismType
	{
		NONE,
		KERBEROS_V4,
		GSSAPI,
		SKEY,
		EXTERNAL,
		CRAM_MD5,
		ANONYMOUS,
		OTP,
		GSS_SPNEGO,
		PLAIN,
		SECURID,
		NTLM,
		NMAS_LOGIN,
		NMAS_AUTHEN,
		DIGEST_MD5,
		ISO_9798_U_RSA_SHA1_ENC,
		ISO_9798_M_RSA_SHA1_ENC,
		ISO_9798_U_DSA_SHA1,
		ISO_9798_M_DSA_SHA1,
		ISO_9798_U_ECDSA_SHA1,
		ISO_9798_M_ECDSA_SHA1,
		KERBEROS_V5,
		NMAS_SAMBA_AUTH,
		X_GOOGLE_TOKEN
	}

	public enum FailureCondition
	{
		aborted,
		incorrect_encoding,
		invalid_authzid,
		invalid_mechanism,
		mechanism_too_weak,
		not_authorized,
		temporary_auth_failure,
		UnknownCondition
	}
	*/
}
