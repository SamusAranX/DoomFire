using System;
using System.Drawing;

namespace DoomFire {

	public static class Extensions {

		public static Color Multiply(this Color orig, float factor) {
			var r = (int)Math.Round(orig.R * factor);
			var g = (int)Math.Round(orig.G * factor);
			var b = (int)Math.Round(orig.B * factor);

			return Color.FromArgb(r, g, b);
		}

		public static int Remap(this int value, int from1, int to1, int from2, int to2) {
			return (int)Math.Round(((float)value).Remap(from1, to1, from2, to2));
		}

		public static float Remap(this float value, float from1, float to1, float from2, float to2) {
			return ((value - from1) / (to1 - from1) * (to2 - from2)) + from2;
		}

		public static double Remap(this double value, double from1, double to1, double from2, double to2) {
			return ((value - from1) / (to1 - from1) * (to2 - from2)) + from2;
		}

	}

}
