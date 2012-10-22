using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Text.RegularExpressions;

namespace Cudumar.Frameworks.OAuth2 {
	public abstract class OAuth2Provider {
		public string ClientID { get; protected set; }
		public string ClientSecret { get; protected set; }
		public Uri LoginUri { get; protected set; }
		public Uri ReturnUri { get; protected set; }
		public OAuth2Provider() { }

		public virtual string GetAccessToken() {
			Uri ResultLoginUri = LaunchBrowser();
			return PerformLogin(ResultLoginUri);
		}

		protected abstract string PerformLogin(Uri ResultLoginUri);
		protected Uri LaunchBrowser() {
			AuthBrowser browser = new AuthBrowser(LoginUri, ReturnUri);
			browser.ShowDialog();
			return browser.GetCurrentUri();
		}

		protected NameValueCollection ParseQueryString(string s) {
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
