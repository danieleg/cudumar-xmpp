using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Threading;
using Base.Xmpp.Core;
using Cudumar.Settings;
using System.Windows.Media;
using System.Collections.ObjectModel;
using System.Collections;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;


namespace Cudumar.Core {
	class RosterManager {
		private Dictionary<string, RosterItem> rosters = new Dictionary<string, RosterItem>();
		private string GetKey(XmppJid jid) { return jid.Bare; }
		private bool needRefresh = false;

		public RosterItem Get(XmppJid jid) {
			string key = GetKey(jid);
			if (rosters.ContainsKey(key))
				return rosters[key];
			return null;
		}
		public void Update(XmppJid jid, RosterItem ri) {
			string key = GetKey(jid);
			if (rosters.ContainsKey(key))
				rosters.Remove(key);
			rosters.Add(key, ri);
			needRefresh = true;
		}
		public void Clear() { rosters.Clear(); needRefresh = true; }
		public int Count() { return rosters.Count; }
		public bool Contains(XmppJid jid) {
			string key = GetKey(jid);
			return rosters.ContainsKey(key);
		}

		public void UpdateViewMode(RosterViewerMode viewMode) {
			Parallel.ForEach(rosters.Values, ri => {
				ri.Dispatcher.BeginInvoke(new Action(() =>
					ri.UpdateViewMode(viewMode))
				, DispatcherPriority.Normal);
			});
		}

		private ListCollectionView view = null;
		public ICollectionView UpdateRosterView() {
			if (view == null || needRefresh) {
				ObservableCollection<RosterItem> collection = new ObservableCollection<RosterItem>(rosters.Values);
				view = CollectionViewSource.GetDefaultView(collection) as ListCollectionView;
				//view.CustomSort = new RosterItemComparer();				
				needRefresh = false;
			} 		
			
			using (view.DeferRefresh()) {
				//view.GroupDescriptions.Clear();
				view.SortDescriptions.Clear();
				//view.GroupDescriptions.Add(new PropertyGroupDescription("IsOnline"));
				view.SortDescriptions.Add(new SortDescription("IsOnline", ListSortDirection.Descending));
				view.SortDescriptions.Add(new SortDescription("Text", ListSortDirection.Ascending));
			}

			return view;
		}
	}

	class RosterItemComparer : IComparer {
		public int Compare(object x, object y) {
			try {
				RosterItem ri1 = x as RosterItem;
				RosterItem ri2 = y as RosterItem;

				if (ri1.IsOnline == ri2.IsOnline)
					return ri1.Text.CompareTo(ri2.Text);

				if (ri1.IsOnline && !ri2.IsOnline)
					return -1;
				return 1;
			}
			catch { return 0; }
		}
	}

	public enum RosterViewerMode {
		Default,
		BigAvatar,
	}

	static class RosterUtils {
		public static string Presence2Str(XmppPresenceStatus p) {
			if (p == XmppPresenceStatus.Away)
				return "(Away)";
			if (p == XmppPresenceStatus.Busy)
				return "(Busy)";
			return string.Empty;
		}
		public static ImageSource Presence2Ico(XmppPresenceStatus p) {
			if (p == XmppPresenceStatus.Online)
				return Icons.UserOnlineSource;
			if (p == XmppPresenceStatus.Away)
				return Icons.UserAwaySource;
			if (p == XmppPresenceStatus.Busy)
				return Icons.UserBusySource;
			return Icons.UserUnavailableSource;
		}
		public static Brush Presence2Brush(XmppPresenceStatus p) {
			if (p == XmppPresenceStatus.Online)
				return UiEnhancer.GreenBrush;
			if (p == XmppPresenceStatus.Away)
				return UiEnhancer.YellowBrush;
			if (p == XmppPresenceStatus.Busy)
				return UiEnhancer.RedBrush;
			return UiEnhancer.LightGrayBrush;
		}

		public static FrameworkElement GetTooltip(RosterItem ri) {
			/*string pres = string.IsNullOrWhiteSpace(ri.Presence.MessageStatus) ? ri.Presence.Status.ToString() : string.Format("{0} ({1})", ri.Presence.MessageStatus.Trim(), ri.Presence.Status.ToString());
			TextBlock tbName = new TextBlock() { Text = ri.Text, FontWeight = FontWeights.Bold };
			TextBlock tbBare = new TextBlock() { Text = ri.Jid.Bare };
			TextBlock tbPres = new TextBlock() { Text = pres, FontStyle = FontStyles.Italic, TextWrapping = TextWrapping.Wrap };

			StackPanel sp = new StackPanel();
			sp.MaxWidth = 300;
			sp.MinWidth = 200;
			sp.Orientation = Orientation.Vertical;
			sp.Children.Add(tbName);
			sp.Children.Add(tbPres);
			sp.Children.Add(getLine(new Thickness(0, 4, 0, 0)));

			Image imgAvatar2 = new Image() { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(1) };
			if (Avatar.Exist(ri.Presence.PhotoHash)) {
				ImageSource avatar = Avatar.GetSource(ri.Presence.PhotoHash);
				imgAvatar2.AttachSource(avatar, null, null);
			} else {
				imgAvatar2.AttachSource(Icons.UserDefaultAvatarSource, null, null);
			}

			sp.Children.Add(imgAvatar2);
			sp.Children.Add(tbBare);
			return sp;*/

			TextBlock tbName = new TextBlock() { Text = string.Format("{0} <{1}>", ri.Text, ri.Jid.Bare) };
			TextBlock tbUpda = new TextBlock() { Text = string.Format("Last Status @ {0}", ri.PresenceUpdated.HasValue ? ri.PresenceUpdated.Value.ToString("U") : "none") };
			TextBlock tbReso = new TextBlock() { Text = string.Format("Resource # {0}", ri.Jid.Resource) };

			TextBlock tbPres = new TextBlock() { TextWrapping = TextWrapping.Wrap };
			tbPres.Inlines.Add(new Run(ri.Presence.Status.ToString() + " ") { FontWeight = FontWeights.Bold });
			tbPres.Inlines.Add(new Run(ri.Presence.MessageStatus.Trim()) { FontStyle = FontStyles.Italic });

			StackPanel sp = new StackPanel() { Margin = new Thickness(6, 0, 0, 0) };
			sp.MaxWidth = 400;
			sp.MinWidth = 200;
			sp.Orientation = Orientation.Vertical;
			sp.Children.Add(tbName);
			sp.Children.Add(tbPres);
			sp.Children.Add(getLine(new Thickness(0, 12, 0, 0)));
			sp.Children.Add(tbUpda);
			if (!string.IsNullOrWhiteSpace(ri.Jid.Resource))
				sp.Children.Add(tbReso);

			Image imgAvatar2 = new Image() { HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top, Margin = new Thickness(1) };
			if (Avatar.Exist(ri.Presence.PhotoHash)) {
				ImageSource avatar = Avatar.GetSource(ri.Presence.PhotoHash);
				imgAvatar2.AttachSource(avatar, null, null);
			} else {
				imgAvatar2.AttachSource(Icons.UserDefaultAvatarSource, null, null);
			}

			StackPanel spLayout = new StackPanel() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 2, 0, 0) };
			spLayout.Children.Add(imgAvatar2);
			spLayout.Children.Add(sp);
			return spLayout;
		}
		private static Label getLine(Thickness margin) {
			return new Label() { Content = "", Background = UiEnhancer.LightGrayBrush, Height = 1, Margin = margin, VerticalAlignment = VerticalAlignment.Center };
		}
	}
}
