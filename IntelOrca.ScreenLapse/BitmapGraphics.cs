using System;
using System.Drawing;

namespace IntelOrca.ScreenLapse
{
	internal class BitmapGraphics : IDisposable
	{
		private readonly Bitmap _bitmap;
		private readonly Graphics _graphics;

		public Bitmap Bitmap { get { return _bitmap; } }
		public Graphics Graphics { get { return _graphics; } }

		public BitmapGraphics(Bitmap bitmap)
		{
			_bitmap = bitmap;
			_graphics = Graphics.FromImage(bitmap);
		}

		public BitmapGraphics(Bitmap bitmap, Graphics graphics)
		{
			_bitmap = bitmap;
			_graphics = graphics;
		}

		public void Dispose()
		{
			_graphics.Dispose();
			_bitmap.Dispose();
		}

		public override string ToString()
		{
			return base.ToString() + "(" + _bitmap.ToString() + ")";
		}
	}
}
