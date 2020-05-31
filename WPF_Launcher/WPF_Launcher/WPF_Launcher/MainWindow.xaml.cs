using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Reflection;
using System.Diagnostics;

namespace WPF_Launcher
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer dispatcherTimer;
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                Process proc = new Process();
                proc.StartInfo.UseShellExecute = false; //既知のため不要
                proc.StartInfo.CreateNoWindow = false;
                proc.StartInfo.ErrorDialog = false;
                proc.StartInfo.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/UI/OredayoUI.exe";
                proc.StartInfo.Arguments = "";
                proc.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/UI/";
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                proc.Start();

                Process proc2 = new Process();
                proc2.StartInfo.UseShellExecute = false; //既知のため不要
                proc2.StartInfo.CreateNoWindow = false;
                proc2.StartInfo.ErrorDialog = false;
                proc2.StartInfo.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Environment/Oredayo.exe";
                proc2.StartInfo.Arguments = "";
                proc2.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/Environment/";
                proc2.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                proc2.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Oredayo Launcher", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            //タイマー起動
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 3, 0);
            dispatcherTimer.Tick += new EventHandler(GenericTimer);
            dispatcherTimer.Start();
        }
        private void GenericTimer(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            this.Close();
        }
    }
}
