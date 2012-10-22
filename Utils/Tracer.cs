using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Xml.Linq;
using System.Windows.Input;
using System.Reflection;

namespace Cudumar.Utils {
	public abstract class TraceBase {		
		public virtual void UpdateBandRates(ref long byteRecv, ref long byteSend) { }
	}

	public class TraceStream : TraceBase {
		public TraceStream(string prop1, string stream) {
			this.Timestamp = DateTime.Now.ToString();
			this._ = prop1;
			this.Stream = UIHelper.XmlIndent(stream);
		}
		public string Timestamp { get; set; }
		public string _ { get; set; }
		public string Stream { get; set; }
		public override void UpdateBandRates(ref long byteRecv, ref long byteSend) {
			if (_.ToLower().Contains("send"))
				byteSend += Stream.Length;
			else
				byteRecv += Stream.Length;
		}
	}
	public class TraceLog : TraceBase {
		public TraceLog(string message) {
			this.Timestamp = DateTime.Now.ToString();
			this.Message = message;
		}
		public string Timestamp { get; set; }
		public string Message { get; set; }
	}

	public class Tracer<T> where T : TraceBase {
		private readonly ObservableCollection<T> data = new ObservableCollection<T>();
		private readonly Window Owner;
		private readonly string Title;
		private readonly bool IsBandMonitorEnabled = false;
		private Window view = null;
		private long ByteRecv = 0, ByteSend = 0;

		public Tracer(Window owner, string title, bool isBandMonitorEnabled = false) {
			this.Owner = owner;
			this.Title = title;
			this.IsBandMonitorEnabled = isBandMonitorEnabled;
		}
		public void Add(T item) {
			item.UpdateBandRates(ref ByteRecv, ref ByteSend);
			data.Add(item);			
		}
		public void Clear() { 
			data.Clear();
			ByteRecv = ByteSend = 0;
		}
		public void Close() {
			if (view != null && view.IsLoaded)
				view.Close();
		}
		public void Show() {
			if (view == null || !view.IsLoaded) 
				view = CreateWindow();
			else view.Focus();

			view.Show();
		}

		private Window CreateWindow() {
			Window result = new Window();
			DataGrid Grid = new DataGrid() { AutoGenerateColumns = true };
			result.Owner = Owner;
			result.Title = Title;

			Grid.ClipboardCopyMode = DataGridClipboardCopyMode.ExcludeHeader;
			Grid.HeadersVisibility = DataGridHeadersVisibility.None;
			Grid.ItemsSource = data;
			result.Content = Grid;

			result.Height = 400;
			result.Width = 800;

			OnDataChanged(result);
			data.CollectionChanged += (sender, e) => {
				OnDataChanged(result);
			};

			return result;
		}
		private void OnDataChanged(Window window) {
			window.Title = Title;
			if (IsBandMonitorEnabled)
				window.Title += string.Format(" [U {0} | D {1} ]", FileUtils.SizeToByteString(ByteSend), FileUtils.SizeToByteString(ByteRecv));
			window.Title += " - " + DateTime.Now.ToString();
		}
	}
}
