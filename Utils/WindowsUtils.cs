using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;

namespace Cudumar.Utils {
	public static class WindowsUtils {
		public static T FindVisualParent<T>(DependencyObject dependencyObject) where T : class {
			if (dependencyObject == null)
				return null;

			DependencyObject candidateResult = VisualTreeHelper.GetParent(dependencyObject);
			while (candidateResult != null) {
				T found = candidateResult as T;
				if (found != null)
					return found;

				candidateResult = VisualTreeHelper.GetParent(candidateResult) as UIElement;
			}

			return null;
		}
		public static T FindChild<T>(DependencyObject dependencyObject) where T : DependencyObject {
			if (dependencyObject == null) return null;

			T candidateResult = null; int childrenCount = VisualTreeHelper.GetChildrenCount(dependencyObject); for (int i = 0; i < childrenCount; i++) {
				DependencyObject child = VisualTreeHelper.GetChild(dependencyObject, i);
				T childType = child as T; if (childType == null) {
					candidateResult = FindChild<T>(child); if (candidateResult != null) break;
				} else {
					candidateResult = (T)child; break;
				}
			} return candidateResult;
		}

		/// <summary> 
		/// Finds a Child of a given item in the visual tree.  
		/// </summary> 
		/// <param name="parent">A direct parent of the queried item.</param> 
		/// <typeparam name="T">The type of the queried item.</typeparam> 
		/// <param name="childName">x:Name or Name of child. </param> 
		/// <returns>The first parent item that matches the submitted type parameter.  
		/// If not matching item can be found, a null parent is being returned.</returns> 
		public static T FindChild<T>(DependencyObject parent, string childName)
			 where T : DependencyObject {
			// Confirm parent and childName are valid.  
			if (parent == null) return null;

			T foundChild = null;

			int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
			for (int i = 0; i < childrenCount; i++) {
				var child = VisualTreeHelper.GetChild(parent, i);
				// If the child is not of the request child type child 
				T childType = child as T;
				if (childType == null) {
					// recursively drill down the tree 
					foundChild = FindChild<T>(child, childName);

					// If the child is found, break so we do not overwrite the found child.  
					if (foundChild != null) break;
				} else if (!string.IsNullOrEmpty(childName)) {
					var frameworkElement = child as FrameworkElement;
					// If the child's name is set for search 
					if (frameworkElement != null && frameworkElement.Name == childName) {
						// if the child's name is of the request name 
						foundChild = (T)child;
						break;
					} else {
						// recursively drill down the tree 
						foundChild = FindChild<T>(child, childName);

						// If the child is found, break so we do not overwrite the found child.  
						if (foundChild != null) break;
					}
				} else {
					// child element found. 
					foundChild = (T)child;
					break;
				}
			}

			return foundChild;
		}


		public static BitmapSource ConvertToBitmapSource(this FrameworkElement element, int dpiX, int dpiY) {
			//element.CacheMode = new BitmapCache();
			// usa sempre la massima qualità
			RenderOptions.SetBitmapScalingMode(element, BitmapScalingMode.Fant);
			// lo posiziona in alto a sinistra
			element.HorizontalAlignment = HorizontalAlignment.Left;
			element.VerticalAlignment = VerticalAlignment.Top;
			// ottiene le misure in pixel
			double pageWidth = element.Width / 96 * dpiX;
			double pageHeight = element.Height / 96 * dpiY;
			// esegue il render dell'elemento su un RenderTargetBitmap
			RenderTargetBitmap r = new RenderTargetBitmap((int)pageWidth, (int)pageHeight, dpiX, dpiY, PixelFormats.Pbgra32);
			var pageRect = new Rect(0, 0, pageWidth, pageHeight);
			element.Measure(pageRect.Size);
			element.Arrange(pageRect);
			element.UpdateLayout();
			r.Render(element);
			return r;
		}

		public static void SaveToFile(this FrameworkElement element, int dpiX, int dpiY, string path) {
			var bitmapSource = element.ConvertToBitmapSource(dpiX, dpiY);
			// esegue il salvataggio
			var encoder = new JpegBitmapEncoder();
			encoder.QualityLevel = 90;
			encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
			using (Stream stream = File.Create(path)) {
				encoder.Save(stream);
			}
		}


	}

}
