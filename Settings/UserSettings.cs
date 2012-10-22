using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Base.Xmpp.Core;
using Cudumar.Core;
using System.Xml.Serialization;
using System.Xml;
using System.IO;
using Cudumar.Utils;

namespace Cudumar.Settings {
	[Serializable]
	public class UserSettings {
		public RosterViewerMode RosterVwrMode = RosterViewerMode.Default;
		public XmppPresenceStatus PresenceStatus = XmppPresenceStatus.Unavailable;
		public string PresenceMessage = string.Empty;
		public List<string> PastPresenceMessages = new List<string>();
		[XmlIgnore]
		[NonSerialized]
		public XmppVCard VCard = null;
		public string VCardPhotoHash = null;
		public static UserSettings Empty { get { return new UserSettings() { }; } }
	}

	public abstract class UserSettingsProvider {
		public abstract bool Save(UserSettings settings);
		public abstract UserSettings Load();
		protected readonly string username;
		protected readonly string server;
		public UserSettingsProvider(string server, string username) {
			this.username = username;
			this.server = server;
		}
	}

	public class DummyUserSettingsProvider : UserSettingsProvider {
		public override bool Save(UserSettings settings) { return true; }
		public override UserSettings Load() { return UserSettings.Empty; }
		public DummyUserSettingsProvider() : base(null, null) { }
	}

	public class LocalUserSettingsProvider : UserSettingsProvider {
		public LocalUserSettingsProvider(string server, string username)
			: base(server, username) {
			fileName = string.Format("{0}_{1}.txt", server, username);
		}

		protected static readonly string FeaturePath = "settings";
		protected string fileName;
		private readonly object syncSave = new object();
		
		public override bool Save(UserSettings settings) {
			if (settings == null)
				return false;
			try {
				string filePath = Path.Combine(Repository.GetFullPath(FeaturePath), fileName);
				lock (syncSave)
					SerializationUtils.SerializeToXmlFile(filePath, settings);
			}
			catch (Exception ex) { Cudumar.Utils.DialogUtils.ShowException(null, ex); return false; }
			return true;
		}
		public override UserSettings Load() {
			UserSettings result = null;
			try {
				string filePath = Path.Combine(Repository.GetFullPath(FeaturePath), fileName);
				if (File.Exists(filePath))
					result = SerializationUtils.DeserializeFromXmlFile<UserSettings>(filePath);
			}
			catch (Exception ex) { /*Cudumar.Utils.DialogUtils.ShowException(null, ex);*/ }
			return result ?? UserSettings.Empty;
		}
	}
	public class CryptedLocalUserSettingsProvider : LocalUserSettingsProvider {
		public CryptedLocalUserSettingsProvider(string server, string username)
			: base(server, username) {
			string tmp = server + "_" + username;
			tmp = Base.Xmpp.Utils.CryptoUtils.SHA1Hash(tmp, Encoding.Default);
			fileName = tmp + ".txt";
		}
	}
}
