using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using DoomFire;

using TrueColorConsole;

namespace DoomFireTest {
	class Program {
		static void Main(string[] args) {
			VTConsole.Enable();

			var palette = Colors.PopulatePalette(256);

			//for (int k = 900; k <= 6900; k += 50) {
			//	var col = Colors.ColorTemperatureToRGB(k);

			//	VTConsole.SetColorBackground(col);
			//	Console.Write("       ");
			//	VTConsole.SoftReset();
			//	Console.Write(" ");
			//	Console.WriteLine($"{k:D5}: {col}");
			//}

			for (int i = 0; i < palette.Length; i++) {
				var col = palette[i];

				VTConsole.SetColorBackground(col);
				Console.Write("       ");
				VTConsole.SoftReset();
				Console.Write(" ");
				Console.WriteLine($"{i:D3}: {col}");
			}

			if (false) {
				var width = Console.BufferWidth / 2;
				var height = 30;

				var df = new DoomFire.DoomFire(width, height);
				df.InitPixels();
				var builder = new StringBuilder();

				Console.Title = $"Doom Fire ({width}×{height})";

				while (!Console.KeyAvailable) {
					df.DoFire();

					builder.Clear();

					foreach (var pixel in df.GetPixels()) {
						var col = palette[pixel];
						var colStr = VTConsole.GetColorBackgroundString(col.R, col.G, col.B);

						builder.Append(colStr);
						builder.Append("  ");
					}

					var strBytes = Encoding.ASCII.GetBytes(builder.ToString());
					VTConsole.WriteFast(strBytes);

					Thread.Sleep(1000 / 30);
				}

				using var bitmap = new Bitmap(width, height, PixelFormat.Format24bppRgb);
				using var newBitmap = new Bitmap(width * 4, height * 4, PixelFormat.Format24bppRgb);
				using var g = Graphics.FromImage(newBitmap);
				g.InterpolationMode = InterpolationMode.NearestNeighbor;

				var bd = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);

				var curPixels = df.GetPixels();
				var newPixels = new byte[width * height * 3];
				for (var i = 0; i < curPixels.Length; i++) {
					var col = palette[curPixels[i]];

					var offset = i * 3;
					newPixels[offset + 0] = col.B;
					newPixels[offset + 1] = col.G;
					newPixels[offset + 2] = col.R;
				}

				Marshal.Copy(newPixels, 0, bd.Scan0, newPixels.Length);

				bitmap.UnlockBits(bd);

				g.DrawImage(bitmap, new Rectangle(1, 1, width * 4, height * 4), 0, 0, width, height, GraphicsUnit.Pixel);

				newBitmap.Save(@"C:\Users\hallo\Desktop\doomfire.png", ImageFormat.Png);
			}

			Console.ReadKey();
		}
	}
}
