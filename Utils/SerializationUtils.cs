namespace Cudumar.Utils {
  using System;
  using System.Collections.Generic;
  using System.Linq;
  using System.Runtime.InteropServices;
  using System.Text;
  using System.Windows;
  using System.Windows.Interop;
  using System.Windows.Media;
using System.Xml.Serialization;
using System.IO;

	public static class SerializationUtils {
		public static T DeserializeFromXmlFile<T>(string path) {
			using (StreamReader reader = new StreamReader(path)) {
				XmlSerializer serializer = new XmlSerializer(typeof(T));
				return (T)serializer.Deserialize(reader);
			}
		}

		public static T DeserializeFromXmlStream<T>(Stream stream) {
			XmlSerializer serializer = new XmlSerializer(typeof(T));
			using (StreamReader reader = new StreamReader(stream)) {
				return (T)serializer.Deserialize(reader);
			}
		}

		public static void SerializeToXmlFile(string path, object obj) {
			XmlSerializer serializer = new XmlSerializer(obj.GetType());
			using (StreamWriter writer = new StreamWriter(path)) {
				serializer.Serialize(writer, obj);
			}
		}
	}
}