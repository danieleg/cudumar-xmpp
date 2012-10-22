using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using Cudumar.Core;
using Base.Xmpp.Core;

namespace Cudumar.Settings {
	[Serializable]
	public class GlobalSettings {
		public List<Tuple<string, string>> RecentUser = new List<Tuple<string, string>>();
		public static GlobalSettings Empty { get { return new GlobalSettings() { }; } }
	}
}
