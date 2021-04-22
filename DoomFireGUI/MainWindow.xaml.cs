using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

using DColor = System.Drawing.Color;

namespace DoomFireGUI {
	public partial class MainWindow: INotifyPropertyChanged {

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

		private DoomFire.DoomFire _df;
		public DoomFire.DoomFire DF {
			get => this._df;
			private set => this.SetField(ref this._df, value);
		}

		private bool _usePalette = true;
		public bool UsePalette {
			get => this._usePalette;
			set => this.SetField(ref this._usePalette, value);
		}

		private bool _invertPixels;
		public bool InvertPixels {
			get => this._invertPixels;
			set => this.SetField(ref this._invertPixels, value);
		}

		private BitmapSource _dfsource;
		public BitmapSource DFSource {
			get => this._dfsource;
			set => this.SetField(ref this._dfsource, value);
		}

		private bool _dfProcessing;
		public bool DFIsProcessing {
			get => this._dfProcessing;
			set => this.SetField(ref this._dfProcessing, value);
		}

		private bool _dfRunning;
		public bool DFIsRunning {
			get => this._dfRunning;
			set => this.SetField(ref this._dfRunning, value);
		}

		private double _downscaleFactor = 2;
		public double DownscaleFactor {
			get => this._downscaleFactor;
			set => this.SetField(ref this._downscaleFactor, value);
		}

		private int _frameSkip = 1;
		public int FrameSkip {
			get => this._frameSkip;
			set => this.SetField(ref this._frameSkip, value);
		}

		private readonly Storyboard sb;
		private readonly DColor[] firePalette;

		private const int FIRE_WIDTH = 600;
		private const int FIRE_HEIGHT = 360;

		public MainWindow() {
			this.firePalette = DoomFire.Colors.PopulatePalette(256);

			this.InitButton_OnClick(null, null);

			this.InitializeComponent();

			this.sb = (Storyboard)this.FindResource("sb");
			CompositionTarget.Rendering += this.OnRender;
		}

		private int _frameSkipCounter;
		private void SimulationStep() {
			if (this.DFIsProcessing) {
				Debug.WriteLine("missed");
				return;
			}

			if (this._frameSkipCounter <= this._frameSkip) {
				this._frameSkipCounter++;
				return;
			}
			
			this.DFIsProcessing = true;

			this.DF.DoFire();
			this.UpdateImage();
			this._frameSkipCounter = 0;

			this.DFIsProcessing = false;
		}

		private void UpdateImage() {
			// this prevents crashes during app shutdown
			if (Application.Current == null || Application.Current.Dispatcher == null)
				return;

			// when called as a timer callback, this method will execute on a different thread
			// so we'll have to manually re-route this to the main thread

			var w = this.DF.FireWidth;
			var h = this.DF.FireHeight;

			var pixels = this.DF.GetPixels();
			var colorPixels = new byte[pixels.Length * 3];

			// early return in case the DoomFire object is replaced
			if (w * h != pixels.Length)
				return;

			for (var i = 0; i < pixels.Length; i++) {
				var offset = i * 3;
				var pixel = pixels[i];

				if (this.InvertPixels)
					pixel ^= 0xFF;

				if (this.UsePalette) {
					var newCol = this.firePalette[pixel];
					colorPixels[offset + 0] = newCol.B;
					colorPixels[offset + 1] = newCol.G;
					colorPixels[offset + 2] = newCol.R;
				} else {
					colorPixels[offset + 0] = pixel;
					colorPixels[offset + 1] = pixel;
					colorPixels[offset + 2] = pixel;
				}
			}

			Application.Current.Dispatcher.Invoke(() => {
				var bs = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgr24, null, colorPixels, w * 3);
				bs.Freeze();
				this.DFSource = bs;
			});
		}

		public void OnRender(object sender, EventArgs e) {
			if (this.DF == null || !this.DFIsRunning)
				return;
			
			this.SimulationStep();
		}

		private void InitButton_OnClick(object sender, RoutedEventArgs e) {
			var w = (int)Math.Round(FIRE_WIDTH / this._downscaleFactor);
			var h = (int)Math.Round(FIRE_HEIGHT / this._downscaleFactor);

			this.DF = new DoomFire.DoomFire(w, h, 2.5f);
			this.DF.InitPixels();
			this.UpdateImage();
		}
		
		private void StartButton_OnClick(object sender, RoutedEventArgs e) {
			if (this.DFIsRunning)
				return;

			this.ResizeMode = ResizeMode.NoResize;
			this.DFIsRunning = true;
			this.sb.Begin();
		}

		private void PauseButton_OnClick(object sender, RoutedEventArgs e) {
			this.ResizeMode = ResizeMode.CanResizeWithGrip;
			this.sb.Stop();
			this.DFIsRunning = false;
		}
	}
}
