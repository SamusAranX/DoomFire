using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using DoomFire;

using Colors = DoomFire.Colors;
using DColor = System.Drawing.Color;

namespace DoomFireGUI {

	public partial class MainWindow : INotifyPropertyChanged {

		private readonly DColor[] _firePalette;

		private int _actualFrameRate;

		private DoomFireSim _df;

		private bool _dfProcessing;

		private bool _dfRunning;

		private BitmapSource _dfsource;

		private double _downscaleFactor = 2;

		private bool _frameMissed;
		private Task _simulationTask;

		private float _targetFrameRate = 30;

		private CancellationTokenSource _tokenSource = new CancellationTokenSource();

		private bool _useFiltering;

		private bool _usePalette = true;

		public MainWindow() {
			this._firePalette = Colors.PopulatePalette(256);

			this.InitializeComponent();
		}

		public DoomFireSim DF {
			get => this._df;
			private set => this.SetField(ref this._df, value);
		}

		public bool UsePalette {
			get => this._usePalette;
			set => this.SetField(ref this._usePalette, value);
		}

		public bool UseFiltering {
			get => this._useFiltering;
			set => this.SetField(ref this._useFiltering, value);
		}

		public BitmapSource DFSource {
			get => this._dfsource;
			set => this.SetField(ref this._dfsource, value);
		}

		public bool DFIsProcessing {
			get => this._dfProcessing;
			set => this.SetField(ref this._dfProcessing, value);
		}

		public bool DFIsRunning {
			get => this._dfRunning;
			set => this.SetField(ref this._dfRunning, value);
		}

		public double DownscaleFactor {
			get => this._downscaleFactor;
			set => this.SetField(ref this._downscaleFactor, value);
		}

		public float TargetFrameRate {
			get => this._targetFrameRate;
			set => this.SetField(ref this._targetFrameRate, value);
		}

		public bool FrameMissed {
			get => this._frameMissed;
			set => this.SetField(ref this._frameMissed, value);
		}

		public int ActualFrameRate {
			get => this._actualFrameRate;
			set => this.SetField(ref this._actualFrameRate, value);
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e) {
			this.InitButton_OnClick(null, null);
		}

		private void MainWindow_OnClosing(object sender, CancelEventArgs e) {
			this._tokenSource.Cancel();
		}

		private void InitButton_OnClick(object sender, RoutedEventArgs e) {
			var borderWidth = (int)Math.Round(this.DFBorder.ActualWidth - this.DFBorder.BorderThickness.Left - this.DFBorder.BorderThickness.Right);
			var borderHeight = (int)Math.Round(this.DFBorder.ActualHeight - this.DFBorder.BorderThickness.Top - this.DFBorder.BorderThickness.Bottom);

			var w = (int)Math.Round(borderWidth / this._downscaleFactor);
			var h = (int)Math.Round(borderHeight / this._downscaleFactor);

			var restartFire = false;

			if (this.DFIsRunning) {
				this.StartButton_OnClick(null, null);
				restartFire = true;
			}

			if (this.DF == null) {
				this.DF = new DoomFireSim(w, h);
				this.DF.InitPixels();
			} else
				this.DF.Resize(w, h);

			this.UpdateImage();

			if (restartFire) this.StartButton_OnClick(null, null);
		}

		private void StartButton_OnClick(object sender, RoutedEventArgs e) {
			if (this.DFIsRunning) {
				this.ResizeMode = ResizeMode.CanResizeWithGrip;

				this._tokenSource.Cancel();
				this.DFIsRunning = false;
			} else {
				this.ResizeMode = ResizeMode.NoResize;

				this._tokenSource = new CancellationTokenSource();
				this._simulationTask = Task.Run(() => {
					this.ThreadLoop(this._tokenSource.Token);
				});

				this.DFIsRunning = true;
			}
		}

		private void ThreadLoop(CancellationToken token) {
			var sw = new Stopwatch();
			sw.Start();

			while (true) {
				if (token.IsCancellationRequested)
					break;

				var curTime = sw.ElapsedMilliseconds;

				this.SimulationStep();
				this.UpdateImage();

				// another check for good measure
				if (token.IsCancellationRequested)
					break;

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
			Debug.WriteLine("Task completed");
		}

		private void SimulationStep() {
			if (this.DFIsProcessing)
				return;

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
					var newCol = this._firePalette[pixel];
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
