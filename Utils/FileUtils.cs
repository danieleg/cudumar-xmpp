using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Cudumar.Utils {
	public class FileUtils {
		public static string SizeToByteString(long bytes) {
			const long kilobyte = 1024;
			const long megabyte = 1024 * kilobyte;
			const long gigabyte = 1024 * megabyte;
			const long terabyte = 1024 * gigabyte;

			if (bytes > terabyte) return (Math.Round(bytes / (double)terabyte)).ToString("0 TB");
			else if (bytes > gigabyte) return (Math.Round(bytes / (double)gigabyte)).ToString("0 GB");
			else if (bytes > megabyte) return (Math.Round(bytes / (double)megabyte)).ToString("0 MB");
			else if (bytes > kilobyte) return (Math.Round(bytes / (double)kilobyte)).ToString("0 KB");
			else return bytes + " Bytes";
		}

		public static string PurgeStrongFilename(string filename) {
			return Regex.Replace(filename, @"[^a-zA-Z0-9.]", "_");	// sostituisce tutti i caratteri NON alfanumerici con uno backspace
		}
	}
}
