using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DoomFire {

	public class DoomFireSim : INotifyPropertyChanged {

		private readonly Random _random;

		private readonly byte[] _randomBuffer = new byte[2];

		private ulong _cutoff = ulong.MaxValue;
		private int _cutoffBits;

		private float _fadeSpeedBase;
		private bool _fireActive = true;

		private bool _oldFireActive;

		private float _spread;

		private float _wind;

		public DoomFireSim(int width, int height, float fade = 4.5f, float spread = 1, float wind = 0, int bits = 16) {
			this.FireWidth = width;
			this.FireHeight = height;
			this.FadeSpeedBase = fade;

			this.Spread = spread;
			this.Wind = wind;
			this.CutoffBits = bits;

			this._oldFireActive = this._fireActive;

			this._random = new Random();
		}

		public int FireWidth { get; private set; }
		public int FireHeight { get; private set; }
		public byte[] Pixels { get; private set; }

		public float FadeSpeedBase {
			get => this._fadeSpeedBase;
			set => this.SetField(ref this._fadeSpeedBase, value);
		}

		public float Spread {
			get => this._spread;
			set => this.SetField(ref this._spread, value);
		}

		public float Wind {
			get => this._wind;
			set => this.SetField(ref this._wind, value);
		}

		public int CutoffBits {
			get => this._cutoffBits;
			set {
				this.SetField(ref this._cutoffBits, value);
				if (value == 0) {
					this._cutoff = 0;

					return;
				}

				this._cutoff = Convert.ToUInt64(new string('1', value), 2);
			}
		}

		public bool FireActive {
			get => this._fireActive;
			set => this.SetField(ref this._fireActive, value);
		}

		/// <summary>
		///     Sets all pixels to 0 and optionally fills the last line with 1s
		/// </summary>
		public void InitPixels() {
			var w = this.FireWidth;
			var h = this.FireHeight;

			this.Pixels = new byte[w * h];
			var offset = w * (h - 1);

			if (!this._fireActive)
				return;

			for (var i = 0; i < w; i++) this.Pixels[offset + i] = 0xFF;
		}

		public void Resize(int width, int height) {
			this.FireWidth = width;
			this.FireHeight = height;

			this.InitPixels();
		}

		private float GetRandomFloat() {
			this._random.NextBytes(this._randomBuffer);

			return (BitConverter.ToUInt16(this._randomBuffer, 0) & this._cutoff) / (float)this._cutoff;
		}

		private static int FixedMod(int x, int m) {
			return ((x % m) + m) % m;
		}

		public void DoFire() {
			if (this._oldFireActive != this._fireActive) {
				var newValue = this._fireActive ? byte.MaxValue : byte.MinValue;
				var offset = this.FireWidth * (this.FireHeight - 1);
				for (var i = 0; i < this.FireWidth; i++) this.Pixels[offset + i] = newValue;

				this._oldFireActive = this._fireActive;
			}

			for (var y = 1; y < this.FireHeight; y++) {
				var row = y * this.FireWidth;
				var nextRow = (y - 1) * this.FireWidth;

				for (var x = 0; x < this.FireWidth; x++) this.SpreadFire(x, row, nextRow);
			}
		}

		private void SpreadFire(int x, int row, int nextRow) {
			var pixels = this.Pixels;

			var idx = row + x;
			var pixel = pixels[idx];

			var rnd = this.GetRandomFloat();
			var randomRemapped = (rnd - 0.5f) * 2; // 0.0 - 1.0 remapped to -1.0 - 1.0
			var randomSpread = randomRemapped * this._spread;

			var newX = (int)Math.Round(x + randomSpread + this._wind);
			newX = FixedMod(newX, this.FireWidth);

			var nextIdx = nextRow + newX;

			if (pixel == 0) {
				pixels[nextIdx] = 0;

				return;
			}

			pixels[nextIdx] = (byte)Math.Max(0, Math.Round(pixel - (rnd * this.FadeSpeedBase)));

			this.Pixels = pixels;
		}

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

	}

}
