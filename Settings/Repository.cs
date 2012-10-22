using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Base.Xmpp.Core;
using System.IO;

namespace Cudumar.Settings {
	class Repository {
		private const string ApplicationName = "cudumar-xmpp";
		public Repository() {	}

		public static string Dir {
			get {
				string localPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string appPath = Path.Combine(localPath, ApplicationName);
				if (!Directory.Exists(appPath))
					Directory.CreateDirectory(appPath);
				return appPath;
			}
		}

		public static string GetFullPath(string feature) {
			string path = Path.Combine(Repository.Dir, feature);
			if (!Directory.Exists(path))
				Directory.CreateDirectory(path);
			return path;
		}

		//public static IEnumerable<string> GetLatestUsers() {}@@@
	}



}
