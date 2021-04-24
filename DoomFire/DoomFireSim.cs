using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace DoomFire {
	public class DoomFireSim: INotifyPropertyChanged {

		#region INotifyPropertyChanged

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName) {
			this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		private void SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null) {
			if (EqualityComparer<T>.Default.Equals(field, value))
				return;

			field = value;
			this.OnPropertyChanged(propertyName);
		}

		#endregion

		public int FireWidth { get; private set; }
		public int FireHeight { get; private set; }

		#region Properties

		private float _fadeSpeedBase;
		public float FadeSpeedBase {
			get => this._fadeSpeedBase;
			set => this.SetField(ref this._fadeSpeedBase, value);
		}

		private float _spread;
		public float Spread {
			get => this._spread;
			set => this.SetField(ref this._spread, value);
		}

		private float _wind;
		public float Wind {
			get => this._wind;
			set => this.SetField(ref this._wind, value);
		}

		private ulong cutoff = ulong.MaxValue;
		private int _cutoffBits;
		public int CutoffBits {
			get => this._cutoffBits;
			set {
				this.SetField(ref this._cutoffBits, value);
				if (value == 0) {
					this.cutoff = 0;
					return;
				}

				this.cutoff = Convert.ToUInt64(new string('1', value), 2);
			}
		}

		private bool oldFireActive = true;
		private bool _fireActive = true;
		public bool FireActive {
			get => this._fireActive;
			set => this.SetField(ref this._fireActive, value);
		}

		#endregion

		private readonly byte[] randomBuffer = new byte[2];
		public byte[] Pixels { get; private set; }

		private readonly Random random;

		public DoomFireSim(int width, int height, float fade = 4.5f, float spread = 1, float wind = 0, int bits = 16) {
			this.FireWidth = width;
			this.FireHeight = height;
			this.FadeSpeedBase = fade;

			this.Spread = spread;
			this.Wind = wind;
			this.CutoffBits = bits;

			this.oldFireActive = this._fireActive;

			this.random = new Random();
		}

		/// <summary>
		/// Sets all pixels to 0 and optionally fills the last line with 1s
		/// </summary>
		public void InitPixels() {
			var w = this.FireWidth;
			var h = this.FireHeight;

			this.Pixels = new byte[w * h];
			var offset = w * (h - 1);

			if (!this._fireActive)
				return;

			for (var i = 0; i < w; i++) {
				this.Pixels[offset + i] = 0xFF;
			}
		}

		public void Resize(int width, int height) {
			this.FireWidth = width;
			this.FireHeight = height;

			this.InitPixels();
		}

		private float GetRandomFloat() {
			this.random.NextBytes(this.randomBuffer);
			return (BitConverter.ToUInt16(this.randomBuffer, 0) & this.cutoff) / (float)this.cutoff;
		}

		private int FixedMod(int x, int m) {
			return (x % m + m) % m;
		}

		public void DoFire() {
			if (this.oldFireActive != this._fireActive) {
				var newValue = this._fireActive ? byte.MaxValue : byte.MinValue;
				var offset = this.FireWidth * (this.FireHeight - 1);
				for (var i = 0; i < this.FireWidth; i++) {
					this.Pixels[offset + i] = newValue;
				}

				this.oldFireActive = this._fireActive;
			}

			for (var y = 1; y < this.FireHeight; y++) {
				var row = y * this.FireWidth;
				var nextRow = (y-1) * this.FireWidth;

				for (var x = 0; x < this.FireWidth; x++) {
					this.SpreadFire(x, row, nextRow);
				}
			}
		}

		private void SpreadFire(int x, int row, int nextRow) {
			var idx = row + x;
			var pixel = this.Pixels[idx];

			var rnd = this.GetRandomFloat();
			var randomRemapped = (rnd - 0.5f) * 2; // 0.0 - 1.0 remapped to -1.0 - 1.0
			var randomSpread = randomRemapped * this._spread;

			var newX = (int)Math.Round(x + randomSpread + this._wind);
			newX = this.FixedMod(newX, this.FireWidth);

			var nextIdx = nextRow + newX;

			if (pixel == 0) {
				this.Pixels[nextIdx] = 0;
				return;
			}

			this.Pixels[nextIdx] = (byte)Math.Max(0, Math.Round(pixel - rnd * this.FadeSpeedBase));
		}
	}
}
