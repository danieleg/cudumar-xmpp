using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Base.Xmpp.Utils {
	/// <summary>
	/// Helps to encode/decode base64 strings.
	/// </summary>
	static class Base64Utils {
		public static string Encode(string inString, Encoding codec) {
			byte[] encbuff = codec.GetBytes(inString);
			return Convert.ToBase64String(encbuff);
			/*char[] charArr = inString.ToCharArray();
			byte[] inData = new byte[charArr.Length];
			for (int i = 0; i < charArr.Length; i++) {
				inData[i] = (byte)charArr[i];
			}

			return Convert.ToBase64String(inData, 0, inData.Length);*/
		}

		public static string Decode(string inBase64, Encoding codec) {
			byte[] inData = Convert.FromBase64String(inBase64);
			return codec.GetString(inData);
		}
	}

	static class CryptoUtils {
		private static string Compute(HashAlgorithm algo, byte[] input) {
			byte[] data = algo.ComputeHash(input);
			StringBuilder builder = new StringBuilder();
			foreach (byte b in data)
				builder.Append(b.ToString("x2"));
			return builder.ToString();
		}
		public static string MD5HashB64(string input) {
			return Convert.ToBase64String(HashByte(MD5.Create(), Encoding.UTF8.GetBytes(input)));
		}
		public static string SHA1HashB64(string input) {
			return Convert.ToBase64String(HashByte(SHA1.Create(), Encoding.UTF8.GetBytes(input)));
		}
		public static string SHA1Hash(string input, Encoding codec) {
			return Compute(SHA1.Create(), codec.GetBytes(input));
		}
		public static byte[] HashByte(HashAlgorithm algo, string input) {
			return algo.ComputeHash(Encoding.UTF8.GetBytes(input));
		}
		public static byte[] HashByte(HashAlgorithm algo, byte[] input) {
			return algo.ComputeHash(input);
		}
	}

	static class StringExtension {
		public static string HtmlEncode(this string s) {
			return System.Net.WebUtility.HtmlEncode(s);
		}
		public static string HtmlDecode(this string s) {
			return System.Net.WebUtility.HtmlDecode(s);
		}

		public static NameValueCollection ParseQueryString(this string s) {
			NameValueCollection nvc = new NameValueCollection();

			// remove anything other than query string from url
			if (s.Contains("?")) {
				s = s.Substring(s.IndexOf('?') + 1);
			}

			foreach (string vp in Regex.Split(s, "&")) {
				string[] singlePair = Regex.Split(vp, "=");
				if (singlePair.Length == 2) {
					nvc.Add(singlePair[0], singlePair[1]);
				} else {
					// only one key with no value specified in query string
					nvc.Add(singlePair[0], string.Empty);
				}
			}

			return nvc;
		}

	}
}
