using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;

namespace DoomFire {
	public class DoomFire: INotifyPropertyChanged {

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

		public readonly int FireWidth;
		public readonly int FireHeight;

		#region Properties

		private float _fadeSpeedBase;
		public float FadeSpeedBase {
			get => this._fadeSpeedBase;
			set => this.SetField(ref this._fadeSpeedBase, value);
		}

		private float _spread = 2;
		public float Spread {
			get => this._spread;
			set => this.SetField(ref this._spread, value);
		}

		private float _wind = 0;
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

		#endregion

		private byte[] pixels;
		
		private readonly Random random;
		private readonly RNGCryptoServiceProvider cRandom;

		public DoomFire(int width, int height, float fade = 1.75f) {
			this.FireWidth = width;
			this.FireHeight = height;
			this.FadeSpeedBase = fade;

			this.CutoffBits = 32;

			this.random = new Random();
			this.cRandom = new RNGCryptoServiceProvider();
		}

		/// <summary>
		/// Sets all pixels to 0 and fills the last line with 1s
		/// </summary>
		public void InitPixels() {
			var w = this.FireWidth;
			var h = this.FireHeight;

			this.pixels = new byte[w * h];
			var offset = w * (h - 1);
			for (var i = 0; i < w; i++) {
				this.pixels[offset + i] = 0xFF;
			}
		}
		
		private readonly byte[] randomBuffer = new byte[8];
		private double GetRandomDouble() {
			//this.cRandom.GetBytes(this.randomBuffer);
			this.random.NextBytes(this.randomBuffer);
			
			return (BitConverter.ToUInt64(this.randomBuffer, 0) & this.cutoff) / (double)this.cutoff;
		}
		
		public void DoFire() {
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

			var rnd = this.GetRandomDouble();
			var randomRemapped = (rnd - 0.5) * 2; // 0.0 to 1.0 remapped to -1.0 to 1.0
			var randomSpread = randomRemapped * this._spread;

			var newX = (int)Math.Round(x + randomSpread + this._wind);
			if (newX >= this.FireWidth)
				newX -= this.FireWidth;
			else if (newX < 0)
				newX += this.FireWidth;

			var nextIdx = nextRow + newX;

			var pixel = this.pixels[idx];
			if (pixel == 0) {
				this.pixels[nextIdx] = 0;
				return;
			}

			this.pixels[nextIdx] = (byte)Math.Max(0, Math.Round(pixel - rnd * this.FadeSpeedBase));
		}

		public byte[] GetPixels() {
			return this.pixels;
		}
	}
}
