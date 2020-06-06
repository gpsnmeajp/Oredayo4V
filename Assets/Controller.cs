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
using System.IO;
using System.Text;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using akr.Unity.Windows;
using EVMC4U;
using UnityMemoryMappedFile;
using VRM;

/*
・あきらさんのグリーンバック(色可変)を載せる
・カメラアングルとかいじれるようにする
・fovは抑えめにする
・画面サイズ可変
・ズーム
・SEDSSクライアント・サーバー両方載せとく
*/

public class Controller : MonoBehaviour
{
    public ExternalReceiver externalReceiver;
    public CommunicationValidator communicationValidator;
    public WindowManagerEx windowManagerEx;

    public Transform cameraArm;
    public Camera camera_comp;
    public LookAtModel lookAtModel;

    public Transform lightArm;
    public Light light_comp;
    public LookAtModel lightLookAtModel;

    public Transform rootLockerTransform;
    public Loader loader;

    public PostProcessLayer PPSLayer;
    public PostProcessVolume PPSVolume;

    public UnityCapture unityCapture;

    PipeCommands.BackgroundObjectControl lastBackgroundPos = null;

    GameObject backgroundObject;

    SEDSS_Server sedss_server;
    SEDSS_Client sedss_client;
    MemoryMappedFileServer server;

    SynchronizationContext synchronizationContext;
    string backgroundObjectUrl = null;

    Color backgroundColor = new Color(0f,1f,0f);

    public RootLocker cameraRootLocker;
    public RootLocker lightRootLocker;
    public RootLocker backgroundRootLocker;

    string ServerExchangePath = null;
    Texture2D bgtexture = null;

    public DateTime startTime = DateTime.Now;

    void Start()
    {
        synchronizationContext = SynchronizationContext.Current;
        sedss_server = GetComponent<SEDSS_Server>();
        sedss_server.SetPassword(null);
        sedss_client = GetComponent<SEDSS_Client>();
        sedss_client.SetPassword(null);

        var uosc = externalReceiver.gameObject.GetComponent<uOSC.uOscServer>();
        uosc.enabled = false; //初期はオフにしておくことで起動時のポートを反映する

        server = new MemoryMappedFileServer();
        server.ReceivedEvent += Server_Received;
        server.Start("Oredayo_UI_Connection");

        Debug.Log(startTime);

        Application.logMessageReceived += ApplicationLogHandler;
        Application.logMessageReceivedThreaded += ApplicationLogHandler;
        Debug.Log("PipeServer started");

        synchronizationContext.Post(async (arg) =>
        {
            await server.SendCommandAsync(new PipeCommands.Hello { startTime = this.startTime });
        }, null);
    }

    private void OnApplicationQuit()
    {
        Application.logMessageReceived -= ApplicationLogHandler;
        Application.logMessageReceivedThreaded -= ApplicationLogHandler;
        sedss_server.StopServer();

        synchronizationContext.Post(async (arg) =>
        {
            await server.SendCommandAsync(new PipeCommands.Bye { });
            server.ReceivedEvent -= Server_Received;
            server.Stop();
        }, null);
    }

    private async void ApplicationLogHandler(string cond, string stack, LogType type)
    {
        synchronizationContext.Post(async (arg) =>
        {
            PipeCommands.LogType sendType = PipeCommands.LogType.Error;
            switch (type)
            {
                case LogType.Error: sendType = PipeCommands.LogType.Error; break;
                case LogType.Assert: sendType = PipeCommands.LogType.Error; break;
                case LogType.Exception: sendType = PipeCommands.LogType.Error; break;
                case LogType.Log: sendType = PipeCommands.LogType.Debug; break;
                case LogType.Warning: sendType = PipeCommands.LogType.Warning; break;
                default: break;
            }

            await server.SendCommandAsync(new PipeCommands.LogMessage
            {
                Message = cond,
                Detail = stack,
                Type = sendType,
            });
        }, null);
    }

