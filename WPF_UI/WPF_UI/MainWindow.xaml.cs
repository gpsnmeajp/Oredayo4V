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
using UnityMemoryMappedFile;

namespace WPF_UI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MemoryMappedFileServer server;
        private MemoryMappedFileClient client;

        public MainWindow()
        {
            InitializeComponent();
            Init();
        }
        private async void Init() {
            server = new MemoryMappedFileServer();
            server.Start("SamplePipeName");

            client = new MemoryMappedFileClient();
            client.ReceivedEvent += Client_Received;
            client.Start("SamplePipeName");

            await client.SendCommandAsync(new PipeCommands.SendMessage { Message = "TestFromWPF" });
            await client.SendCommandWaitAsync(new PipeCommands.GetCurrentPosition(), d =>
            {
                var ret = (PipeCommands.ReturnCurrentPosition)d;
                Dispatcher.Invoke(() => {
                    //UIスレッド
                });
            });
        }

        private void Client_Received(object sender, DataReceivedEventArgs e)
        {
            if (e.CommandType == typeof(PipeCommands.SendMessage))
            {
                var d = (PipeCommands.SendMessage)e.Data;
                MessageBox.Show($"[Client]ReceiveFromServer:{d.Message}");
            }
        }

    }
}
