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
using UnityEngine;
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

    PipeCommands.BackgroundObjectControl lastBackgroundPos = null;

    GameObject backgroundObject;

    SEDSS_Server sedss_server;
    MemoryMappedFileServer server;

    SynchronizationContext synchronizationContext;
    string backgroundObjectUrl = null;

    Color backgroundColor = new Color(0f,1f,0f);

    public RootLocker cameraRootLocker;
    public RootLocker lightRootLocker;
    public RootLocker backgroundRootLocker;

    async void Start()
    {
        synchronizationContext = SynchronizationContext.Current;
        sedss_server = new SEDSS_Server();

        server = new MemoryMappedFileServer();
        server.ReceivedEvent += Server_Received;
        server.Start("Oredayo_UI_Connection");

        await server.SendCommandAsync(new PipeCommands.Hello { });
        Application.logMessageReceived += ApplicationLogHandler;
        Debug.Log("Server started");
    }

    private async void OnApplicationQuit()
    {
        Application.logMessageReceived -= ApplicationLogHandler;
        await server.SendCommandAsync(new PipeCommands.Bye { });

        server.ReceivedEvent -= Server_Received;
        server.Stop();
    }

    private async void ApplicationLogHandler(string cond, string stack, LogType type)
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
    }

    private async void Server_Received(object sender, DataReceivedEventArgs e)
    {
        synchronizationContext.Post(async (arg) => {
            //-----------システム系----------------
            if (e.CommandType == typeof(PipeCommands.Hello))
            {
                //Helloが来たらHelloを返す。すると初期値が送られてくる。
                await server.SendCommandAsync(new PipeCommands.Hello { });
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
            //===========VRM読み込み===========
            if (e.CommandType == typeof(PipeCommands.LoadVRM))
            {
                var d = (PipeCommands.LoadVRM)e.Data;
                Debug.Log("LoadVRM: " + d.filepath);
                externalReceiver.LoadVRM(d.filepath);
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
                    if (d.filepath.ToLower().EndsWith(".vrm") || d.filepath.ToLower().EndsWith(".glb"))
                    {
                        Destroy(backgroundObject);
                        backgroundObject = null;
                        //ファイルからモデルを読み込む
                        //バイナリの読み込み
                        if (File.Exists(d.filepath))
                        {
                            byte[] VRMdata = File.ReadAllBytes(d.filepath);

                            //読み込み
                            VRMImporterContext vrmImporter = new VRMImporterContext();
                            vrmImporter.ParseGlb(VRMdata);

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
                                    backgroundObject.transform.localPosition = new Vector3(lastBackgroundPos.Px, lastBackgroundPos.Py, lastBackgroundPos.Pz);
                                    backgroundObject.transform.localRotation = Quaternion.Euler(lastBackgroundPos.Rx, lastBackgroundPos.Ry, lastBackgroundPos.Rz);
                                }
                                //その下にモデルをぶら下げる
                                Model.transform.SetParent(backgroundObject.transform, false);

                                vrmImporter.EnableUpdateWhenOffscreen();
                                vrmImporter.ShowMeshes();
                            });
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
                        backgroundObject = null;

                        backgroundObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
                        backgroundObject.transform.SetParent(rootLockerTransform, false);
                        //最後に設定されていた位置に設定
                        if (lastBackgroundPos != null)
                        {
                            backgroundObject.transform.localPosition = new Vector3(lastBackgroundPos.Px, lastBackgroundPos.Py, lastBackgroundPos.Pz);
                            backgroundObject.transform.localRotation = Quaternion.Euler(lastBackgroundPos.Rx, lastBackgroundPos.Ry, lastBackgroundPos.Rz);
                        }

                        backgroundObjectUrl = "file://" + d.filepath;
                        StartCoroutine("LoadTexture");
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
                if (backgroundObject != null)
                {
                    var d = (PipeCommands.BackgroundObjectControl)e.Data;
                    lastBackgroundPos = d;
                    backgroundObject.transform.localPosition = new Vector3(d.Px, d.Py, d.Pz);
                    backgroundObject.transform.localRotation = Quaternion.Euler(d.Rx, d.Ry, d.Rz);
                }
            }

            //-----------詳細設定----------------
            //===========EVMC4U===========
            else if (e.CommandType == typeof(PipeCommands.EVMC4UControl))
            {
                var d = (PipeCommands.EVMC4UControl)e.Data;
                externalReceiver.Freeze = d.Freeze;
                externalReceiver.BoneFilter = d.BoneFilterValue;
                externalReceiver.BonePositionFilterEnable = d.BoneFilterEnable;
                externalReceiver.BoneRotationFilterEnable = d.BoneFilterEnable;

                //TODO: BlendShapeFilter
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

            //===========SEDSSサーバー===========
            else if (e.CommandType == typeof(PipeCommands.SEDSSServerControl))
            {
                var d = (PipeCommands.SEDSSServerControl)e.Data;
                //TODO: SEDSSサーバー設定
            }

            //===========SEDSSクライアント===========
            else if (e.CommandType == typeof(PipeCommands.SEDSSClientRequestCommand))
            {
                var d = (PipeCommands.SEDSSClientRequestCommand)e.Data;
                //TODO: SEDSSクライアントリクエスト
            }

            //-----------色設定----------------
            //===========背景色===========
            else if (e.CommandType == typeof(PipeCommands.BackgrounColor))
            {
                var d = (PipeCommands.BackgrounColor)e.Data;
                windowManagerEx.SetWindowBackgroundTransparent(false, new Color(d.r / 255f, d.g / 255f, d.b / 255f));
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
            else
            {
                //未対応のなにか
            }

        }, null);
    }

    float nextTime = 0;
    float lastEVMC4UTime = 0;
    async void Update()
    {
        //KeepAlive 
        if (Time.time > nextTime)
        {
            //生きているということだけ送る
            await server.SendCommandAsync(new PipeCommands.KeepAlive { });

            await server.SendCommandAsync(new PipeCommands.CommunicationStatus
            {
                EVMC4U = communicationValidator.time != lastEVMC4UTime //通信が行われていれば常に時刻は更新される
            });

            lastEVMC4UTime = communicationValidator.time;
            nextTime = Time.time + 1.5f;
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
                Texture2D texture = www.texture;
                texture.anisoLevel = 16;
                texture.filterMode = FilterMode.Trilinear;
                renderer.material.mainTexture = texture;

                backgroundObject.transform.localScale = new Vector3(1f, (float)texture.height/ (float)texture.width, 0);
            }
        }
    }
}
