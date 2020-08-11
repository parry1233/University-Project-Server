using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CCU2._0_Server
{
	/// <summary>
	/// LoadWindow.xaml 的互動邏輯
	/// </summary>
	public partial class LoadWindow : Window
	{
		public LoadWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			
		}

		private void Window_ContentRendered(object sender, EventArgs e)
		{
			/*BackgroundWorker worker = new BackgroundWorker();
			worker.WorkerReportsProgress = true;
			worker.DoWork += worker_DoWork;
			worker.ProgressChanged += worker_ProgressChanged;

			worker.RunWorkerAsync();*/
		}

		private void ConnectProgress_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			/*if(ConnectProgress.Value>=99)
			{
				this.Close();
			}*/
		}

		/*void worker_DoWork(object sender, DoWorkEventArgs e)
		{
			for (int i = 0; i < 100; i++)
			{
				(sender as BackgroundWorker).ReportProgress(i);
				Thread.Sleep(100);
			}
		}

		void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			ConnectProgress.Value = e.ProgressPercentage;
		}*/
	}
}
