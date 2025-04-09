using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.WindowsAPICodePack.Dialogs;
using MidasGtsExporter.ABAQUS;
using MidasGtsExporter.FLAC3D;
using MidasGtsExporter.LSDYNA;

namespace MidasGtsExporter
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			//
			_backgroundWorker = new BackgroundWorker()
			{
				WorkerReportsProgress = true,
			};
			_backgroundWorker.DoWork += _backgroundWorker_DoWork;
			_backgroundWorker.ProgressChanged += _backgroundWorker_ProgressChanged;
			_backgroundWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;

			//
			_stopWatch = new Stopwatch();
			_timer = new DispatcherTimer()
			{
				Interval = TimeSpan.FromSeconds(1),
			};
			_timer.Tick += Timer_Tick;
		}

		private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
		{
			this.Title = $"MidasGtsExporter v{AppInfo.Version} by {AppInfo.Author}";
			this.RadioButtonFlac3D.IsChecked = true;
		}

		#region private fields

		private readonly BackgroundWorker _backgroundWorker;
		private readonly Stopwatch _stopWatch;
		private readonly DispatcherTimer _timer;
		private DateTime _startTime;

		private string _inputFilePath;
		private string _outputFolder;
		private string _outputFileName;
		private OutputFormat _outputFormat;

		#endregion

		#region Control event handlers

		private void ButtonInputFilePath_OnClick(object sender, RoutedEventArgs e)
		{
			var ofd = new CommonOpenFileDialog()
			{
				IsFolderPicker = false,
				DefaultExtension = "fpn",
			};
			ofd.Filters.Add(new CommonFileDialogFilter("Midas FPN File", ".fpn"));
			if (ofd.ShowDialog() != CommonFileDialogResult.Ok)
				return;

			_inputFilePath = ofd.FileName;
			Debug.Assert(IO.IsFileExist(_inputFilePath));
			this.TextBoxInputFilePath.Text = _inputFilePath;

			if (string.IsNullOrWhiteSpace(_outputFolder))
			{
				_outputFolder = Path.GetDirectoryName(_inputFilePath);
				Debug.Assert(IO.IsFolderExist(_outputFolder));
				this.TextBoxOutputFolder.Text = _outputFolder;
			}

			if (CheckBoxSameFileName.IsChecked == true
			    || string.IsNullOrWhiteSpace(_outputFileName))
			{
				_outputFileName = Path.GetFileNameWithoutExtension(_inputFilePath);
				TextBoxOutputFileName.Text = _outputFileName;
			}
		}

		private void ButtonOutputFolder_OnClick(object sender, RoutedEventArgs e)
		{
			var fbd = new CommonOpenFileDialog()
			{
				IsFolderPicker = true,
			};
			if (fbd.ShowDialog() != CommonFileDialogResult.Ok)
				return;

			_outputFolder = fbd.FileName;
			Debug.Assert(IO.IsFolderExist(_outputFolder));
			this.TextBoxOutputFolder.Text = _outputFolder;
		}

		private void TextBoxOutputFileName_OnTextChanged(object sender, TextChangedEventArgs e)
		{
			var newFileName = TextBoxOutputFileName.Text;
			if (string.IsNullOrWhiteSpace(newFileName))
			{
				TextBoxOutputFileName.Text = _outputFileName;
			}
			else
			{
				_outputFileName = newFileName;
			}
		}

		private void CheckBoxSameFileName_OnChecked(object sender, RoutedEventArgs e)
		{
			if (CheckBoxSameFileName.IsChecked == true)
			{
				if (TextBoxOutputFileName.IsReadOnly == false)
				{
					TextBoxOutputFileName.IsReadOnly = true;
					Debug.Assert(IO.IsFileExist(_inputFilePath));
					_outputFileName = Path.GetFileNameWithoutExtension(_inputFilePath);
					TextBoxOutputFileName.Text = _outputFileName;
				}
			}
			else
			{
				TextBoxOutputFileName.IsReadOnly = false;
			}
		}

		private void RadioButtonOutputFormat_OnChecked(object sender, RoutedEventArgs e)
		{
			var lookupTable = new List<ValueTuple<RadioButton, OutputFormat, UserControl>>()
			{
				(RadioButtonFlac3D, OutputFormat.Flac3D, Flac3dOption),
				(RadioButtonAbaqus, OutputFormat.Abaqus, AbaqusOption),
				(RadioButtonLsDyna, OutputFormat.LsDyna, LsDynaOption),
			};
			var selectedItem = lookupTable.First(item => item.Item1.IsChecked == true);
			_outputFormat = selectedItem.Item2;

			foreach (var item in lookupTable)
			{
				if (item.Item3 != null)
					item.Item3.Visibility = Visibility.Collapsed;
			}
			var option = selectedItem.Item3;
			if (option != null)
			{
				option.Visibility = Visibility.Visible;
			}
		}

		private void ButtonOpenOutputFolder_OnClick(object sender, RoutedEventArgs e)
		{
			if (IO.IsFolderExist(_outputFolder))
			{
				Process.Start(_outputFolder);
			}
		}

		private void ButtonConvert_OnClick(object sender, RoutedEventArgs e)
		{
			Debug.Assert(!_backgroundWorker.IsBusy);
			if (!IO.IsFileExist(_inputFilePath))
			{
				MessageBox.Show("请先指定Midas Gts FPN文件路径!", "",
					MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			this.TextBoxOutputMessage.Clear();
			this.ButtonConvert.IsEnabled = false;
			this.ButtonConvert.Content = "正在转换 ……";

			var param = new ConvertParameter()
			{
				InputFilePath = _inputFilePath,
				OutputFolderPath = _outputFolder,
				OutputFileName = _outputFileName,
				OutputFormat = _outputFormat,
			};
			if (_outputFormat == OutputFormat.LsDyna)
			{
				param.Option = LsDynaOption.GetOption();
			}
			else if (_outputFormat == OutputFormat.Flac3D)
			{
				param.Option = Flac3dOption.GetOption();
			}

			_backgroundWorker.RunWorkerAsync(param);

			//
			TextBlockTime.Text = "00:00:00";
			_startTime = DateTime.Now;
			_timer.Start();
		}

		private void Timer_Tick(object sender, EventArgs e)
		{
			UpdateTimer();
		}

		private void UpdateTimer()
		{
			var elapsedTime = DateTime.Now - _startTime;
			TextBlockTime.Text = $"{elapsedTime:hh\:mm\:ss}";
		}

		private void ButtonAbout_OnClick(object sender, RoutedEventArgs e)
		{
			var aboutWindow = new AboutWindow();
			aboutWindow.ShowDialog();
		}

		#endregion

		#region Background worker event handlers

		private void _backgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			var progressMsg = (string) e.UserState;
			this.TextBoxOutputMessage.AppendText(progressMsg + Environment.NewLine);
			this.ProgressBarConvert.Value = e.ProgressPercentage;
		}

		private void _backgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			_timer.Stop();
			UpdateTimer();

			//
			this.ButtonConvert.IsEnabled = true;
			this.ButtonConvert.Content = "开始转换";
			this.TextBoxOutputMessage.ScrollToEnd();
		}

		private void _backgroundWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			var convertParam = (ConvertParameter) e.Argument;

			//
			var reader = new GtsFpnDataReader();
			reader.ProgressChanged += (s, args) =>
			{
				_backgroundWorker.ReportProgress(args.ProgressPercentage, args.UserState);
			};
			reader.ReadFile(convertParam.InputFilePath);

			//
			var outputFilePath = Path.Combine(convertParam.OutputFolderPath,
				convertParam.OutputFileName);
			switch (convertParam.OutputFormat)
			{
				case OutputFormat.Flac3D:
				{
					var option = (Flac3dOption.Option) convertParam.Option;
					var writer = new Flac3dWriter(reader.Nodes, reader.Elements, reader.Groups);
					writer.ProgressChanged += (s, args) =>
					{
						_backgroundWorker.ReportProgress(args.ProgressPercentage, args.UserState);
					};
					writer.WriteFile(outputFilePath, option);
					break;
				}
				case OutputFormat.Abaqus:
				{
					var writer = new AbaqusWriter(reader.Nodes, reader.Elements, reader.Groups);
					writer.ProgressChanged += (s, args) =>
					{
						_backgroundWorker.ReportProgress(args.ProgressPercentage, args.UserState);
					};
					writer.WriteFile(outputFilePath);
					break;
				}
				case OutputFormat.LsDyna:
				{
					var option = (LsDynaOption.Option) convertParam.Option;
					var writer = new LsDynaWriter(reader.Nodes, reader.Elements, reader.Groups);
					writer.ProgressChanged += (s, args) =>
					{
						_backgroundWorker.ReportProgress(args.ProgressPercentage, args.UserState);
					};
					writer.WriteFile(outputFilePath, option);
					break;
				}
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		public class ConvertParameter
		{
			public string InputFilePath { get; set; }
			public string OutputFolderPath { get; set; }
			public string OutputFileName { get; set; }
			public OutputFormat OutputFormat { get; set; }

			public object Option { get; set; }
		}

		public enum OutputFormat
		{
			Flac3D,
			Abaqus,
			Ansys,
			LsDyna,
		}
	}
}