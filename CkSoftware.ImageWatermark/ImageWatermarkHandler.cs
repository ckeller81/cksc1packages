﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Caching;
using CkSoftware.ImageWatermark.DataTypes;
using Composite.Core.WebClient;
using Composite.Data;
using Composite.Data.Plugins.DataProvider.Streams;
using Composite.Data.Types;

namespace CkSoftware.ImageWatermark
{
	public class ImageWatermarkModule : IHttpModule
	{
		private const string WatermarkConfigKey = "Watermark_Config";
		private static readonly TimeSpan CacheExpirationTimeSpan = new TimeSpan(1, 0, 0, 0);

		public void Init(HttpApplication context)
		{
			context.BeginRequest += ContextOnBeginRequest;
		}

		public void Dispose()
		{
		}

		private static ImageFormat GetImageFormat(string contentType)
		{
			switch (contentType.ToLower())
			{
				case "image/bmp":
					return ImageFormat.Bmp;
				case "image/gif":
					return ImageFormat.Gif;
				case "image/jpg":
				case "image/jpeg":
					return ImageFormat.Jpeg;
				case "image/png":
					return ImageFormat.Png;
				default:
					return null;
			}
		}

		protected byte[] WatermarkImage(Image image, string contentType)
		{
			byte[] imageBytes;

			Graphics graphic;
			if (image.PixelFormat != PixelFormat.Indexed && image.PixelFormat != PixelFormat.Format8bppIndexed &&
			    image.PixelFormat != PixelFormat.Format4bppIndexed && image.PixelFormat != PixelFormat.Format1bppIndexed)
			{
				// Graphic is not a Indexed (GIF) image
				graphic = Graphics.FromImage(image);
			}
			else
			{
				// Cannot create a graphics object from an indexed (GIF) image. So we're going to copy the image into a new bitmap so we can work with it.
				var indexedImage = new Bitmap(image);
				graphic = Graphics.FromImage(indexedImage);

				// Draw the contents of the original bitmap onto the new bitmap. 
				graphic.DrawImage(image, 0, 0, image.Width, image.Height);
				image = indexedImage;
			}
			graphic.SmoothingMode = SmoothingMode.HighQuality;

			DrawWatermark(graphic);

			using (var memoryStream = new MemoryStream())
			{
				image.Save(memoryStream, GetImageFormat(contentType));
				imageBytes = memoryStream.ToArray();
			}

			return imageBytes;
		}

		private void DrawWatermark(Graphics graphic)
		{
			HttpContext context = HttpContext.Current;
			if (!context.Items.Contains(WatermarkConfigKey))
			{
				return;
			}

			var config = (IWatermarkConfiguration) HttpContext.Current.Items[WatermarkConfigKey];

			if (!string.IsNullOrEmpty(config.WatermarkText) && config.FontSize > 0)
			{
				WriteWatermarkTextToImage(config, graphic);
			}

			if (!string.IsNullOrEmpty(config.WatermarkImageFilePath))
			{
				WriteWatermarkImageToImage(config, graphic);
			}
		}

		private void WriteWatermarkImageToImage(IWatermarkConfiguration config, Graphics graphic)
		{
			IMediaFile originalMediaFile = MediaUrlHelper.GetFileFromQueryString(HttpContext.Current.Request.QueryString);

			IMediaFile imageFile;
			using (var conn = new DataConnection())
			{
				imageFile = conn.Get<IMediaFile>().SingleOrDefault(i => i.KeyPath == config.WatermarkImageFilePath);
			}

			Image watermarkImage = Image.FromStream(imageFile.GetReadStream());
			IEnumerable<PointF> positions = GetAllTextPositionByConfigFontAndImage(config, watermarkImage.Size, graphic);

			Size originalImageSize = GetImageSizeFromCacheOrFile(originalMediaFile);
			float widthRatio = graphic.VisibleClipBounds.Width/originalImageSize.Width;
			float heightRatio = graphic.VisibleClipBounds.Height/originalImageSize.Height;
			var watermarkSize = new SizeF(watermarkImage.Width*widthRatio, watermarkImage.Height*heightRatio);

			foreach (PointF position in positions)
			{
				graphic.DrawImage(watermarkImage, position.X, position.Y, watermarkSize.Width, watermarkSize.Height);
			}
		}

