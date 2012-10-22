using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Threading;
using System.Windows.Threading;

namespace Cudumar.Utils {
	public static class UIHelper {

		public static string XmlIndent(string xmlStr) {
			try {
				return XElement.Parse(xmlStr, LoadOptions.PreserveWhitespace).ToString(SaveOptions.None);
			}
			catch { }
			return xmlStr;
		}

		public static void DelayInvoke(this Dispatcher dispatcher, TimeSpan ts, Action action) {
			DispatcherTimer delayTimer = new DispatcherTimer(DispatcherPriority.Send, dispatcher);
			delayTimer.Interval = ts;
			delayTimer.Tick += (s, e) => {
				delayTimer.Stop();
				action();
			};
			delayTimer.Start();
		}
	}

	public class DispatcherTimerContainingAction : System.Windows.Threading.DispatcherTimer {
		/// <summary>
		/// uncomment this to see when the DispatcherTimerContainingAction is collected
		/// if you remove  t.Tick -= onTimeout; line from onTimeout method
		/// you will see that the timer is never collected
		/// </summary>
		//~DispatcherTimerContainingAction()
		//{
		//    throw new Exception("DispatcherTimerContainingAction is disposed");
		//}

		public Action Action { get; set; }
	}



}
