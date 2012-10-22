using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Specialized;
using System.Web;
using System.Text.RegularExpressions;
using Cudumar.Frameworks.Json;
using Cudumar.Utils;

namespace Cudumar.Frameworks.OAuth2 {
	public class FacebookOAuth2Provider : OAuth2Provider {
		private const string AuthEndpoint = "https://www.facebook.com/dialog/oauth";
		private const string RedirectEndpoint = "https://www.facebook.com/connect/login_success.html";
		private const string TokenEndpoint = "https://graph.facebook.com/oauth/access_token";
		private const string RestEndpoint = "https://graph.facebook.com";
		public FacebookOAuth2Provider() {
			ClientID = "109395889210392";
			ClientSecret = "ac66047174e3a8fdf816977bbf65829e";
			LoginUri = new Uri(string.Format("{0}?client_id={1}&redirect_uri={2}&scope=xmpp_login", AuthEndpoint, ClientID, RedirectEndpoint));
			ReturnUri = new Uri(RedirectEndpoint);

			WebUtils.DeleteAllCookie(new Uri("http://www.facebook.com"));
			WebUtils.DeleteAllCookie(new Uri("https://www.facebook.com"));
		}

		protected override string PerformLogin(Uri ResultLoginUri) {
			NameValueCollection query = ParseQueryString(ResultLoginUri.Query);
			if (query["code"] == null)
				return null;

			string reply = string.Empty;
			string code = query["code"];
			reply = new System.Net.WebClient().DownloadString(
				string.Format("{0}?client_id={1}&redirect_uri={2}&client_secret={3}&code={4}", TokenEndpoint, ClientID, RedirectEndpoint, ClientSecret, code)
			);

			query = ParseQueryString(reply);
			return query["access_token"];
		}
	}
}
