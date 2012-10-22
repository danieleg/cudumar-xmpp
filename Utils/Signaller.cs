using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.Concurrent;
using Base.Xmpp.Core;

namespace Cudumar.Utils {
	public class Signaller<T> {
		private ConcurrentDictionary<int, ConcurrentQueue<Action<T>>> signals = new ConcurrentDictionary<int, ConcurrentQueue<Action<T>>>();
		public void Register(int id, Action<T> onSignal) {
			if (signals.ContainsKey(id)) {
				ConcurrentQueue<Action<T>> q = signals[id];
				if (q == null) {
					q = new ConcurrentQueue<Action<T>>();
					signals.GetOrAdd(id, q);
				}
				
				q.Enqueue(onSignal);
			} else {
				ConcurrentQueue<Action<T>> q = new ConcurrentQueue<Action<T>>();
				q.Enqueue(onSignal);
				signals.GetOrAdd(id, q);
			}
		}
		public void Signal(int id, T par) {
			if (signals.ContainsKey(id)) {
				ConcurrentQueue<Action<T>> q = signals[id];
				if (q != null) {
					while(!q.IsEmpty) {
						Action<T> onSignal = null;
						if (q.TryDequeue(out onSignal) && onSignal != null)
							onSignal(par);
					}
				}
			}
		}
		public void Clear() {
			signals.Clear();
		}
	}

	public static class GlobalSignaller<T> {
		private static Signaller<T> signaller = null;
		private static Signaller<T> Singleton() {
			if (signaller == null)
				signaller = new Signaller<T>();
			return signaller;
		}
		public static void Register(int id, Action<T> onSignal) {
			Singleton().Register(id, onSignal);
		}
		public static void Signal(int id, T par) {
			Singleton().Signal(id, par);
		}
	}
}
