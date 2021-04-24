using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using DoomFire;
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

		private DoomFireSim _df;
		public DoomFireSim DF {
			get => this._df;
			private set => this.SetField(ref this._df, value);
		}

		private bool _usePalette = true;
		public bool UsePalette {
			get => this._usePalette;
			set => this.SetField(ref this._usePalette, value);
		}

		private bool _useFiltering = false;
		public bool UseFiltering {
			get => this._useFiltering;
			set => this.SetField(ref this._useFiltering, value);
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

		private float _targetFrameRate = 30;
		public float TargetFrameRate {
			get => this._targetFrameRate;
			set => this.SetField(ref this._targetFrameRate, value);
		}

		private bool _frameMissed;
		public bool FrameMissed {
			get => this._frameMissed;
			set => this.SetField(ref this._frameMissed, value);
		}

		private int _actualFrameRate;
		public int ActualFrameRate {
			get => this._actualFrameRate;
			set => this.SetField(ref this._actualFrameRate, value);
		}

		private readonly DColor[] firePalette;

		private Thread simThread;

		public MainWindow() {
			this.firePalette = DoomFire.Colors.PopulatePalette(256);

			this.InitializeComponent();
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
			this.InitButton_OnClick(null, null);
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
			this.DFIsRunning = false;
		}

		private void InitButton_OnClick(object sender, RoutedEventArgs e) {
			var borderWidth = (int)Math.Round(this.DFBorder.ActualWidth - this.DFBorder.BorderThickness.Left - this.DFBorder.BorderThickness.Right);
			var borderHeight = (int)Math.Round(this.DFBorder.ActualHeight - this.DFBorder.BorderThickness.Top - this.DFBorder.BorderThickness.Bottom);

			var w = (int)Math.Round(borderWidth / this._downscaleFactor);
			var h = (int)Math.Round(borderHeight / this._downscaleFactor);

			if (this.DF == null) {
				this.DF = new DoomFireSim(w, h);
				this.DF.InitPixels();
			} else
				this.DF.Resize(w, h);
				
			this.UpdateImage();
		}

		private void StartButton_OnClick(object sender, RoutedEventArgs e) {
			if (this.DFIsRunning)
				return;

			this.ResizeMode = ResizeMode.NoResize;
			this.DFIsRunning = true;

			this.simThread = new Thread(this.ThreadLoop);
			this.simThread.Start();
		}

		private void PauseButton_OnClick(object sender, RoutedEventArgs e) {
			this.DFIsRunning = false;
			this.ResizeMode = ResizeMode.CanResizeWithGrip;
		}

		private void ThreadLoop() {
			var sw = new Stopwatch();
			sw.Start();

			while (this.DF != null && this.DFIsRunning) {
				var curTime = sw.ElapsedMilliseconds;
				this.SimulationStep();
				this.UpdateImage();

				var targetSleep = 1000 / this._targetFrameRate;

				var frameElapsed = (int)(sw.ElapsedMilliseconds - curTime);
				var sleepElapsed = (int)Math.Round(targetSleep - frameElapsed);

				this.ActualFrameRate = (int)Math.Min(1000f / frameElapsed, this._targetFrameRate);

				if (frameElapsed > targetSleep) {
					this.FrameMissed = true;
					Thread.Sleep((int)Math.Round(targetSleep));
				} else {
					this.FrameMissed = false;
					Thread.Sleep(sleepElapsed);
				}
			}

			sw.Stop();
		}
		
		private void SimulationStep() {
			if (this.DFIsProcessing) {
				return;
			}
			
			this.DFIsProcessing = true;

			this.DF.DoFire();

			this.DFIsProcessing = false;
		}

		private void UpdateImage() {
			var w = this.DF.FireWidth;
			var h = this.DF.FireHeight;

			var pixels = this.DF.Pixels;
			var colorPixels = new byte[pixels.Length * 3];

			// early return in case the DoomFireSim object is replaced
			if (w * h != pixels.Length)
				return;

			for (var i = 0; i < pixels.Length; i++) {
				var offset = i * 3;
				var pixel = pixels[i];

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

			// this prevents most crashes during app shutdown
			if (Application.Current == null || Application.Current.Dispatcher == null)
				return;

			// this method might execute on a different thread
			// so we'll have to manually re-route this to the main thread
			Application.Current.Dispatcher.Invoke(() => {
				var bs = BitmapSource.Create(w, h, 96, 96, PixelFormats.Bgr24, null, colorPixels, w * 3);
				bs.Freeze();
				this.DFSource = bs;
			});
		}
	}
}
