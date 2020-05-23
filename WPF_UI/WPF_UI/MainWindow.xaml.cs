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
using System.Windows.Threading;

namespace WPF_UI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MemoryMappedFileClient client;
        private DispatcherTimer dispatcherTimer;
        private float gamingH = 0f;

        private int UI_KeepAlive = 0;

        public MainWindow()
        {
            InitializeComponent();
        }

        //-----------システム系----------------
        private void Client_Received(object sender, DataReceivedEventArgs e)
        {
            if (e.CommandType == typeof(PipeCommands.Hello))
            {
                //Unity側起動時処理(値送信など)
                Console.WriteLine(">Hello");

            }
            else if (e.CommandType == typeof(PipeCommands.Bye))
            {
                //Unity側終了処理
                Dispatcher.Invoke(() => {
                    //this.Close();
                });
                Console.WriteLine(">Bye");
            }
            else if (e.CommandType == typeof(PipeCommands.LogMessage))
            {
                //ログ受信時処理
                var d = (PipeCommands.LogMessage)e.Data;
                Dispatcher.Invoke(() => {
                    StatusBarText.Text = "[" + d.Type.ToString() + "] " + d.Message;
                    switch (d.Type)
                    {
                        case PipeCommands.LogType.Error:
                            StatusBarText.Foreground = new SolidColorBrush(Color.FromRgb(255, 30, 30));
                            break;
                        case PipeCommands.LogType.Warning:
                            StatusBarText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 30));
                            break;
                        default:
                            StatusBarText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                            break;
                    }
                });
            }
            else if (e.CommandType == typeof(PipeCommands.SendMessage))
            {
                //エラーダイアログ処理
                var d = (PipeCommands.SendMessage)e.Data;
                Dispatcher.Invoke(() => {
                    MessageBox.Show(d.Message, "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            else if (e.CommandType == typeof(PipeCommands.CommunicationStatus))
            {
                //KeepAlive処理
                var d = (PipeCommands.CommunicationStatus)e.Data;
                Dispatcher.Invoke(() => {
                    if (!d.EVMC4U)
                    {
                        InfoEVMC4UStateTextBlock.Background = new SolidColorBrush(Color.FromRgb(200, 0, 0));
                        InfoEVMC4UStateTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        InfoEVMC4UStateTextBlock.Text = "NG";
                    }
                    else
                    {
                        InfoEVMC4UStateTextBlock.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                        InfoEVMC4UStateTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                        InfoEVMC4UStateTextBlock.Text = "OK";
                    }
                });
            }
            else if (e.CommandType == typeof(PipeCommands.KeepAlive))
            {
                UI_KeepAlive = 0;
            }
        }
        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            client = new MemoryMappedFileClient();
            client.ReceivedEvent += Client_Received;
            client.Start("Oredayo_UI_Connection");

            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 32);
            dispatcherTimer.Tick += new EventHandler(GenericTimer);
            dispatcherTimer.Start();

            //起動したことを伝える
            await client.SendCommandAsync(new PipeCommands.Hello { });
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
        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //終了を伝える
            await client.SendCommandAsync(new PipeCommands.Bye { });
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            dispatcherTimer.Stop();
            client.Stop();
        }

        private void GenericTimer(object sender, EventArgs e) {
            if (GamingBackgroundCheckBox.IsChecked.HasValue && GamingBackgroundCheckBox.IsChecked.Value)
            {
                BackgroundColorPicker.SelectedColor = akr.WPF.Utilities.ColorUtilities.HsvToRgb(gamingH, 1, 1);
            }
            if (GamingLightCheckBox.IsChecked.HasValue && GamingLightCheckBox.IsChecked.Value)
            {
                LightColorPicker.SelectedColor = akr.WPF.Utilities.ColorUtilities.HsvToRgb(gamingH, 1, 1);
            }

            gamingH += 10f;
            if (gamingH > 360f) {
                gamingH -= 360f;
            }

            //-------
            UI_KeepAlive++;
            if (UI_KeepAlive > 60)
            {
                InfoUIStateTextBlock.Background = new SolidColorBrush(Color.FromRgb(200, 0, 0));
                InfoUIStateTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                InfoUIStateTextBlock.Text = "NG";
                InfoEVMC4UStateTextBlock.Background = new SolidColorBrush(Color.FromRgb(200, 0, 0));
                InfoEVMC4UStateTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                InfoEVMC4UStateTextBlock.Text = "--";
            }
            else {
                InfoUIStateTextBlock.Background = new SolidColorBrush(Color.FromRgb(0, 255, 0));
                InfoUIStateTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(0,0,0));
                InfoUIStateTextBlock.Text = "OK";
            }
        }

        //-----------基本設定----------------
        //===========VRM読み込み===========
        private async void VRMLoadButton_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text });
            Console.WriteLine("VRMLoadButton_Click");
        }

        private async void VRMLoadFileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = ".vrm";
            dlg.Filter = "VRM file|*.vrm|All file|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                VRMPathTextBox.Text = dlg.FileName;
                await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text });
                Console.WriteLine("VRMLoadFileSelectButton_Click");
            }
        }

        //===========背景読み込み===========
        private async void BackgroundObjectLoadButton_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.LoadBackground { filepath = BackgroundObjectPathTextBox.Text });
            Console.WriteLine("BackgroundObjectLoadButton_Click");
        }

        private async void BackgroundObjectLoadFileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG file|*.png|JPEG file|*.jpg|VRM file|*.vrm|All file|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                BackgroundObjectPathTextBox.Text = dlg.FileName;
                await client.SendCommandAsync(new PipeCommands.LoadBackground { filepath = BackgroundObjectPathTextBox.Text });
                Console.WriteLine("BackgroundObjectLoadFileSelectButton_Click");
            }
        }
        private async void BackgroundObjectRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.RemoveBackground { });
            Console.WriteLine("BackgroundObjectRemoveButton_Click");
        }

        //===========カメラ位置===========
        private async void CameraSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.CameraControl
                {
                    Rx = (float)CameraRotateXSlider.Value,
                    Ry = (float)CameraRotateYSlider.Value,
                    Rz = (float)CameraRotateZSlider.Value,

                    Zoom = (float)CameraValue1Slider.Value,
                    Height = (float)CameraValue2Slider.Value,
                    Fov = (float)CameraValue3Slider.Value,
                });
                Console.WriteLine("CameraSlider");
            }
        }
        private void CameraRotateXResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraRotateXSlider.Value = 180f;
        }

        private void CameraRotateYResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraRotateYSlider.Value = 180f;
        }

        private void CameraRotateZResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraRotateZSlider.Value = 180f;
        }
        private void CameraValue1ResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraValue1Slider.Value = 30f;
        }

        private void CameraValue2ResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraValue2Slider.Value = 1.4f;
        }

        private void CameraValue3ResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraValue3Slider.Value = 1f;
        }


        //===========ライト位置===========
        private async void LightSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (client != null)
            {
                PipeCommands.LightType type = PipeCommands.LightType.Directional;
                if (LightTypeDirectionalRadioButton.IsChecked.Value)
                {
                    type = PipeCommands.LightType.Directional;
                }
                if (LightTypePointRadioButton.IsChecked.Value)
                {
                    type = PipeCommands.LightType.Point;
                }
                if (LightTypeSpotRadioButton.IsChecked.Value)
                {
                    type = PipeCommands.LightType.Spot;
                }

                await client.SendCommandAsync(new PipeCommands.LightControl
                {
                    Rx = (float)LightRotateXSlider.Value,
                    Ry = (float)LightRotateYSlider.Value,
                    Rz = (float)LightRotateZSlider.Value,

                    Distance = (float)LightValue1Slider.Value,
                    Range = (float)LightValue2Slider.Value,
                    SpotAngle = (float)LightValue3Slider.Value,

                    Type = type,
                });
                Console.WriteLine("LightSlider");

            }
        }

        private void LightTypeChanged(object sender, RoutedEventArgs e)
        {
            //ただ転送する(お行儀悪い)
            LightSlider_ValueChanged(null, null);
        }

        private void LightRotateXResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightRotateXSlider.Value = 180f;
        }

        private void LightRotateYResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightRotateYSlider.Value = 180f;
        }

        private void LightRotateZResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightRotateZSlider.Value = 180f;
        }

        private void LightValue1ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightValue1Slider.Value = 30f;
        }

        private void LightValue2ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightValue2Slider.Value = 10f;
        }

        private void LightValue3ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightValue3Slider.Value = 30;
        }


        //===========背景オブジェクト位置===========


        private async void BackgroundSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.BackgroundObjectControl
                {
                    Rx = (float)BackgroundRotateXSlider.Value,
                    Ry = (float)BackgroundRotateYSlider.Value,
                    Rz = (float)BackgroundRotateZSlider.Value,

                    Px = (float)BackgroundValue1Slider.Value,
                    Py = (float)BackgroundValue2Slider.Value,
                    Pz = (float)BackgroundValue3Slider.Value,
                });
            }
            Console.WriteLine("BackgroundSlider");

        }

        private void BackgroundRotateXResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundRotateXSlider.Value = 180f;
        }

        private void BackgroundRotateYResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundRotateYSlider.Value = 180f;
        }

        private void BackgroundRotateZResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundRotateZSlider.Value = 180f;
        }

        private void BackgroundValue1ResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundValue1Slider.Value = 0f;
        }

        private void BackgroundValue2ResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundValue2Slider.Value = 0f;
        }

        private void BackgroundValue3ResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundValue3Slider.Value = 0f;
        }

        //-----------詳細設定----------------
        //===========EVMC4U===========
        float BoneFilter = 1f;
        float BlendShapeFilter = 1f;
        private async void EVMC4U_Checked(object sender, RoutedEventArgs e)
        {
            float tmp = 0f;
            if (EVMC4UBoneFilterValueTextBox != null && float.TryParse(EVMC4UBoneFilterValueTextBox.Text, out tmp))
            {
                BoneFilter = tmp;
            }
            if (EVMC4UBlendShapeFilterValueTextBox != null && float.TryParse(EVMC4UBlendShapeFilterValueTextBox.Text, out tmp))
            {
                BlendShapeFilter = tmp;
            }

            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.EVMC4UControl
                {
                    Freeze = EVMC4UFreezeCheckBox.IsChecked.Value,
                    BoneFilterEnable = EVMC4UBoneFilterCheckBox.IsChecked.Value,
                    BlendShapeFilterEnable = EVMC4UBlendShapeFilterCheckBox.IsChecked.Value,
                    BoneFilterValue = BoneFilter,
                    BlendShapeFilterValue = BlendShapeFilter,
                });
            }
            Console.WriteLine("EVMC4U");
        }
        private async void EVMC4U_TextChanged(object sender, TextChangedEventArgs e)
        {
            //ただ転送する(お行儀悪い)
            EVMC4U_Checked(null, null);
        }
        private async void EVMC4U_TakePhotoButton(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.EVMC4UTakePhotoCommand
            {
            });
            Console.WriteLine("EVMC4U TakePhoto");
        }

        //===========Window===========
        private async void WindowOption_Checked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.WindowControl
                {
                    Border = WindowOptionWindowBorderCheckBox.IsChecked.Value,
                    ForceForeground = WindowOptionForceForegroundCheckBox.IsChecked.Value,
                    Transparent = WindowOptionTransparentCheckBox.IsChecked.Value,
                });
            }
            Console.WriteLine("WindowOption");
        }

        //===========Root位置===========
        private async void RootPos_Checked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.RootPositionControl
                {
                    CameraLock = CameraRootPosLockCheckBox.IsChecked.Value,
                    LightLock = LightRootPosLockCheckBox.IsChecked.Value,
                });
            }
            Console.WriteLine("RootPos");
        }

        //===========外部連携===========
        private async void ExternalControl_Checked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.ExternalControl
                {
                    OBS = OBSExternalControl_CheckBox.IsChecked.Value
                });
            }
            Console.WriteLine("ExternalControl");
        }
        //===========SEDSSサーバー===========
        private async void SEDSSServer_Checked(object sender, RoutedEventArgs e)
        {
            if (SEDSSServerEnableCheckBox != null)
            {
                SEDSSServerPasswordTextBox.IsEnabled = !SEDSSServerEnableCheckBox.IsChecked.Value;
            }

            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.SEDSSServerControl
                {
                    Enable = SEDSSServerEnableCheckBox.IsChecked.Value,
                    Password = SEDSSServerPasswordTextBox.Text,
                });
            }
            Console.WriteLine("SEDSSServer");
        }

        //===========SEDSSクライアント===========
        private async void SEDSSClientUploadButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.SEDSSClientRequestCommand
                {
                    RequestType = PipeCommands.SEDSS_RequestType.Upload,
                    Address = SEDSSClientAddressTextBox.Text,
                    Port = SEDSSClientPortTextBox.Text,
                    Password = SEDSSClientPasswordTextBox.Text,
                    ID = SEDSSClientIDTextBox.Text,
                });
            }
            Console.WriteLine("SEDSSClient Upload");
        }
        private async void SEDSSClientDownloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.SEDSSClientRequestCommand
                {
                    RequestType = PipeCommands.SEDSS_RequestType.Downdload,
                    Address = SEDSSClientAddressTextBox.Text,
                    Port = SEDSSClientPortTextBox.Text,
                    Password = SEDSSClientPasswordTextBox.Text,
                    ID = SEDSSClientIDTextBox.Text,
                });
            }
            Console.WriteLine("SEDSSClient Download");
        }
        //-----------色設定----------------
        //===========背景色===========
        private async void BackgroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
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
            Console.WriteLine("BackgroundColor");
        }

        //===========ライト色===========
        private async void LightColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (client != null)
            {
                Color? d = ((ColorPicker)sender).SelectedColor;
                if (d.HasValue)
                {
                    Color c = d.Value;
                    await client?.SendCommandAsync(new PipeCommands.LightColor { r = c.R, g = c.G, b = c.B });
                }
            }
            Console.WriteLine("LightColor");
        }


    }
}
