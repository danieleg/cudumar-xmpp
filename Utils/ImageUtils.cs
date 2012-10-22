using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;

namespace Cudumar.Utils {
/// <summary>
/// Helps to manage thumbnails, resize and image utils
/// </summary>
	public static class ImageUtils {
		public static Size GetResizeSize(Image originalImage, int maxPixels) {
			int originalWidth = originalImage.Width;
			int originalHeight = originalImage.Height;

			double factor;
			if (originalWidth > originalHeight)
				factor = (double)maxPixels / originalWidth;
			else
				factor = (double)maxPixels / originalHeight;

			return new Size((int)(originalWidth * factor), (int)(originalHeight * factor));
		}
		public static MemoryStream Resize(Image originalImage, int maxPixels, ImageFormat format) {
			Size size = GetResizeSize(originalImage, maxPixels);

			using (Bitmap thumbnailBitmap = new Bitmap(size.Width, size.Height)) {
				using (Graphics thumbnailGraph = Graphics.FromImage(thumbnailBitmap)) {
					thumbnailGraph.CompositingQuality = CompositingQuality.HighQuality;
					thumbnailGraph.SmoothingMode = SmoothingMode.HighQuality;
					thumbnailGraph.InterpolationMode = InterpolationMode.HighQualityBicubic;

					Rectangle imageRectangle = new Rectangle(0, 0, size.Width, size.Height);
					thumbnailGraph.DrawImage(originalImage, imageRectangle);

					MemoryStream ms = new MemoryStream();
					EncoderParameters encoderParameters = new EncoderParameters(1);
					encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 100L);

					ImageCodecInfo codec = GetEncoder(format);
					if (codec == null)
						codec = ImageCodecInfo.GetImageDecoders()[0];

					thumbnailBitmap.Save(ms, codec, encoderParameters);
					thumbnailGraph.Dispose();
					thumbnailBitmap.Dispose();
					originalImage.Dispose();

					return ms;
				}
			}
		}
		private static ImageCodecInfo GetEncoder(ImageFormat format) {
			ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
			foreach (ImageCodecInfo codec in codecs) {
				if (codec.FormatID == format.Guid)
					return codec;
			}
			return null;
		}
	}
}
