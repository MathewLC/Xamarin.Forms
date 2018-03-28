using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Android;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.Widget;
using Android.Net;
using Xamarin.Forms.Internals;
using Android.Graphics.Drawables;
using Android.Util;

namespace Xamarin.Forms.Platform.Android
{
	public sealed class FileImageSourceHandler : IImageSourceHandler, IImageViewHandler, IImageSourceHandlerEx
    {
		// This is set to true when run under designer context
		internal static bool DecodeSynchronously {
			get;
			set;
		}

		public async Task<Bitmap> LoadImageAsync(ImageSource imagesource, Context context, CancellationToken cancelationToken = default(CancellationToken))
		{
			string file = ((FileImageSource)imagesource).File;
			Bitmap bitmap;
			if (File.Exists (file))
				bitmap = !DecodeSynchronously ? (await BitmapFactory.DecodeFileAsync (file).ConfigureAwait (false)) : BitmapFactory.DecodeFile (file);
			else
				bitmap = !DecodeSynchronously ? (await context.Resources.GetBitmapAsync (file, context).ConfigureAwait (false)) : context.Resources.GetBitmap (file, context);

			if (bitmap == null)
			{
				Internals.Log.Warning(nameof(FileImageSourceHandler), "Could not find image or image file was invalid: {0}", imagesource);
			}

			return bitmap;
		}

		public async Task<AnimationDrawable> LoadImageAnimationAsync(ImageSource imagesource, Context context, CancellationToken cancelationToken = default(CancellationToken))
		{
			string file = ((FileImageSource)imagesource).File;

			BitmapFactory.Options options = new BitmapFactory.Options();
			options.InJustDecodeBounds = true;

			if (!DecodeSynchronously)
				await BitmapFactory.DecodeResourceAsync(context.Resources, ResourceManager.GetDrawableByName(file), options);
			else
				BitmapFactory.DecodeResource(context.Resources, ResourceManager.GetDrawableByName(file), options);

			var decoder = new AndroidGIFImageParser(context, options.InDensity, options.InTargetDensity);
			using (var stream = context.Resources.OpenRawResource(ResourceManager.GetDrawableByName(file)))
			{
				if (!DecodeSynchronously)
					await decoder.ParseAsync(stream);
				else
					decoder.ParseAsync(stream).Wait();
			}

			return decoder.Animation;
		}

		public Task LoadImageAsync(ImageSource imagesource, ImageView imageView, CancellationToken cancellationToken = default(CancellationToken))
		{
			string file = ((FileImageSource)imagesource).File;
			if (File.Exists(file))
			{
				var uri = Uri.Parse(file);
				if (uri != null)
					imageView.SetImageURI(uri);
				else
					Log.Warning(nameof(FileImageSourceHandler), "Could not find image or image file was invalid: {0}", imagesource);
			}
			else
			{
				var drawable = ResourceManager.GetDrawable(imageView.Context, file);
				if (drawable != null)
					imageView.SetImageDrawable(drawable);
				else
					Log.Warning(nameof(FileImageSourceHandler), "Could not find image or image file was invalid: {0}", imagesource);
			}

			return Task.FromResult(true);
		}
	}
}