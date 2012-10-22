using System;
using System.IO;
using System.Text;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Base.Xmpp.Utils;
using System.Windows;
using Cudumar.Utils;
using System.Collections.Concurrent;

namespace Cudumar.Settings {
	class Avatar {
		public static readonly string RelPath = "avatar";
		public static ConcurrentDictionary<string, ImageSource> Cache = new ConcurrentDictionary<string, ImageSource>();

		public static ImageSource GetSource(string hash) {
			if (Cache.ContainsKey(hash))
				return Cache[hash];
			
			BitmapImage result = new BitmapImage(new Uri(GetFullPath(hash), UriKind.Absolute));
			if (result.CanFreeze)
				result.Freeze();

			Cache.GetOrAdd(hash, result);
			return result;
		}
		public static string GetData(string hash) {
			string data = System.IO.File.ReadAllText(GetFullPath(hash), System.Text.Encoding.Default);
			return Base64Utils.Encode(data, System.Text.Encoding.Default);
		}
		private static string GetFullPath(string hash) {
			string fileName = GetOriginalName(hash);
			string filePath = Path.Combine(Repository.GetFullPath(RelPath), fileName);
			if (File.Exists(filePath))
				return filePath;
			return string.Empty;
		}
		public static bool Exist(string hash) {
			return !string.IsNullOrWhiteSpace(GetFullPath(hash));
		}
		public static void Save(string hash, string buf) {
			string fileName = GetOriginalName(hash);
			string filePath = Path.Combine(Repository.GetFullPath(RelPath), fileName);
			File.WriteAllText(filePath, buf, Encoding.Default);
		}
		public static string ComputeHash(string buf) {
			return CryptoUtils.SHA1Hash(buf, Encoding.Default);
		}
		public static void GenerateFromFile(string fullPath, out string hash) {
			const int MAX_PIXEL = 96;
			using (MemoryStream thumb = ImageUtils.Resize(System.Drawing.Image.FromFile(fullPath), MAX_PIXEL, System.Drawing.Imaging.ImageFormat.Png)) {
				string data = System.Text.Encoding.Default.GetString(thumb.ToArray());
				hash = ComputeHash(data);
				if (!Exist(hash))
					Save(hash, data);
			}
		}
		public static void ClearCache() {
			Cache.Clear();
		}

		private static string GetOriginalName(string hash) {
			return string.Format("{0}.original.avatar", hash);
		}
	}
}
