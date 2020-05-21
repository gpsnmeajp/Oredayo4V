/* Oredayo

MIT License

Copyright (c) 2020 gpsnmeajp

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
 */
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
using akr.WPF.Controls;

namespace WPF_UI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MemoryMappedFileClient client;


        public MainWindow()
        {
            InitializeComponent();
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client = new MemoryMappedFileClient();
            client.ReceivedEvent += Client_Received;
            client.Start("Oredayo_UI_Connection");
            /*
                        await client.SendCommandAsync(new PipeCommands.SendMessage { Message = "TestFromWPF" });
                        await client.SendCommandWaitAsync(new PipeCommands.GetCurrentPosition(), d =>
                        {
                            var ret = (PipeCommands.ReturnCurrentPosition)d;
                            Dispatcher.Invoke(() => {
                                //UIスレッド
                            });
                        });
                        */
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            client.Stop();
        }


        private void Client_Received(object sender, DataReceivedEventArgs e)
        {
            if (e.CommandType == typeof(PipeCommands.SendMessage))
            {
                var d = (PipeCommands.SendMessage)e.Data;
                MessageBox.Show($"[Client]ReceiveFromServer:{d.Message}");
            }
        }
        private async void ColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (client != null)
            {
                Color? d = ((ColorPicker)sender).SelectedColor;
                if (d.HasValue)
                {
                    Color c = d.Value;
                    await client?.SendCommandAsync(new PipeCommands.BackgrounColor { r = c.R, g = c.G, b = c.B });
                }
            }
        }

        private async void VRMLoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                Console.WriteLine("VRM Load");
                await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text });
            }
        }

        private async void CameraSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (client != null) {
                await client.SendCommandAsync(new PipeCommands.CameraPos
                {
                    rotate = (float)CameraRotateSlider.Value,
                    zoom = (float)CameraZoomSlider.Value,
                    height = (float)CameraHeightSlider.Value,
                });
            }
        }

        private void CameraRotateResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraRotateSlider.Value = 180f;
        }

        private void CameraZoomResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraZoomSlider.Value = 30f;
        }

        private void CameraHeightResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraHeightSlider.Value = 1.4f;
        }
    }
}
