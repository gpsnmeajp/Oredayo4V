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
using System.IO;
using System.Net;
using System.Text;
using System.Media;
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
using Newtonsoft.Json;
using System.Collections.ObjectModel;
using System.Reflection;

using MaterialDesignExtensions.Controls;
using System.Threading;
//using System.Drawing;

namespace WPF_UI
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : MaterialWindow// : Window
    {
        private MemoryMappedFileClient client;
        private DispatcherTimer dispatcherTimer;
        private DispatcherTimer initialTimer;
        private float gamingH = 0f;

        private int UI_KeepAlive = int.MaxValue;
        private float cameraResetHeight = 0f;
        private DateTime lastStartTime = new DateTime();

        static ResourceDictionary[] dics = null;

        private Common commonSetting = null;

        private bool FoundOnce = false; //接続されたか
        private int DiscoverTimer = 0;

        private void Windows_KeyDown(object sender, KeyEventArgs e)
        {
            /*
            //デバッグ用言語切替
            if (e.Key == Key.A)
            {
                SetLangDict(0);
            }
            if (e.Key == Key.S)
            {
                SetLangDict(1);
            }
            if (e.Key == Key.D)
            {
                SetLangDict(2);
            }
            */
        }

        System.Windows.Media.Color ToWPFColor(System.Drawing.Color c)
        {
            return System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
        }

        void SetLangDict(int num)
        {
            Application.Current.Resources.MergedDictionaries.Remove(dics[0]); //JP
            Application.Current.Resources.MergedDictionaries.Remove(dics[1]); //EN
            Application.Current.Resources.MergedDictionaries.Remove(dics[2]); //KO

            Application.Current.Resources.MergedDictionaries.Add(dics[num]); //JP
        }

        public MainWindow()
        {
            InitializeComponent();

            if (dics == null)
            {
                dics = new ResourceDictionary[Application.Current.Resources.MergedDictionaries.Count];
                Application.Current.Resources.MergedDictionaries.CopyTo(dics, 0);
            }

            SetLangDict(0);//JP
        }

        //-----------システム系----------------

        private void Reload()
        {
            Dispatcher.Invoke(async () =>
            {
                //デフォルト値送信
                EVMC4U_Checked(null, null);
                //VRMLoadButton_Click(null, null);
                await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text, skip = true, hide = false });

                //BackgroundObjectLoadButton_Click(null, null);
                await client.SendCommandAsync(new PipeCommands.LoadBackground { filepath = BackgroundObjectPathTextBox.Text, skip = true });
                BackgroundChecked(null, null);

                CameraSlider_ValueChanged(null, null);
                LightSlider_ValueChanged(null, null);
                //BackgroundSlider_ValueChanged(null, null);
                BackgroundChecked(null, null);
                WindowOption_Checked(null, null);
                RootPos_Checked(null, null);
                ExternalControl_Checked(null, null);
                UnityCaptureEnable_Checked(null, null);
                //SEDSSServer_Checked(null, null); //SEDSSサーバーは起動時同期しない
                BackgroundColorPicker_SelectedColorChanged(null, null);
                LightColorPicker_SelectedColorChanged(null, null);
                EnvironmentColorPicker_SelectedColorChanged(null, null);
                PostProcessingStackSlider_Checked(null, null);
            });
        }

        private void Client_Received(object sender, DataReceivedEventArgs e)
        {
            Dispatcher.Invoke(async () => {
                UI_KeepAlive = 0; //何らか受信した

                if (e.CommandType == typeof(PipeCommands.Hello))
                {
                    var d = (PipeCommands.Hello)e.Data;

                    //Unity側からすでに送られているものは無視する
                    if (lastStartTime != d.startTime)
                    {
                        lastStartTime = d.startTime;
                        //Unity側起動時処理(値送信など)
                        Console.WriteLine(">Hello");
                        Console.WriteLine(lastStartTime);

                        Reload();
                    }
                }
                else if (e.CommandType == typeof(PipeCommands.Bye))
                {
                    //Unity側終了処理
                    //this.Close();
                    Console.WriteLine(">Bye");
                }
                else if (e.CommandType == typeof(PipeCommands.LogMessage))
                {
                    //ログ受信時処理
                    var d = (PipeCommands.LogMessage)e.Data;
                    StatusBarText.Text = "[" + d.Type.ToString() + "] " + d.Message;
                    switch (d.Type)
                    {
                        case PipeCommands.LogType.Error:
                            StatusBarText.Foreground = new SolidColorBrush(Color.FromRgb(255, 128, 128));
                            break;
                        case PipeCommands.LogType.Warning:
                            StatusBarText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 30));
                            break;
                        default:
                            StatusBarText.Foreground = new SolidColorBrush(Color.FromRgb(255, 255, 255));
                            break;
                    }
                    if (d.Message.Contains("SocketException"))
                    {
                        MessageBox.Show("ポートのオープンに失敗しました。\n他のアプリケーションが競合している可能性があります。\n\n通信機能は利用できません。\n(Port open failed.)", "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else if (e.CommandType == typeof(PipeCommands.SendMessage))
                {
                    //エラーダイアログ処理
                    var d = (PipeCommands.SendMessage)e.Data;
                    MessageBox.Show(d.Message, "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else if (e.CommandType == typeof(PipeCommands.CyclicStatus))
                {
                    //KeepAlive処理
                    var d = (PipeCommands.CyclicStatus)e.Data;
                    if (!d.EVMC4U)
                    {
                        if (EVMC4UEnableCheckBox != null && !EVMC4UEnableCheckBox.IsChecked.Value)
                        {
                            Welcome2Expander.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.LightPink));
                            Welcome2Expander.IsExpanded = true;
                        }
                        else
                        {
                            Welcome2Expander.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.White));
                            Welcome2Expander.IsExpanded = true;
                        }
                    }
                    else
                    {
                        Welcome2Expander.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.LightGreen));
                        Welcome2Expander.IsExpanded = false;
                    }

                    //前回と値が違うときは合わせる(VRM読み込み時)
                    if (cameraResetHeight != d.HeadHeight)
                    {
                        CameraValue2Slider.Value = d.HeadHeight;
                        BackgroundValue2Slider.Value = d.HeadHeight;
                    }
                    cameraResetHeight = d.HeadHeight;
                }
                else if (e.CommandType == typeof(PipeCommands.VRMLicenceCheck))
                {
                    //ライセンスチェック処理
                    var d = (PipeCommands.VRMLicenceCheck)e.Data;
                    if (d.skip)
                    {
                        //スキップ
                        await client.SendCommandAsync(new PipeCommands.VRMLicenceAnser
                        {
                            Agree = true,
                        });
                    }
                    else
                    {
                        var result = MessageBox.Show("このVRMライセンスに同意しますか？\nDo you agree with this VRM license?", "Oredayo UI", MessageBoxButton.YesNo, MessageBoxImage.Question);

                        await client.SendCommandAsync(new PipeCommands.VRMLicenceAnser
                        {
                            Agree = result == MessageBoxResult.Yes,
                        });
                    }
                }
                else if (e.CommandType == typeof(PipeCommands.DiscoverResponse))
                {
                    //探索応答処理
                    var d = (PipeCommands.DiscoverResponse)e.Data;
                    SEDSSClientAddressTextBox.Text = d.ip;
                    FoundOnce = true;
                }
                else if (e.CommandType == typeof(PipeCommands.SEDSSResult))
                {
                    //アバター送信結果処理
                    var d = (PipeCommands.SEDSSResult)e.Data;
                    if (d.ok)
                    {
                        SEDSS_Client_Card.Background = new SolidColorBrush(Color.FromArgb(255, 219, 255, 223));
                    }
                    else
                    {
                        SEDSS_Client_Card.Background = new SolidColorBrush(Color.FromArgb(255, 255, 235, 235));
                    }
                }
                else if (e.CommandType == typeof(PipeCommands.LoginDVRConnectResult))
                {
                    //DVRCログイン処理
                    var d = (PipeCommands.LoginDVRConnectResult)e.Data;
                    DVRConnectKey.Content = d.key;
                }
                else if (e.CommandType == typeof(PipeCommands.GetAvatarDVRConnectResult))
                {
                    //DVRCアバター取得処理
                    var d = (PipeCommands.GetAvatarDVRConnectResult)e.Data;
                    DVRConnectAvatarComboBox.Items.Clear();
                    for (int i = 0; i < d.avatars.Length; i++) {
                        DVRConnectAvatarComboBox.Items.Add(d.avatars[i]);
                    }
                    DVRConnectAvatarComboBox.SelectedIndex = 0;
                }

            });
        }
        private void LoadCommonSetting()
        {
            //共通設定を読み込む。なければ作る
            if (File.Exists("Common.json"))
            {
                //読み込み
                commonSetting = JsonConvert.DeserializeObject<Common>(File.ReadAllText("Common.json", new UTF8Encoding(false)), new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Populate //デフォルト値を使用する
                });
            }
            else
            {
                commonSetting = new Common();
                commonSetting.SEDSSClientPortTextBox_Text = "8000";
                SaveCommonSetting();
            }
        }
        private void SaveCommonSetting()
        {
            //書き込む
            string json = JsonConvert.SerializeObject(commonSetting);
            File.WriteAllText("Common.json", json, new UTF8Encoding(false));
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCommonSetting();
            //初回起動時処理
            if (!commonSetting.Initialized)
            {
                //初回起動タイマー起動
                initialTimer = new DispatcherTimer();
                initialTimer.Interval = new TimeSpan(0, 0, 0, 5,0);
                initialTimer.Tick += new EventHandler(InitialTimerEvent);
                initialTimer.Start();
            }

            //SEDSS設定読み込み
            SEDSSClientAddressTextBox.Text = commonSetting.SEDSSClientAddressTextBox_Text;
            SEDSSClientPortTextBox.Text = commonSetting.SEDSSClientPortTextBox_Text;
            SEDSSClientPasswordTextBox.Text = commonSetting.SEDSSClientPasswordTextBox_Password;

            //通信をトライ開始
            client = new MemoryMappedFileClient();
            client.ReceivedEvent += Client_Received;
            client.Start("Oredayo_UI_Connection");

            //タイマー起動
            dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 32);
            dispatcherTimer.Tick += new EventHandler(GenericTimer);
            dispatcherTimer.Start();

            //起動したことを伝える
            await client.SendCommandAsync(new PipeCommands.Hello { });

            //IPアドレス取得
            IPAddress[] ips = Dns.GetHostAddresses(Dns.GetHostName());
            string ipList = "";
            foreach (var ip in ips)
            {
                //IPv4のみ
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                {
                    ipList += ip.ToString() + "\n";
                }
            }
            WelcomeIPAddressTextBlock.Text = ipList.Trim();

            //クイックセーブを起動時に読み込む
            if (File.Exists("QuickSetting.json"))
            {
                Setting s = LoadSettingFromFile("QuickSetting.json");
                //起動時のみ言語を反映する
                LanguageComboBox.SelectedIndex = s.LanguageComboBox_SelectedIndex;
            }
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

        private void InitialTimerEvent(object sender, EventArgs e)
        {
            Console.WriteLine("InitialTimerEvent");
            initialTimer.Stop(); //1回っきり
            if (!commonSetting.Initialized) {
                //初回起動を済ませた
                commonSetting.Initialized = true;
                SaveCommonSetting();

                var result = MessageBox.Show("Oredayo4Vへようこそ！\n説明書を開きますか？\nWelcome to Oredayo4V!\nDo you want to see manual?", "Oredayo UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://github.com/gpsnmeajp/Oredayo4V/wiki");
                }

                var result2 = MessageBox.Show("サポートDiscordを開きますか？(原則こちらでのみ対応しております)\nDo you want to join discord?", "Oredayo UI", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result2 == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start("https://discord.gg/nGapSR7");
                }
            }
        }

        private void GenericTimer(object sender, EventArgs e)
        {
            if (GamingBackgroundCheckBox.IsChecked.HasValue && GamingBackgroundCheckBox.IsChecked.Value)
            {
                BackgroundColorPicker.SelectedColor = akr.WPF.Utilities.ColorUtilities.HsvToRgb(gamingH, 1, 0.2f);
            }
            if (GamingLightCheckBox.IsChecked.HasValue && GamingLightCheckBox.IsChecked.Value)
            {
                LightColorPicker.SelectedColor = akr.WPF.Utilities.ColorUtilities.HsvToRgb(gamingH, 1, 1);
            }
            if (GamingEnvironmentCheckBox.IsChecked.HasValue && GamingEnvironmentCheckBox.IsChecked.Value)
            {
                EnvironmentColorPicker.SelectedColor = akr.WPF.Utilities.ColorUtilities.HsvToRgb(gamingH, 1, 1);
            }

            gamingH += 1f;
            if (gamingH > 360f)
            {
                gamingH -= 360f;
            }

            
            //身長が一定以上で読み込み成功と判定する
            if (cameraResetHeight > 0.1f)
            {
                VRMLoadCard.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.LightGreen));
            }
            else {
                VRMLoadCard.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.White));
            }
            

            //3秒おきに探索
            if (FoundOnce == false && DiscoverTimer > 3 * 30)
            {
                Console.WriteLine("AutoDiscoverRequest");
                Task.Run(async () =>
                {
                    await client.SendCommandAsync(new PipeCommands.DiscoverRequest { });
                });
                DiscoverTimer = 0;
            }
            DiscoverTimer++;

            //-------
            //2秒間反応がない
            if (UI_KeepAlive > 60)
            {
                //ようこそ画面
                Welcome1Expander.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.White));
                Welcome2Expander.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.White));
                Welcome3Expander.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.White));
                Welcome1Expander.IsExpanded = true;
                Welcome2Expander.IsExpanded = true;

                //バッファの影響で高速点滅する場合があるので、UI通信が切れた場合は禁止
                GamingBackgroundCheckBox.IsChecked = false;
                GamingLightCheckBox.IsChecked = false;
                GamingEnvironmentCheckBox.IsChecked = false;

                //通信できていないなら探索リセット
                FoundOnce = false;
            }
            else
            {
                UI_KeepAlive++;

                //ようこそ画面
                Welcome1Expander.Background = new SolidColorBrush(ToWPFColor(System.Drawing.Color.LightGreen));
                Welcome1Expander.IsExpanded = false;
            }
        }

        //-----------ようこそ----------------
        //===========チュートリアル==========

        //開き閉じを記憶する
        private void WelcomeExpanded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("OK");
        }


        private void WelcomeWaidayoNextButton_Clicked(object sender, RoutedEventArgs e)
        {
            WelcomeWaidayoTabControl.SelectedIndex++;
        }

        private void LaunchOredayo_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    //Unity側を強制終了する
                    System.Diagnostics.Process proc1 = new System.Diagnostics.Process();
                    proc1.StartInfo.UseShellExecute = true;
                    proc1.StartInfo.CreateNoWindow = true;
                    proc1.StartInfo.ErrorDialog = false;
                    proc1.StartInfo.FileName = "taskkill";
                    proc1.StartInfo.Arguments = " /F /IM Oredayo.exe";
                    proc1.StartInfo.WorkingDirectory = "";
                    proc1.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Minimized;
                    proc1.Start();

                    await Task.Delay(3000);

                    //Unity側を起動する
                    System.Diagnostics.Process proc2 = new System.Diagnostics.Process();
                    proc2.StartInfo.UseShellExecute = false; //既知のため不要
                    proc2.StartInfo.CreateNoWindow = false;
                    proc2.StartInfo.ErrorDialog = false;
                    proc2.StartInfo.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../Environment/Oredayo.exe";
                    proc2.StartInfo.Arguments = "";
                    proc2.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../Environment/";
                    proc2.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Normal;
                    proc2.Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });
        }

        private async void AutoConfig_Click(object sender, RoutedEventArgs e)
        {
            if (EVMC4UPortTextBox.Text == "")
            {
                EVMC4UPortTextBox.Text = "39540";
            }
            EVMC4UEnableCheckBox.IsChecked = true;
            await client.SendCommandAsync(new PipeCommands.DiscoverRequest { });
        }

        private void Preset1_Cliecked(object sender, RoutedEventArgs e)
        {
            LoadSettingFromFile("Preset1.json");
        }

        private void Preset2_Cliecked(object sender, RoutedEventArgs e)
        {
            LoadSettingFromFile("Preset2.json");
        }

        private void Preset3_Cliecked(object sender, RoutedEventArgs e)
        {
            LoadSettingFromFile("Preset3.json");
        }

        private void Preset4_Cliecked(object sender, RoutedEventArgs e)
        {
            LoadSettingFromFile("Preset4.json");
        }
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start(e.Uri.AbsoluteUri);
        }

        //-----------基本設定----------------
        //===========言語切替==========
        private void LanguageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //言語切替
            if (dics != null)
            {
                SetLangDict(LanguageComboBox.SelectedIndex);
            }
        }

        //===========設定読み込み==========

        private void QuickSaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string json = JsonConvert.SerializeObject(SaveSetting());
                File.WriteAllText("QuickSetting.json", json, new UTF8Encoding(false));
            }
            catch (Exception)
            {
                MessageBox.Show("設定の保存に失敗しました\n(Save failed.)", "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void QuickLoadButton_Click(object sender, RoutedEventArgs e)
        {
            LoadSettingFromFile("QuickSetting.json");
        }

        Setting LoadSettingFromFile(string path)
        {
            if (File.Exists(path))
            {
                Setting s = JsonConvert.DeserializeObject<Setting>(File.ReadAllText(path, new UTF8Encoding(false)), new JsonSerializerSettings
                {
                    DefaultValueHandling = DefaultValueHandling.Populate //デフォルト値を使用する
                });
                LoadSetting(s);
                return s;
            }
            else
            {
                MessageBox.Show("ファイルがありません\n(File not found.)", "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                return new Setting();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = ".json";
            dlg.Filter = "JSON file|*.json|All file|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(SaveSetting());
                    File.WriteAllText(dlg.FileName, json, new UTF8Encoding(false));
                }
                catch (Exception)
                {
                    MessageBox.Show("設定の保存に失敗しました\n(Save failed.)", "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = ".json";
            dlg.Filter = "JSON file|*.json|All file|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                LoadSettingFromFile(dlg.FileName);
            }
        }

        //===========VRM読み込み===========
        private async void VRMLoadButton_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text, skip = false, hide = false });
            SEDSSClientUploadFilePathTextBox.Text = VRMPathTextBox.Text;
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
                await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text, skip = false, hide = false });
                SEDSSClientUploadFilePathTextBox.Text = VRMPathTextBox.Text;
                Console.WriteLine("VRMLoadFileSelectButton_Click");
            }
        }
        private async void VRMLoadFileSelectOnWelcomeButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = ".vrm";
            dlg.Filter = "VRM file|*.vrm|All file|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                VRMPathTextBox.Text = dlg.FileName;
                await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text, skip = false, hide = true });
                SEDSSClientUploadFilePathTextBox.Text = VRMPathTextBox.Text;
                Console.WriteLine("VRMLoadFileSelectButton_Click");
            }
        }

        //===========DVRConnect===========
        private async void DVRConnectLogin_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.LoginDVRConnect { });
            Console.WriteLine("DVRConnectLogin_Click");
        }
        private async void DVRConnectLoad_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.LoadDVRConnect
            {
                index = DVRConnectAvatarComboBox.SelectedIndex
            });
            Console.WriteLine("DVRConnectLoad_Click");
        }
        private async void DVRConnectReloadList_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.GetAvatarDVRConnect { });
            Console.WriteLine("DVRConnectReloadList_Click");
        }

        //===========背景読み込み===========
        private async void BackgroundObjectLoadButton_Click(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.LoadBackground { filepath = BackgroundObjectPathTextBox.Text, skip = false });
            BackgroundChecked(null, null);
            Console.WriteLine("BackgroundObjectLoadButton_Click");
        }

        private async void BackgroundObjectLoadFileSelectButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = "";
            dlg.DefaultExt = ".png";
            dlg.Filter = "PNG file|*.png|VRM file|*.vrm|All file|*.*";

            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                BackgroundObjectPathTextBox.Text = dlg.FileName;
                await client.SendCommandAsync(new PipeCommands.LoadBackground { filepath = BackgroundObjectPathTextBox.Text, skip = false });
                BackgroundChecked(null, null);
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
            CameraRotateXSlider.Value = 0f;
        }

        private void CameraRotateYResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraRotateYSlider.Value = 0f;
        }

        private void CameraRotateZResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraRotateZSlider.Value = 0f;
        }
        private void CameraValue1ResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraValue1Slider.Value = 1.7f;
        }

        private void CameraValue2ResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraValue2Slider.Value = cameraResetHeight;
        }

        private void CameraValue3ResetButton_Click(object sender, RoutedEventArgs e)
        {
            CameraValue3Slider.Value = 20f;
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
            LightRotateXSlider.Value = 140f;
        }

        private void LightRotateYResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightRotateYSlider.Value = 0f;
        }

        private void LightRotateZResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightRotateZSlider.Value = 0f;
        }

        private void LightValue1ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightValue1Slider.Value = 5f;
        }

        private void LightValue2ResetButton_Click(object sender, RoutedEventArgs e)
        {
            LightValue2Slider.Value = 50f;
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

                    scale = (float)BackgroundScaleSlider.Value,

                    cameraTaget = BackgroundCameraTagetCheckBox.IsChecked.Value,

                    windowCapture = BackgroundWindowCaptureCheckBox.IsChecked.Value,
                    windowTtitle = BackgroundWindowTitleTextBox.Text,
                });
            }

            Console.WriteLine("BackgroundSlider");

        }
        private void BackgroundChecked(object sender, RoutedEventArgs e)
        {
            //ただ転送する(お行儀悪い)
            BackgroundSlider_ValueChanged(null, null);
        }

        private void Background_TextChanged(object sender, TextChangedEventArgs e)
        {
            //ただ転送する(お行儀悪い)
            BackgroundSlider_ValueChanged(null, null);
        }

        private void BackgroundRotateXResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundRotateXSlider.Value = 0f;
        }

        private void BackgroundRotateYResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundRotateYSlider.Value = -180f;
        }

        private void BackgroundRotateZResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundRotateZSlider.Value = 0f;
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
            BackgroundValue3Slider.Value = -1f;
        }

        private void BackgroundScaleResetButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundScaleSlider.Value = 1f;
        }

        //-----------詳細設定----------------
        //===========EVMC4U===========
        float BoneFilter = 1f;
        float BlendShapeFilter = 1f;
        int Port = 0;
        private async void EVMC4U_Checked(object sender, RoutedEventArgs e)
        {
            float tmp = 0f;
            int tmpInt = 0;
            if (EVMC4UBoneFilterValueTextBox != null && float.TryParse(EVMC4UBoneFilterValueTextBox.Text, out tmp))
            {
                BoneFilter = tmp;
            }
            if (EVMC4UBlendShapeFilterValueTextBox != null && float.TryParse(EVMC4UBlendShapeFilterValueTextBox.Text, out tmp))
            {
                BlendShapeFilter = tmp;
            }
            if (EVMC4UPortTextBox != null && int.TryParse(EVMC4UPortTextBox.Text, out tmpInt))
            {
                Port = tmpInt;
            }

            if (EVMC4UEnableCheckBox != null && EVMC4UPortTextBox != null)
            {
                EVMC4UPortTextBox.IsEnabled = !EVMC4UEnableCheckBox.IsChecked.Value;

                //有効なときだけ反映する
                /*
                if (EVMC4UPortTextBlock != null && WelcomePortTextBlock != null && EVMC4UEnableCheckBox.IsChecked.Value)
                {
                    EVMC4UPortTextBlock.Text = Port.ToString();
                    WelcomePortTextBlock.Text = " " + Port.ToString();
                }
                */
            }

            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.EVMC4UControl
                {
                    Enable = EVMC4UEnableCheckBox.IsChecked.Value,
                    Port = Port,
                    Freeze = EVMC4UFreezeCheckBox.IsChecked.Value,
                    BoneFilterEnable = EVMC4UBoneFilterCheckBox.IsChecked.Value,
                    BlendShapeFilterEnable = EVMC4UBlendShapeFilterCheckBox.IsChecked.Value,
                    BoneFilterValue = BoneFilter,
                    BlendShapeFilterValue = BlendShapeFilter,
                });
            }
            Console.WriteLine("EVMC4U");
        }
        private void EVMC4U_TextChanged(object sender, TextChangedEventArgs e)
        {
            //ただ転送する(お行儀悪い)
            EVMC4U_Checked(null, null);
        }
        private async void EVMC4U_TakePhotoButton(object sender, RoutedEventArgs e)
        {
            await client.SendCommandAsync(new PipeCommands.EVMC4UTakePhotoCommand
            {
            });
            SystemSounds.Beep.Play();
            Console.WriteLine("EVMC4U TakePhoto");
        }

        //===========Window===========
        private async void WindowOption_Checked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                if (WindowOptionTransparentCheckBox.IsChecked.Value)
                {
                    WindowOptionWindowBorderCheckBox.IsChecked = false;
                }

                await client.SendCommandAsync(new PipeCommands.WindowControl
                {
                    NoBorder = !WindowOptionWindowBorderCheckBox.IsChecked.Value,
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
                    BackgroundLock =
                    BackgroundRootPosLockCheckBox.IsChecked.Value,
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

        //===========仮想Webカメラ===========
        private async void UnityCaptureEnable_Checked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.VirtualWebCamera
                {
                    Enable = UnityCaptureEnable_CheckBox.IsChecked.Value
                });
            }
            Console.WriteLine("UnityCaptureEnable");
        }

        private void UnityCaptureInstallButton_Clicked(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process proc1 = new System.Diagnostics.Process();
            proc1.StartInfo.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/DLLInstaller32.EXE";
            proc1.StartInfo.Arguments = @"/i ..\UnityCaptureFilter\UnityCaptureFilter32bit.dll";
            proc1.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/";
            proc1.Start();
            proc1.WaitForExit();

            System.Diagnostics.Process proc2 = new System.Diagnostics.Process();
            proc2.StartInfo.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/DLLInstaller64.EXE";
            proc2.StartInfo.Arguments = @"/i ..\UnityCaptureFilter\UnityCaptureFilter64bit.dll";
            proc2.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/";
            proc2.Start();
            proc2.WaitForExit();
        }

        private void UnityCaptureUninstallButton_Clicked(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process proc1 = new System.Diagnostics.Process();
            proc1.StartInfo.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/DLLInstaller32.EXE";
            proc1.StartInfo.Arguments = @"/u ..\UnityCaptureFilter\UnityCaptureFilter32bit.dll";
            proc1.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/";
            proc1.Start();
            proc1.WaitForExit();

            System.Diagnostics.Process proc2 = new System.Diagnostics.Process();
            proc2.StartInfo.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/DLLInstaller64.EXE";
            proc2.StartInfo.Arguments = @"/u ..\UnityCaptureFilter\UnityCaptureFilter64bit.dll";
            proc2.StartInfo.WorkingDirectory = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "/../DLLInstaller/";
            proc2.Start();
            proc2.WaitForExit();
        }

        //===========SEDSSサーバー===========
        /*
        private async void SEDSSServer_Checked(object sender, RoutedEventArgs e)
        {
            if (SEDSSServerEnableCheckBox != null)
            {
                if (SEDSSServerEnableCheckBox.IsChecked.Value)
                {
                    if (SEDSSServerPasswordTextBox.Password.Length < 4)
                    {
                        MessageBox.Show("暗号化パスワードが短すぎます\n(Password is too short.)", "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                        //拒否
                        SEDSSServerEnableCheckBox.IsChecked = false; //強制的にオフ
                        return;
                    }

                    var result = MessageBox.Show("SEDSSサーバー機能を本当に有効にしますか？\nパスワードを共有したデバイスとVRMデータを共有することができるようになります。\n注意: 信頼できる端末とのみ通信してください。\n(Are you sure you want to Data Sharing with LAN device?)", "Oredayo UI", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                    if (result != MessageBoxResult.OK)
                    {
                        //拒否
                        SEDSSServerEnableCheckBox.IsChecked = false; //強制的にオフ
                        return;
                    }
                }
                SEDSSServerPasswordTextBox.IsEnabled = !SEDSSServerEnableCheckBox.IsChecked.Value;
                SEDSSServerExchangeFilePathTextBox.IsEnabled = !SEDSSServerEnableCheckBox.IsChecked.Value;
            }

            if (client != null)
            {
                await client.SendCommandAsync(new PipeCommands.SEDSSServerControl
                {
                    Enable = SEDSSServerEnableCheckBox.IsChecked.Value,
                    Password = SEDSSServerPasswordTextBox.Password,
                    ExchangeFilePath = SEDSSServerExchangeFilePathTextBox.Text,
                });
            }
            Console.WriteLine("SEDSSServer");
        }
        */

        //===========SEDSSクライアント===========
        private async void SEDSSClientUploadButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                if (SEDSSClientPasswordTextBox.Text.Length < 4)
                {
                    MessageBox.Show("暗号化パスワードが短すぎます\n(Password is too short.)", "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var result = MessageBox.Show("SEDSSアップロードを本当に実行しますか？\nパスワードを共有したデバイスにVRMデータを送信します。\n注意: 信頼できる端末とのみ通信してください。\n(Are you sure you want to Data Upload to device?)", "Oredayo UI", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
                await client.SendCommandAsync(new PipeCommands.SEDSSClientRequestCommand
                {
                    RequestType = PipeCommands.SEDSS_RequestType.Upload,
                    Address = SEDSSClientAddressTextBox.Text,
                    Port = SEDSSClientPortTextBox.Text,
                    Password = SEDSSClientPasswordTextBox.Text,
                    ID = "",
                    UploadFilePath = SEDSSClientUploadFilePathTextBox.Text,
                });
            }
            Console.WriteLine("SEDSSClient Upload");

            //保存
            commonSetting.SEDSSClientAddressTextBox_Text = SEDSSClientAddressTextBox.Text;
            commonSetting.SEDSSClientPortTextBox_Text = SEDSSClientPortTextBox.Text;
            commonSetting.SEDSSClientPasswordTextBox_Password = SEDSSClientPasswordTextBox.Text;
            SaveCommonSetting();
        }
        private async void SEDSSClientDownloadButton_Clicked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                if (SEDSSClientPasswordTextBox.Text.Length < 4)
                {
                    MessageBox.Show("暗号化パスワードが短すぎます\n(Password is too short.)", "Oredayo UI", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var result = MessageBox.Show("SEDSSダウンロードを本当に実行しますか？\nパスワードを共有したデバイスからVRMデータを受信します。\n注意: 信頼できる端末とのみ通信してください。\n(Are you sure you want to Data Download from device?)", "Oredayo UI", MessageBoxButton.OKCancel, MessageBoxImage.Question);
                if (result != MessageBoxResult.OK)
                {
                    return;
                }
                await client.SendCommandAsync(new PipeCommands.SEDSSClientRequestCommand
                {
                    RequestType = PipeCommands.SEDSS_RequestType.Downdload,
                    Address = SEDSSClientAddressTextBox.Text,
                    Port = SEDSSClientPortTextBox.Text,
                    Password = SEDSSClientPasswordTextBox.Text,
                    ID = "",
                    UploadFilePath = "",
                });
            }
            Console.WriteLine("SEDSSClient Download");

            //保存
            commonSetting.SEDSSClientAddressTextBox_Text = SEDSSClientAddressTextBox.Text;
            commonSetting.SEDSSClientPortTextBox_Text = SEDSSClientPortTextBox.Text;
            commonSetting.SEDSSClientPasswordTextBox_Password = SEDSSClientPasswordTextBox.Text;
            SaveCommonSetting();
        }
        //-----------色設定----------------
        //===========背景色===========
        private async void BackgroundColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (client != null)
            {
                Color? d = BackgroundColorPicker.SelectedColor;
                if (d.HasValue)
                {
                    Color c = d.Value;
                    await client?.SendCommandAsync(new PipeCommands.BackgrounColor { r = c.R, g = c.G, b = c.B });
                }
                //透明だと見えないでしょ！
                WindowOptionTransparentCheckBox.IsChecked = false;
            }
            Console.WriteLine("BackgroundColor");
        }

        //===========ライト色===========
        private async void LightColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (client != null)
            {
                Color? d = LightColorPicker.SelectedColor;
                if (d.HasValue)
                {
                    Color c = d.Value;
                    await client?.SendCommandAsync(new PipeCommands.LightColor { r = c.R, g = c.G, b = c.B });
                }
            }
            Console.WriteLine("LightColor");
        }

        //===========環境光===========
        private async void EnvironmentColorPicker_SelectedColorChanged(object sender, RoutedPropertyChangedEventArgs<Color?> e)
        {
            if (client != null)
            {
                Color? d = EnvironmentColorPicker.SelectedColor;
                if (d.HasValue)
                {
                    Color c = d.Value;
                    await client?.SendCommandAsync(new PipeCommands.EnvironmentColor { r = c.R, g = c.G, b = c.B });
                }
            }
            Console.WriteLine("EnvironmentColor");
        }
        //-----------画質設定----------------
        //===========PostProcessing===========
        private async void PostProcessingStackSlider_Checked(object sender, RoutedEventArgs e)
        {
            if (client != null)
            {
                await client?.SendCommandAsync(new PipeCommands.PostProcessingControl
                {
                    AntiAliasingEnable = PostProcessingAntiAliasingEnableCheckBox.IsChecked.Value,

                    BloomEnable = PostProcessingBloomEnableCheckBox.IsChecked.Value,
                    BloomIntensity = (float)PostProcessingBloomIntensitySlider.Value,
                    BloomThreshold = (float)PostProcessingBloomThresholdSlider.Value,

                    DoFEnable = PostProcessingDoFEnableCheckBox.IsChecked.Value,
                    DoFFocusDistance = (float)PostProcessingDoFFocusDistanceSlider.Value,
                    DoFAperture = (float)PostProcessingDoFApertureSlider.Value,
                    DoFFocusLength = (float)PostProcessingDoFFocusLengthSlider.Value,
                    DoFMaxBlurSize = (int)PostProcessingDoFMaxBlurSizeSlider.Value,

                    CGEnable = PostProcessingCGEnableCheckBox.IsChecked.Value,
                    CGTemperature = (float)PostProcessingCGTemperatureSlider.Value,
                    CGSaturation = (float)PostProcessingCGSaturationSlider.Value,
                    CGContrast = (float)PostProcessingCGContrastSlider.Value,

                    VEnable = PostProcessingVEnableCheckBox.IsChecked.Value,
                    VIntensity = (float)PostProcessingVIntensitySlider.Value,
                    VSmoothness = (float)PostProcessingVSmoothnessSlider.Value,
                    VRounded = (float)PostProcessingVRoundedSlider.Value,

                    CAEnable = PostProcessingCAEnableCheckBox.IsChecked.Value,
                    CAIntensity = (float)PostProcessingCAIntensitySlider.Value,
                });
            }
            Console.WriteLine("PostProcessingStack");
        }

        private void PostProcessingStackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //ただ転送する(お行儀悪い)
            PostProcessingStackSlider_Checked(null, null);
        }

        private void PostProcessingCGResetButton_Click(object sender, RoutedEventArgs e)
        {
            PostProcessingCGTemperatureSlider.Value = 0f;
            PostProcessingCGSaturationSlider.Value = 0f;
            PostProcessingCGContrastSlider.Value = 0f;
        }

        void LoadSetting(Setting s)
        {
            Dispatcher.Invoke(async () => {
                //依存関係があるため先に設定する
                BackgroundColorPicker.SelectedColor = Color.FromArgb(255, 255, 255, 255); //リセット
                await Task.Delay(10);

                if (s.WindowOptionTransparentCheckBox_IsChecked_Value)
                {
                    WindowOptionTransparentCheckBox.IsChecked = s.WindowOptionTransparentCheckBox_IsChecked_Value;
                    await Task.Delay(10);
                }
                else
                {
                    WindowOptionTransparentCheckBox.IsChecked = s.WindowOptionTransparentCheckBox_IsChecked_Value;
                    await Task.Delay(10);
                    BackgroundColorPicker.SelectedColor = new Color
                    {
                        R = s.BackgroundColorPicker_SelectedColor_R,
                        G = s.BackgroundColorPicker_SelectedColor_G,
                        B = s.BackgroundColorPicker_SelectedColor_B,
                        A = s.BackgroundColorPicker_SelectedColor_A
                    };
                    await Task.Delay(10);
                }

                WindowOptionWindowBorderCheckBox.IsChecked = !s.WindowOptionWindowBorderCheckBox_IsChecked_Value; //反転
                WindowOptionForceForegroundCheckBox.IsChecked = s.WindowOptionForceForegroundCheckBox_IsChecked_Value;


                if (s.VRMPathTextBox_Text != "")
                {
                    VRMPathTextBox.Text = s.VRMPathTextBox_Text;
                    SEDSSClientUploadFilePathTextBox.Text = VRMPathTextBox.Text;

                    //自動読み込み
                    await client.SendCommandAsync(new PipeCommands.LoadVRM { filepath = VRMPathTextBox.Text, skip = true,hide = false });
                }
                if (s.BackgroundObjectPathTextBox_Text != "")
                {
                    BackgroundObjectPathTextBox.Text = s.BackgroundObjectPathTextBox_Text;

                    //自動読み込み
                    await client.SendCommandAsync(new PipeCommands.LoadBackground { filepath = BackgroundObjectPathTextBox.Text, skip = true });
                }
                //LanguageComboBox.SelectedIndex = s.LanguageComboBox_SelectedIndex; //毎回読み込み時には反映しない
                CameraRotateXSlider.Value = s.CameraRotateXSlider_Value;
                CameraRotateYSlider.Value = s.CameraRotateYSlider_Value;
                CameraRotateZSlider.Value = s.CameraRotateZSlider_Value;
                CameraValue1Slider.Value = s.CameraValue1Slider_Value;
                CameraValue2Slider.Value = s.CameraValue2Slider_Value;
                CameraValue3Slider.Value = s.CameraValue3Slider_Value;
                LightRotateXSlider.Value = s.LightRotateXSlider_Value;
                LightRotateYSlider.Value = s.LightRotateYSlider_Value;
                LightRotateZSlider.Value = s.LightRotateZSlider_Value;
                LightValue1Slider.Value = s.LightValue1Slider_Value;
                LightValue2Slider.Value = s.LightValue2Slider_Value;
                LightValue3Slider.Value = s.LightValue3Slider_Value;
                LightTypeDirectionalRadioButton.IsChecked = s.LightTypeDirectionalRadioButton_IsChecked_Value;
                LightTypePointRadioButton.IsChecked = s.LightTypePointRadioButton_IsChecked_Value;
                LightTypeSpotRadioButton.IsChecked = s.LightTypeSpotRadioButton_IsChecked_Value;
                BackgroundRotateXSlider.Value = s.BackgroundRotateXSlider_Value;
                BackgroundRotateYSlider.Value = s.BackgroundRotateYSlider_Value;
                BackgroundRotateZSlider.Value = s.BackgroundRotateZSlider_Value;
                BackgroundValue1Slider.Value = s.BackgroundValue1Slider_Value;
                BackgroundValue2Slider.Value = s.BackgroundValue2Slider_Value;
                BackgroundValue3Slider.Value = s.BackgroundValue3Slider_Value;
                BackgroundScaleSlider.Value = s.BackgroundScaleSlider_Value;
                BackgroundCameraTagetCheckBox.IsChecked = s.BackgroundCameraTagetCheckBox_IsChecked_Value;
                BackgroundWindowCaptureCheckBox.IsChecked = s.BackgroundWindowCaptureCheckBox_IsChecked_Value;
                BackgroundWindowTitleTextBox.Text = s.BackgroundWindowTitleTextBox_Text;

                await Task.Delay(10);
                EVMC4UPortTextBox.Text = s.EVMC4UPortTextBox_Text;
                EVMC4UFreezeCheckBox.IsChecked = s.EVMC4UFreezeCheckBox_IsChecked_Value;
                EVMC4UBoneFilterCheckBox.IsChecked = s.EVMC4UBoneFilterCheckBox_IsChecked_Value;
                EVMC4UBlendShapeFilterCheckBox.IsChecked = s.EVMC4UBlendShapeFilterCheckBox_IsChecked_Value;
                EVMC4UBoneFilterValueTextBox.Text = s.EVMC4UBoneFilterValueTextBox_Text;
                EVMC4UBlendShapeFilterValueTextBox.Text = s.EVMC4UBlendShapeFilterValueTextBox_Text;
                CameraRootPosLockCheckBox.IsChecked = s.CameraRootPosLockCheckBox_IsChecked_Value;
                LightRootPosLockCheckBox.IsChecked = s.LightRootPosLockCheckBox_IsChecked_Value;
                BackgroundRootPosLockCheckBox.IsChecked = s.BackgroundRootPosLockCheckBox_IsChecked_Value;
                OBSExternalControl_CheckBox.IsChecked = s.OBSExternalControl_CheckBox_IsChecked_Value;
                UnityCaptureEnable_CheckBox.IsChecked = s.UnityCaptureEnable_CheckBox_IsChecked_Value;
                //SEDSSServerPasswordTextBox.Password = s.SEDSSServerPasswordTextBox_Password;
                //SEDSSServerExchangeFilePathTextBox.Text = s.SEDSSServerExchangeFilePathTextBox_Text;
                //SEDSSClientAddressTextBox.Text = s.SEDSSClientAddressTextBox_Text;
                //SEDSSClientPortTextBox.Text = s.SEDSSClientPortTextBox_Text;
                //SEDSSClientPasswordTextBox.Text = s.SEDSSClientPasswordTextBox_Password;
                //SEDSSClientIDTextBox.Text = s.SEDSSClientIDTextBox_Text;
                //SEDSSClientUploadFilePathTextBox.Text = s.SEDSSClientUploadFilePathTextBox_Text;
                await Task.Delay(10);
                LightColorPicker.SelectedColor = new Color
                {
                    R = s.LightColorPicker_SelectedColor_R,
                    G = s.LightColorPicker_SelectedColor_G,
                    B = s.LightColorPicker_SelectedColor_B,
                    A = s.LightColorPicker_SelectedColor_A,
                };

                EnvironmentColorPicker.SelectedColor = new Color
                {
                    R = s.EnvironmentColorPicker_SelectedColor_R,
                    G = s.EnvironmentColorPicker_SelectedColor_G,
                    B = s.EnvironmentColorPicker_SelectedColor_B,
                    A = s.EnvironmentColorPicker_SelectedColor_A,
                };
                await Task.Delay(10);
                PostProcessingAntiAliasingEnableCheckBox.IsChecked = s.PostProcessingAntiAliasingEnableCheckBox_IsChecked_Value;
                PostProcessingBloomEnableCheckBox.IsChecked = s.PostProcessingBloomEnableCheckBox_IsChecked_Value;
                PostProcessingBloomIntensitySlider.Value = s.PostProcessingBloomIntensitySlider_Value;
                PostProcessingBloomThresholdSlider.Value = s.PostProcessingBloomThresholdSlider_Value;
                PostProcessingDoFEnableCheckBox.IsChecked = s.PostProcessingDoFEnableCheckBox_IsChecked_Value;
                PostProcessingDoFFocusDistanceSlider.Value = s.PostProcessingDoFFocusDistanceSlider_Value;
                PostProcessingDoFApertureSlider.Value = s.PostProcessingDoFApertureSlider_Value;
                PostProcessingDoFFocusLengthSlider.Value = s.PostProcessingDoFFocusLengthSlider_Value;
                PostProcessingDoFMaxBlurSizeSlider.Value = s.PostProcessingDoFMaxBlurSizeSlider_Value;
                PostProcessingCGEnableCheckBox.IsChecked = s.PostProcessingCGEnableCheckBox_IsChecked_Value;
                PostProcessingCGTemperatureSlider.Value = s.PostProcessingCGTemperatureSlider_Value;
                PostProcessingCGSaturationSlider.Value = s.PostProcessingCGSaturationSlider_Value;
                PostProcessingCGContrastSlider.Value = s.PostProcessingCGContrastSlider_Value;
                PostProcessingVEnableCheckBox.IsChecked = s.PostProcessingVEnableCheckBox_IsChecked_Value;
                PostProcessingVIntensitySlider.Value = s.PostProcessingVIntensitySlider_Value;
                PostProcessingVSmoothnessSlider.Value = s.PostProcessingVSmoothnessSlider_Value;
                PostProcessingVRoundedSlider.Value = s.PostProcessingVRoundedSlider_Value;
                PostProcessingCAEnableCheckBox.IsChecked = s.PostProcessingCAEnableCheckBox_IsChecked_Value;
                PostProcessingCAIntensitySlider.Value = s.PostProcessingCAIntensitySlider_Value;

                //ゲーミングは強制的にオフ
                GamingBackgroundCheckBox.IsChecked = false;
                GamingLightCheckBox.IsChecked = false;
                GamingEnvironmentCheckBox.IsChecked = false;

                //ポート反映
                EVMC4UEnableCheckBox.IsChecked = false; //設定有効化のため一旦切る
                await Task.Delay(500);
                EVMC4UEnableCheckBox.IsChecked = s.EVMC4UEnableCheckBox_IsChecked_Value;
            });
        }

        Setting SaveSetting()
        {
            Setting s = new Setting();
            s.LanguageComboBox_SelectedIndex = LanguageComboBox.SelectedIndex;
            s.VRMPathTextBox_Text = VRMPathTextBox.Text;
            s.BackgroundObjectPathTextBox_Text = BackgroundObjectPathTextBox.Text;
            s.CameraRotateXSlider_Value = CameraRotateXSlider.Value;
            s.CameraRotateYSlider_Value = CameraRotateYSlider.Value;
            s.CameraRotateZSlider_Value = CameraRotateZSlider.Value;
            s.CameraValue1Slider_Value = CameraValue1Slider.Value;
            s.CameraValue2Slider_Value = CameraValue2Slider.Value;
            s.CameraValue3Slider_Value = CameraValue3Slider.Value;
            s.LightRotateXSlider_Value = LightRotateXSlider.Value;
            s.LightRotateYSlider_Value = LightRotateYSlider.Value;
            s.LightRotateZSlider_Value = LightRotateZSlider.Value;
            s.LightValue1Slider_Value = LightValue1Slider.Value;
            s.LightValue2Slider_Value = LightValue2Slider.Value;
            s.LightValue3Slider_Value = LightValue3Slider.Value;
            s.LightTypeDirectionalRadioButton_IsChecked_Value = LightTypeDirectionalRadioButton.IsChecked.Value;
            s.LightTypePointRadioButton_IsChecked_Value = LightTypePointRadioButton.IsChecked.Value;
            s.LightTypeSpotRadioButton_IsChecked_Value = LightTypeSpotRadioButton.IsChecked.Value;
            s.BackgroundRotateXSlider_Value = BackgroundRotateXSlider.Value;
            s.BackgroundRotateYSlider_Value = BackgroundRotateYSlider.Value;
            s.BackgroundRotateZSlider_Value = BackgroundRotateZSlider.Value;
            s.BackgroundValue1Slider_Value = BackgroundValue1Slider.Value;
            s.BackgroundValue2Slider_Value = BackgroundValue2Slider.Value;
            s.BackgroundValue3Slider_Value = BackgroundValue3Slider.Value;
            s.BackgroundScaleSlider_Value = BackgroundScaleSlider.Value;
            s.BackgroundCameraTagetCheckBox_IsChecked_Value = BackgroundCameraTagetCheckBox.IsChecked.Value;
            s.BackgroundWindowCaptureCheckBox_IsChecked_Value = BackgroundWindowCaptureCheckBox.IsChecked.Value;
            s.BackgroundWindowTitleTextBox_Text = BackgroundWindowTitleTextBox.Text;

            s.EVMC4UEnableCheckBox_IsChecked_Value = EVMC4UEnableCheckBox.IsChecked.Value;
            s.EVMC4UPortTextBox_Text = EVMC4UPortTextBox.Text;
            s.EVMC4UFreezeCheckBox_IsChecked_Value = EVMC4UFreezeCheckBox.IsChecked.Value;
            s.EVMC4UBoneFilterCheckBox_IsChecked_Value = EVMC4UBoneFilterCheckBox.IsChecked.Value;
            s.EVMC4UBlendShapeFilterCheckBox_IsChecked_Value = EVMC4UBlendShapeFilterCheckBox.IsChecked.Value;
            s.EVMC4UBoneFilterValueTextBox_Text = EVMC4UBoneFilterValueTextBox.Text;
            s.EVMC4UBlendShapeFilterValueTextBox_Text = EVMC4UBlendShapeFilterValueTextBox.Text;
            s.WindowOptionWindowBorderCheckBox_IsChecked_Value = !WindowOptionWindowBorderCheckBox.IsChecked.Value; //反転
            s.WindowOptionForceForegroundCheckBox_IsChecked_Value = WindowOptionForceForegroundCheckBox.IsChecked.Value;
            s.WindowOptionTransparentCheckBox_IsChecked_Value = WindowOptionTransparentCheckBox.IsChecked.Value;
            s.CameraRootPosLockCheckBox_IsChecked_Value = CameraRootPosLockCheckBox.IsChecked.Value;
            s.LightRootPosLockCheckBox_IsChecked_Value = LightRootPosLockCheckBox.IsChecked.Value;
            s.BackgroundRootPosLockCheckBox_IsChecked_Value = BackgroundRootPosLockCheckBox.IsChecked.Value;
            s.OBSExternalControl_CheckBox_IsChecked_Value = OBSExternalControl_CheckBox.IsChecked.Value;
            s.UnityCaptureEnable_CheckBox_IsChecked_Value = UnityCaptureEnable_CheckBox.IsChecked.Value;
            //s.SEDSSServerPasswordTextBox_Password = SEDSSServerPasswordTextBox.Password;
            //s.SEDSSServerExchangeFilePathTextBox_Text = SEDSSServerExchangeFilePathTextBox.Text;
            //s.SEDSSClientAddressTextBox_Text = SEDSSClientAddressTextBox.Text;
            //s.SEDSSClientPortTextBox_Text = SEDSSClientPortTextBox.Text;
            //s.SEDSSClientPasswordTextBox_Password = SEDSSClientPasswordTextBox.Text;
            //s.SEDSSClientIDTextBox_Text = SEDSSClientIDTextBox.Text;
            //s.SEDSSClientUploadFilePathTextBox_Text = SEDSSClientUploadFilePathTextBox.Text;
            s.BackgroundColorPicker_SelectedColor_R = BackgroundColorPicker.SelectedColor.Value.R;
            s.BackgroundColorPicker_SelectedColor_G = BackgroundColorPicker.SelectedColor.Value.G;
            s.BackgroundColorPicker_SelectedColor_B = BackgroundColorPicker.SelectedColor.Value.B;
            s.BackgroundColorPicker_SelectedColor_A = BackgroundColorPicker.SelectedColor.Value.A;
            s.LightColorPicker_SelectedColor_R = LightColorPicker.SelectedColor.Value.R;
            s.LightColorPicker_SelectedColor_G = LightColorPicker.SelectedColor.Value.G;
            s.LightColorPicker_SelectedColor_B = LightColorPicker.SelectedColor.Value.B;
            s.LightColorPicker_SelectedColor_A = LightColorPicker.SelectedColor.Value.A;
            s.EnvironmentColorPicker_SelectedColor_R = EnvironmentColorPicker.SelectedColor.Value.R;
            s.EnvironmentColorPicker_SelectedColor_G = EnvironmentColorPicker.SelectedColor.Value.G;
            s.EnvironmentColorPicker_SelectedColor_B = EnvironmentColorPicker.SelectedColor.Value.B;
            s.EnvironmentColorPicker_SelectedColor_A = EnvironmentColorPicker.SelectedColor.Value.A;
            s.PostProcessingAntiAliasingEnableCheckBox_IsChecked_Value = PostProcessingAntiAliasingEnableCheckBox.IsChecked.Value;
            s.PostProcessingBloomEnableCheckBox_IsChecked_Value = PostProcessingBloomEnableCheckBox.IsChecked.Value;
            s.PostProcessingBloomIntensitySlider_Value = PostProcessingBloomIntensitySlider.Value;
            s.PostProcessingBloomThresholdSlider_Value = PostProcessingBloomThresholdSlider.Value;
            s.PostProcessingDoFEnableCheckBox_IsChecked_Value = PostProcessingDoFEnableCheckBox.IsChecked.Value;
            s.PostProcessingDoFFocusDistanceSlider_Value = PostProcessingDoFFocusDistanceSlider.Value;
            s.PostProcessingDoFApertureSlider_Value = PostProcessingDoFApertureSlider.Value;
            s.PostProcessingDoFFocusLengthSlider_Value = PostProcessingDoFFocusLengthSlider.Value;
            s.PostProcessingDoFMaxBlurSizeSlider_Value = PostProcessingDoFMaxBlurSizeSlider.Value;
            s.PostProcessingCGEnableCheckBox_IsChecked_Value = PostProcessingCGEnableCheckBox.IsChecked.Value;
            s.PostProcessingCGTemperatureSlider_Value = PostProcessingCGTemperatureSlider.Value;
            s.PostProcessingCGSaturationSlider_Value = PostProcessingCGSaturationSlider.Value;
            s.PostProcessingCGContrastSlider_Value = PostProcessingCGContrastSlider.Value;
            s.PostProcessingVEnableCheckBox_IsChecked_Value = PostProcessingVEnableCheckBox.IsChecked.Value;
            s.PostProcessingVIntensitySlider_Value = PostProcessingVIntensitySlider.Value;
            s.PostProcessingVSmoothnessSlider_Value = PostProcessingVSmoothnessSlider.Value;
            s.PostProcessingVRoundedSlider_Value = PostProcessingVRoundedSlider.Value;
            s.PostProcessingCAEnableCheckBox_IsChecked_Value = PostProcessingCAEnableCheckBox.IsChecked.Value;
            s.PostProcessingCAIntensitySlider_Value = PostProcessingCAIntensitySlider.Value;

            //ゲーミングは保存しない
            return s;
        }
    }
}
