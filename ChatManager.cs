using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Base.Xmpp.Core;

namespace Cudumar.Core {
	class ChatManager {
		private Dictionary<string, Chat> chats = new Dictionary<string, Chat>();
		private string GetKey(XmppJid jid) { return jid.Bare; }
		
		public Chat Get(XmppJid jid, string thread) {
			string key = GetKey(jid);
			if (chats.ContainsKey(key))
				return chats[key];
			return null;
		}
		public void Update(XmppJid jid, Chat ci) {
			string key = GetKey(jid);
			if (chats.ContainsKey(key))
				chats.Remove(key);
			chats.Add(key, ci);
		}
		public void Clear() {
			foreach (Chat ci in chats.Values) {
				try {
					if (ci != null)
						ci.Close();
				}
				catch { }
			}

			chats.Clear();		
		}
		public bool CheckIfPendingChat() {
			foreach (Chat ci in chats.Values) {
				if (ci != null && ci.IsLoaded && ci.IsVisible && !ci.IsEmptyChat)
					return true;
			}

			return false;
		}

		public int Count() { return chats.Count; }
		public IEnumerable<Chat> GetValues() { return chats.Values; }
		public void SetConversationInput(bool enableInput) {
			foreach (Chat ci in chats.Values) {
				if (ci != null)
					ci.SetInput(enableInput);
			}
		}
	}
}