		/// <summary>
		/// Thanks to C1 ImageResizer.cs.
		/// </summary>
		/// <returns></returns>
		private Size GetImageSizeFromCacheOrFile(IMediaFile mediaFile)
		{
			string imageKey = mediaFile.CompositePath;
			string imageSizeCacheKey = "ShowMedia.ashx image size " + imageKey;
			var imageSize = HttpRuntime.Cache.Get(imageSizeCacheKey) as Size?;

			if (imageSize == null)
			{
				using (Stream fileStream = mediaFile.GetReadStream())
				{
					Size calculatedSize;
					if (!ImageSizeReader.TryGetSize(fileStream, out calculatedSize))
					{
						using (Stream manualFileStream = mediaFile.GetReadStream())
						{
							using (var bitmap = new Bitmap(manualFileStream))
							{
								calculatedSize = new Size {Width = bitmap.Width, Height = bitmap.Height};
							}
						}
					}

					imageSize = calculatedSize;

					bool isNativeProvider = mediaFile is FileSystemFileBase;

					// We can provider cache dependency only for the native media provider
					CacheDependency cacheDependency = isNativeProvider
						? new CacheDependency((mediaFile as FileSystemFileBase).SystemPath)
						: null;

					HttpRuntime.Cache.Add(imageSizeCacheKey, imageSize, cacheDependency, DateTime.MaxValue, CacheExpirationTimeSpan,
						CacheItemPriority.Normal, null);
				}
			}

			return imageSize.Value;
		}

		private void WriteWatermarkTextToImage(IWatermarkConfiguration config, Graphics graphic)
		{
			var watermarkFont = new Font("Arial", config.FontSize);
			var brush = new SolidBrush(Color.FromArgb(80, Color.White));
			SizeF textSize = graphic.MeasureString(config.WatermarkText, watermarkFont);

			IEnumerable<PointF> positions = GetAllTextPositionByConfigFontAndImage(config, textSize, graphic);

			foreach (PointF position in positions)
			{
				graphic.DrawString(config.WatermarkText, watermarkFont, brush, position);
			}
		}

		private IEnumerable<PointF> GetAllTextPositionByConfigFontAndImage(IWatermarkConfiguration config, SizeF watermarkSize, Graphics graphic)
		{
			if (config.ShowOnTopLeft)
			{
				yield return new PointF(0, 0);
			}

			if (config.ShowOnTopRight)
			{
				yield return new PointF(graphic.VisibleClipBounds.Width - watermarkSize.Width, 0);
			}

			if (config.ShowOnBottmRight)
			{
				yield return
					new PointF(graphic.VisibleClipBounds.Width - watermarkSize.Width,
						graphic.VisibleClipBounds.Height - watermarkSize.Height);
			}

			if (config.ShowOnBottmLeft)
			{
				yield return new PointF(0, graphic.VisibleClipBounds.Height - watermarkSize.Height);
			}
		}

		private void ContextOnBeginRequest(object sender, EventArgs eventArgs)
		{
			var application = (HttpApplication) sender;
			HttpContext context = application.Context;

			if (context.Request.Url.PathAndQuery.Contains("ShowMedia.ashx"))
			{
				IWatermarkConfiguration watermarkConfig = GetWatermarkConfigOrDefault(context.Request.QueryString);

				if (watermarkConfig != null && IsWatermarkEnabled(watermarkConfig, context.Request.Url))
				{
					var filter = new ResponseFilterStream(context.Response.Filter);
					filter.TransformStream += AddWatermarkToStream;

					context.Response.Filter = filter;
					context.Items.Add(WatermarkConfigKey, watermarkConfig);
				}
			}
		}

		private bool IsWatermarkEnabled(IWatermarkConfiguration watermarkConfig, Uri requestUrl)
		{
			bool haveIgnoreWatermarkQueryString = requestUrl.Query.IndexOf("wm=false", StringComparison.OrdinalIgnoreCase) != -1;
			return !watermarkConfig.EnableIgnoreWatermarkQueryString ||
			       (watermarkConfig.EnableIgnoreWatermarkQueryString && !haveIgnoreWatermarkQueryString);
		}

		private IWatermarkConfiguration GetWatermarkConfigOrDefault(NameValueCollection queryString)
		{
			try
			{
				IMediaFile mediaFile = MediaUrlHelper.GetFileFromQueryString(queryString);

				using (var connection = new DataConnection())
				{
					IQueryable<IWatermarkConfiguration> configs = connection.Get<IWatermarkConfiguration>();

					foreach (IWatermarkConfiguration config in configs)
					{
						IMediaFileFolder mediaFolder =
							connection.Get<IMediaFileFolder>().SingleOrDefault(mf => mf.KeyPath == config.TargetMediaFolderPath);
						if (mediaFolder != null && mediaFile.CompositePath.StartsWith(mediaFolder.CompositePath))
						{
							return config;
						}
					}
				}
			}
				// ReSharper disable once EmptyGeneralCatchClause
			catch (Exception)
			{
			}

			return null;
		}

		private MemoryStream AddWatermarkToStream(MemoryStream memoryStream)
		{
			HttpResponse response = HttpContext.Current.Response;

			byte[] imageData = memoryStream.ToArray();
			var inputStream = new MemoryStream(imageData);

			Image image = Image.FromStream(inputStream);
			byte[] data = WatermarkImage(image, response.ContentType);

			var outputStream = new MemoryStream(data.Length);
			outputStream.Write(data, 0, data.Length);

			return outputStream;
		}
	}
}