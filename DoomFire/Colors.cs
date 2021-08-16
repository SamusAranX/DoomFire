using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;

namespace DoomFire {

	public class Colors {

		public static Color ColorTemperatureToRGB(float kelvin) {
			var rgb = new float[3];

			if (kelvin >= 12000) {
				rgb[0] = 0.826270103f;
				rgb[1] = 0.994478524f;
				rgb[2] = 1.56626022f;
			} else if (kelvin < 965) {
				rgb[0] = 4.70366907f;
				rgb[1] = 0.0f;
				rgb[2] = 0.0f;
			} else {
				var i = kelvin >= 6365.0f ? 5 :
					kelvin >= 3315.0f ? 4 :
					kelvin >= 1902.0f ? 3 :
					kelvin >= 1449.0f ? 2 :
					kelvin >= 1167.0f ? 1 : 0;

				var r = TEMPS_R[i];
				var g = TEMPS_G[i];
				var b = TEMPS_B[i];

				var tempInv = 1.0f / kelvin;
				rgb[0] = (r[0] * tempInv) + (r[1] * kelvin) + r[2];
				rgb[1] = (g[0] * tempInv) + (g[1] * kelvin) + g[2];
				rgb[2] = (((((b[0] * kelvin) + b[1]) * kelvin) + b[2]) * kelvin) + b[3];
			}

			var rNew = (int)Math.Min(Math.Round(rgb[0] * 255), 255);
			var gNew = (int)Math.Min(Math.Round(rgb[1] * 255), 255);
			var bNew = (int)Math.Min(Math.Round(rgb[2] * 255), 255);

			return Color.FromArgb(rNew, gNew, bNew);
		}

		public static Color[] PopulatePalette(int size) {
			var fadeToBlack = size / 3; // the first x palette entries fade in from black
			var fadeToWhite = size - (int)Math.Round(size / 12f);

			var palette = new Color[size];

			Debug.WriteLine(fadeToBlack);
			Debug.WriteLine(fadeToWhite);

			for (var i = fadeToBlack; i <= fadeToWhite; i++) {
				var factor = ((float)i - fadeToBlack) / (fadeToWhite - fadeToBlack);
				palette[i] = ColorTemperatureToRGB(factor.Remap(0, 1, RED_K, YELLOW_K));
			}

			for (var i = fadeToWhite; i < size; i++) {
				var factor = ((float)i - fadeToWhite) / (size - fadeToWhite);
				palette[i] = ColorTemperatureToRGB(factor.Remap(0, 1, YELLOW_K, WHITE_K));
			}

			for (var i = 1; i < fadeToBlack; i++) {
				var factor = (float)i / fadeToBlack;
				palette[i] = palette[fadeToBlack].Multiply(factor);
			}

			palette[0] = Color.Black;
			palette[size - 1] = Color.White;

			return palette.ToArray();
		}

		#region Constants
		
		private static readonly float[][] TEMPS_R = {
			new[] { 2.52432244e+03f, -1.06185848e-03f, 3.11067539e+00f }, new[] { 3.37763626e+03f, -4.34581697e-04f, 1.64843306e+00f }, new[] { 4.10671449e+03f, -8.61949938e-05f, 6.41423749e-01f },
			new[] { 4.66849800e+03f, 2.85655028e-05f, 1.29075375e-01f }, new[] { 4.60124770e+03f, 2.89727618e-05f, 1.48001316e-01f }, new[] { 3.78765709e+03f, 9.36026367e-06f, 3.98995841e-01f }
		};

		private static readonly float[][] TEMPS_G = {
			new[] { -7.50343014e+02f, 3.15679613e-04f, 4.73464526e-01f }, new[] { -1.00402363e+03f, 1.29189794e-04f, 9.08181524e-01f }, new[] { -1.22075471e+03f, 2.56245413e-05f, 1.20753416e+00f },
			new[] { -1.42546105e+03f, -4.01730887e-05f, 1.44002695e+00f }, new[] { -1.18134453e+03f, -2.18913373e-05f, 1.30656109e+00f }, new[] { -5.00279505e+02f, -4.59745390e-06f, 1.09090465e+00f }
		};

		private static readonly float[][] TEMPS_B = {
			new[] { 0.0f, 0.0f, 0.0f, 0.0f }, new[] { 0.0f, 0.0f, 0.0f, 0.0f }, new[] { 0.0f, 0.0f, 0.0f, 0.0f }, new[] { -2.02524603e-11f, 1.79435860e-07f, -2.60561875e-04f, -1.41761141e-02f },
			new[] { -2.22463426e-13f, -1.55078698e-08f, 3.81675160e-04f, -7.30646033e-01f }, new[] { 6.72595954e-13f, -2.73059993e-08f, 4.24068546e-04f, -7.52204323e-01f }
		};

		private const int RED_K = 965;
		private const int YELLOW_K = 3250;
		private const int WHITE_K = 7500;

		#endregion

	}

}