    private void Server_Received(object sender, DataReceivedEventArgs e)
    {
        synchronizationContext.Post(async (arg) => {
            //-----------システム系----------------
            if (e.CommandType == typeof(PipeCommands.Hello))
            {
                //Helloが来たらHelloを返す。すると初期値が送られてくる。
                await server.SendCommandAsync(new PipeCommands.Hello { startTime = this.startTime });
                Debug.Log(">Hello");
            }
            else if (e.CommandType == typeof(PipeCommands.Bye))
            {
                //Unity側終了処理
                /*
#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
                */
                Debug.Log(">Bye");
            }
            //-----------基本設定----------------
            //===========VRMライセンス応答===========
            if (e.CommandType == typeof(PipeCommands.VRMLicenceAnser))
            {
                var d = (PipeCommands.VRMLicenceAnser)e.Data;
                if (d.Agree)
                {
                    loader.Agree();
                }
                else {
                    loader.DisAgree();
                }
            }

            //===========VRM読み込み===========
            else if (e.CommandType == typeof(PipeCommands.LoadVRM))
            {
                var d = (PipeCommands.LoadVRM)e.Data;
                Debug.Log("LoadVRM: " + d.filepath);

                //許諾画面を出す
                if (File.Exists(d.filepath)) {
                    loader.LoadRequest(d.filepath,(path,bytes)=> 
                    {
                        synchronizationContext.Post((args) =>
                        {
                            Debug.Log("Load start");
                            externalReceiver.LoadVRMFromData(bytes);
                        }, null);
                    });

                    //許諾応答要求を出す
                    await server.SendCommandAsync(new PipeCommands.VRMLicenceCheck{});
                }
            }

            //===========背景読み込み===========
            else if (e.CommandType == typeof(PipeCommands.LoadBackground))
            {
                var d = (PipeCommands.LoadBackground)e.Data;
                Debug.Log("LoadBackground: " + d.filepath);

                //TODO: 背景読み込み処理
                if (d.filepath != null && d.filepath != "")
                {
                    //VRMの場合
                    if (d.filepath.ToLower().EndsWith(".vrm"))
                    {
                        Destroy(backgroundObject);
                        bgtexture = null;
                        backgroundObject = null;
                        //ファイルからモデルを読み込む
                        //バイナリの読み込み
                        if (File.Exists(d.filepath))
                        {
                            //許諾画面を出す
                            loader.LoadRequest(d.filepath, (path, bytes) => {
                                //読み込み
                                VRMImporterContext vrmImporter = new VRMImporterContext();
                                vrmImporter.ParseGlb(bytes);

                                vrmImporter.LoadAsync(() =>
                                {
                                    GameObject Model = vrmImporter.Root;

                                    //backgroundObjectの下にぶら下げる
                                    backgroundObject = new GameObject();
                                    backgroundObject.transform.SetParent(rootLockerTransform, false);
                                    backgroundObject.name = "backgroundObject";

                                    //最後に設定されていた位置に設定
                                    if (lastBackgroundPos != null)
                                    {
                                        if (lastBackgroundPos.cameraTaget)
                                        {
                                            //カメラターゲット時は、カメラアームに
                                            backgroundObject.transform.SetParent(cameraArm, false);
                                        }
                                        else {
                                            //そうでないときは独立した位置に
                                            backgroundObject.transform.SetParent(rootLockerTransform, false);
                                        }
                                        backgroundObject.transform.localPosition = new Vector3(lastBackgroundPos.Px, lastBackgroundPos.Py, lastBackgroundPos.Pz);
                                        backgroundObject.transform.localRotation = Quaternion.Euler(lastBackgroundPos.Rx, lastBackgroundPos.Ry, lastBackgroundPos.Rz);
                                        backgroundObject.transform.localScale = new Vector3(lastBackgroundPos.scale, lastBackgroundPos.scale, lastBackgroundPos.scale);
                                    }
                                    //その下にモデルをぶら下げる
                                    Model.transform.SetParent(backgroundObject.transform, false);

                                    vrmImporter.EnableUpdateWhenOffscreen();
                                    vrmImporter.ShowMeshes();
                                });
                            });
                            //許諾応答要求を出す
                            await server.SendCommandAsync(new PipeCommands.VRMLicenceCheck { });
                        }
                        else
                        {
                            Debug.LogError("VRM load failed.");
                        }
                    }
                    //画像の場合
                    else if (d.filepath.ToLower().EndsWith(".png"))
                    {
                        Destroy(backgroundObject);
                        bgtexture = null;
                        backgroundObject = null;
                        if (File.Exists(d.filepath))
                        {
                            backgroundObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                            backgroundObject.transform.SetParent(rootLockerTransform, false);
                            //最後に設定されていた位置に設定
                            if (lastBackgroundPos != null)
                            {
                                if (lastBackgroundPos.cameraTaget)
                                {
                                    //カメラターゲット時は、カメラアームに
                                    backgroundObject.transform.SetParent(cameraArm, false);
                                }
                                else
                                {
                                    //そうでないときは独立した位置に
                                    backgroundObject.transform.SetParent(rootLockerTransform, false);
                                }
                                backgroundObject.transform.localPosition = new Vector3(lastBackgroundPos.Px, lastBackgroundPos.Py, lastBackgroundPos.Pz);
                                backgroundObject.transform.localRotation = Quaternion.Euler(lastBackgroundPos.Rx, lastBackgroundPos.Ry, lastBackgroundPos.Rz);
                            }

                            backgroundObjectUrl = "file://" + d.filepath;
                            StartCoroutine("LoadTexture");
                        }
                    }
                    else
                    {
                        await server.SendCommandAsync(new PipeCommands.SendMessage { Message = "その背景は対応していません" });
                    }
                }

            }
            else if (e.CommandType == typeof(PipeCommands.RemoveBackground))
            {
                Destroy(backgroundObject);
                backgroundObject = null;
            }


            //===========カメラ位置===========
            else if (e.CommandType == typeof(PipeCommands.CameraControl))
            {
                var d = (PipeCommands.CameraControl)e.Data;

                lookAtModel.zaxis = d.Rz;
                lookAtModel.height = d.Height;
                cameraArm.localPosition = new Vector3(0, d.Height, 0);
                cameraArm.localRotation = Quaternion.Euler(d.Rx, d.Ry, 0);

                camera_comp.transform.localPosition = new Vector3(0, 0, d.Zoom);
                camera_comp.fieldOfView = d.Fov;

                //ライト連動 //カメラのを持ってくる
                lightArm.localPosition = new Vector3(0, lookAtModel.height, 0);
                lightLookAtModel.height = lookAtModel.height;
                light_comp.transform.localPosition = new Vector3(0, 0, light_comp.transform.localPosition.z);

            }


            //===========ライト位置===========
            else if (e.CommandType == typeof(PipeCommands.LightControl))
            {
                var d = (PipeCommands.LightControl)e.Data;

                lightLookAtModel.height = lookAtModel.height; //カメラのを持ってくる

                lightLookAtModel.zaxis = d.Rz;
                lightArm.localPosition = new Vector3(0, lookAtModel.height, 0);

                light_comp.transform.localPosition = new Vector3(0, 0, d.Distance);
                light_comp.range = d.Range;
                light_comp.spotAngle = d.SpotAngle;

                switch (d.Type)
                {
                    case PipeCommands.LightType.Directional:
                        lightArm.localRotation = Quaternion.Euler(0, 0, 0);
                        light_comp.transform.localRotation = Quaternion.Euler(d.Rx, d.Ry, d.Rz);
                        lightLookAtModel.enabled = false;
                        light_comp.type = LightType.Directional;
                        break;
                    case PipeCommands.LightType.Point:
                        lightArm.localRotation = Quaternion.Euler(d.Rx, d.Ry, 0);
                        lightLookAtModel.enabled = true;
                        light_comp.type = LightType.Point;
                        break;
                    case PipeCommands.LightType.Spot:
                        lightArm.localRotation = Quaternion.Euler(d.Rx, d.Ry, 0);
                        lightLookAtModel.enabled = true;
                        light_comp.type = LightType.Spot;
                        break;
                    default:
                        lightArm.localRotation = Quaternion.Euler(0, 0, 0);
                        light_comp.transform.localRotation = Quaternion.Euler(d.Rx, d.Ry, d.Rz);
                        lightLookAtModel.enabled = false;
                        light_comp.type = LightType.Directional;
                        break;
                }
            }

            //===========背景オブジェクト位置===========
            else if (e.CommandType == typeof(PipeCommands.BackgroundObjectControl))
            {
                var d = (PipeCommands.BackgroundObjectControl)e.Data;
                lastBackgroundPos = d;

                if (backgroundObject != null)
                {
                    if (d.cameraTaget)
                    {
                        //カメラターゲット時は、カメラアームに
                        backgroundObject.transform.SetParent(cameraArm, false);
                    }
                    else
                    {
                        //そうでないときは独立した位置に
                        backgroundObject.transform.SetParent(rootLockerTransform, false);
                    }

                    backgroundObject.transform.localPosition = new Vector3(d.Px, d.Py, d.Pz);
                    backgroundObject.transform.localRotation = Quaternion.Euler(d.Rx, d.Ry, d.Rz);

                    if (bgtexture != null)
                    {
                        backgroundObject.transform.localScale = new Vector3(d.scale, d.scale* (float)bgtexture.height / (float)bgtexture.width, d.scale);
                    }
                    else {
                        backgroundObject.transform.localScale = new Vector3(d.scale, d.scale, d.scale);
                    }
                }
            }

            //-----------詳細設定----------------
            //===========EVMC4U===========
            else if (e.CommandType == typeof(PipeCommands.EVMC4UControl))
            {
                var d = (PipeCommands.EVMC4UControl)e.Data;

                var uosc = externalReceiver.gameObject.GetComponent<uOSC.uOscServer>();
                //uOSCのポートを書き換え
                FieldInfo fieldInfo = typeof(uOSC.uOscServer).GetField("port", BindingFlags.NonPublic | BindingFlags.Instance);
                fieldInfo.SetValue(uosc,d.Port);
                uosc.enabled = d.Enable;

                externalReceiver.Freeze = d.Freeze;
                externalReceiver.BoneFilter = d.BoneFilterValue;
                externalReceiver.BonePositionFilterEnable = d.BoneFilterEnable;
                externalReceiver.BoneRotationFilterEnable = d.BoneFilterEnable;

                externalReceiver.BlendShapeFilter = d.BlendShapeFilterValue;
                externalReceiver.BlendShapeFilterEnable = d.BlendShapeFilterEnable;
            }
            else if (e.CommandType == typeof(PipeCommands.EVMC4UTakePhotoCommand))
            {
                var d = (PipeCommands.EVMC4UTakePhotoCommand)e.Data;
                GetComponent<HiResolutionPhotoCamera>().shot = true;

            }
            //===========Window===========
            else if (e.CommandType == typeof(PipeCommands.WindowControl))
            {
                var d = (PipeCommands.WindowControl)e.Data;

                windowManagerEx.SetThroughMouseClick(d.Transparent);
                windowManagerEx.SetWindowBackgroundTransparent(d.Transparent, backgroundColor);

                windowManagerEx.SetWindowBorder(d.NoBorder);
                windowManagerEx.SetWindowAlwaysTopMost(d.ForceForeground);

            }

            //===========Root位置===========
            else if (e.CommandType == typeof(PipeCommands.RootPositionControl))
            {
                var d = (PipeCommands.RootPositionControl)e.Data;
                cameraRootLocker.Lock = d.CameraLock;
                lightRootLocker.Lock = d.LightLock;
                backgroundRootLocker.Lock = d.BackgroundLock;
            }
            //===========外部連携===========
            else if (e.CommandType == typeof(PipeCommands.ExternalControl))
            {
                var d = (PipeCommands.ExternalControl)e.Data;
                //TODO: OBS連動など
            }
            //===========仮想Webカメラ===========
            else if (e.CommandType == typeof(PipeCommands.VirtualWebCamera))
            {
                var d = (PipeCommands.VirtualWebCamera)e.Data;
                unityCapture.enabled = d.Enable;
            }

            //===========SEDSSサーバー===========
            else if (e.CommandType == typeof(PipeCommands.SEDSSServerControl))
            {
                var d = (PipeCommands.SEDSSServerControl)e.Data;

                sedss_server.SetPassword(d.Password);

                //サーバー起動終了
                if (d.Enable)
                {
                    ServerExchangePath = d.ExchangeFilePath;
                    sedss_server.StartServer();
                    Debug.Log("[SEDSS Server] サーバー起動");
                }
                else {
                    ServerExchangePath = null;
                    sedss_server.StopServer();
                    Debug.Log("[SEDSS Server] サーバー停止");
                }

                sedss_server.OnDataUploaded = (bytes, id) => {
                    Debug.Log("[SEDSS Server] アップロードを受けました");
                    externalReceiver.LoadVRMFromData(bytes);
                    return;
                };
                sedss_server.OnDownloadRequest = (id) =>
                {
                    if (File.Exists(d.ExchangeFilePath))
                    {
                        Debug.Log("[SEDSS Server] ダウンロード要求を受けました");
                        byte[] data = File.ReadAllBytes(ServerExchangePath);
                        return data;
                    }
                    else {
                        Debug.LogError("[SEDSS Server] ダウンロード要求を受けましたがファイルが存在しません");
                        return new byte[0];
                    }
                };
            }

            //===========SEDSSクライアント===========
            else if (e.CommandType == typeof(PipeCommands.SEDSSClientRequestCommand))
            {
                var d = (PipeCommands.SEDSSClientRequestCommand)e.Data;

                sedss_client.SetPassword(d.Password);
                sedss_client.SetAddress(d.Address);

                int port = 0;
                if (int.TryParse(d.Port, out port))
                {
                    sedss_client.port = port;

                    if (d.RequestType == PipeCommands.SEDSS_RequestType.Downdload)
                    {
                        sedss_client.Download(d.ID, (bytes, id) => {
                            Debug.Log("[SEDSS Client] ダウンロード完了");
                            externalReceiver.LoadVRMFromData(bytes);
                        }, (err, id) =>
                        {
                            Debug.LogError("[SEDSS Client] ダウンロード失敗(通信異常,パスワード誤り)");
                        });
                    }
                    else if (d.RequestType == PipeCommands.SEDSS_RequestType.Upload)
                    {
                        if (File.Exists(d.UploadFilePath))
                        {
                            byte[] data = File.ReadAllBytes(d.UploadFilePath);
                            sedss_client.Upload(data, d.ID, (id) =>
                            {
                                Debug.Log("[SEDSS Client] アップロード完了");
                            }, (err, id) =>
                            {
                                Debug.LogError("[SEDSS Client] アップロード失敗(通信異常,パスワード誤り)");
                            });
                        }
                        else {
                            Debug.LogError("アップロードファイルが存在しません");
                        }
                    }
                    else
                    {
                        Debug.LogError("不正なリクエスト");
                    }
                }
                else {
                    Debug.LogError("不正なポート番号");
                }
            }

            //-----------色設定----------------
            //===========背景色===========
            else if (e.CommandType == typeof(PipeCommands.BackgrounColor))
            {
                var d = (PipeCommands.BackgrounColor)e.Data;
                Color c = new Color(d.r / 255f, d.g / 255f, d.b / 255f);
                backgroundColor = c;
                windowManagerEx.SetWindowBackgroundTransparent(false, c);
            }

            //===========ライト色===========
            else if (e.CommandType == typeof(PipeCommands.LightColor))
            {
                var d = (PipeCommands.LightColor)e.Data;
                light_comp.color = new Color(d.r / 255f, d.g / 255f, d.b / 255f);
            }
            //===========環境光===========
            else if (e.CommandType == typeof(PipeCommands.EnvironmentColor))
            {
                var d = (PipeCommands.EnvironmentColor)e.Data;
                RenderSettings.ambientLight = new Color(d.r / 255f, d.g / 255f, d.b / 255f);
            }
            //===========PostProcessing===========
            else if (e.CommandType == typeof(PipeCommands.PostProcessingControl))
            {
                var d = (PipeCommands.PostProcessingControl)e.Data;

                //アンチエイリアス
                if (d.AntiAliasingEnable)
                {
                    PPSLayer.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
                }
                else {
                    PPSLayer.antialiasingMode = PostProcessLayer.Antialiasing.None;
                }

                var p = PPSVolume.sharedProfile;

                //ブルーム
                var bloom = p.GetSetting<Bloom>();
                bloom.active = true;
                bloom.enabled.value = d.BloomEnable;
                bloom.intensity.value = d.BloomIntensity;
                bloom.threshold.value = d.BloomThreshold;

                //DoF
                var dof = p.GetSetting<DepthOfField>();
                dof.active = true;
                dof.enabled.value = d.DoFEnable;
                dof.focusDistance.value = d.DoFFocusDistance;
                dof.aperture.value = d.DoFAperture;
                dof.focalLength.value = d.DoFFocusLength;
                switch (d.DoFMaxBlurSize) {
                    case 1:
                        dof.kernelSize.value = KernelSize.Small; break;
                    case 2:
                        dof.kernelSize.value = KernelSize.Medium; break;
                    case 3:
                        dof.kernelSize.value = KernelSize.Large; break;
                    case 4:
                        dof.kernelSize.value = KernelSize.VeryLarge; break;
                    default:
                        dof.kernelSize.value = KernelSize.Medium; break;
                }

                //CG
                var cg = p.GetSetting<ColorGrading>();
                cg.active = true;
                cg.enabled.value = d.CGEnable;
                cg.temperature.value = d.CGTemperature;
                cg.saturation.value = d.CGSaturation;
                cg.contrast.value = d.CGContrast;

                var v = p.GetSetting<Vignette>();
                v.active = true;
                v.enabled.value = d.VEnable;
                v.intensity.value = d.VIntensity;
                v.smoothness.value = d.VSmoothness;
                v.roundness.value = d.VRounded;

                var ca = p.GetSetting<ChromaticAberration>();
                ca.active = true;
                ca.enabled.value = d.CAEnable;
                ca.intensity.value = d.CAIntensity;


                PPSVolume.sharedProfile = p;
                
            }
            else
            {
                //未対応のなにか
            }

        }, null);
    }

