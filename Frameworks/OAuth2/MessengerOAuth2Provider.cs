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
	public class MessengerOAuth2Provider : OAuth2Provider {
		private const string AuthEndpoint = "https://login.live.com/oauth20_authorize.srf";
		private const string RedirectEndpoint = "https://login.live.com/oauth20_desktop.srf";
		private const string TokenEndpoint = "https://login.live.com/oauth20_token.srf";
		private const string RestEndpoint = "https://apis.live.net/v5.0";
		public MessengerOAuth2Provider() {
			ClientID = "00000000440C0BC6";
			ClientSecret = "bwujnq1YCBqPCxOXsR6BETNHafSvQ3e7";
			LoginUri = new Uri(string.Format("{0}?client_id={1}&redirect_uri={2}&scope=wl.messenger&response_type=code", AuthEndpoint, ClientID, RedirectEndpoint));
			ReturnUri = new Uri(RedirectEndpoint);

			WebUtils.DeleteAllCookie(new Uri("http://www.live.com"));
			WebUtils.DeleteAllCookie(new Uri("https://www.live.com"));
		}

		protected override string PerformLogin(Uri ResultLoginUri) {
			NameValueCollection query = ParseQueryString(ResultLoginUri.Query);
			if (query["code"] == null)
				return null;

			string reply = string.Empty;
			string code = query["code"];
			reply = new System.Net.WebClient().DownloadString(
				string.Format("{0}?client_id={1}&redirect_uri={2}&client_secret={3}&code={4}&grant_type=authorization_code", TokenEndpoint, ClientID, RedirectEndpoint, ClientSecret, code)
			);

			JsonObject JsonReply = null;
			try {
				JsonReply = JsonObject.CreateFromString(reply);
			}
			catch (Exception) {
				return null;
			}

			if (!JsonReply.Dictionary.ContainsKey("access_token"))
				return null;

			return JsonReply.Dictionary["access_token"].String;
		}
	}
}