    float nextTime = 0;
    float lastEVMC4UTime = 0;
    int failCount = 0;
    void Update()
    {
        //KeepAlive 
        if (Time.time > nextTime)
        {
            synchronizationContext.Post(async (arg) =>
            {
                await server.SendCommandAsync(new PipeCommands.CyclicStatus
                {
                    EVMC4U = failCount<5,
                    HeadHeight = externalReceiver.HeadPosition.y, //カメラ位置用に送り続ける
                });
            }, null);

            //通信が行われていれば常に時刻は更新される
            if (communicationValidator.time == lastEVMC4UTime)
            {
                failCount++;
            }
            else {
                failCount = 0;
            }

            lastEVMC4UTime = communicationValidator.time;
            nextTime = Time.time + 0.5f; //500ms周期
        }
    }

    IEnumerator LoadTexture()
    {
        using (WWW www = new WWW(backgroundObjectUrl))
        {
            yield return www;
            if (backgroundObject != null)
            {
                Renderer renderer = backgroundObject.GetComponent<Renderer>();
                renderer.material.shader = Shader.Find("Unlit/Texture");

                //高画質化処理
                bgtexture = www.texture;
                bgtexture.anisoLevel = 16;
                bgtexture.filterMode = FilterMode.Trilinear;
                renderer.material.mainTexture = bgtexture;

                backgroundObject.transform.localScale = new Vector3(lastBackgroundPos.scale, lastBackgroundPos.scale * (float)bgtexture.height / (float)bgtexture.width, lastBackgroundPos.scale);
            }
        }
    }
}
