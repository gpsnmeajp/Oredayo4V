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
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using akr.Unity.Windows;
using EVMC4U;
using UnityMemoryMappedFile;

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
    public MainThreadInvoker mainThreadInvoker;
    public ExternalReceiver externalReceiver;
    public WindowManagerEx windowManagerEx;

    public Transform cameraArm;
    public Transform camera;

    SEDSS_Server sedss_server;
    MemoryMappedFileServer server;

    void Start()
    {
        sedss_server = new SEDSS_Server();

        server = new MemoryMappedFileServer();
        server.ReceivedEvent += Server_Received;
        server.Start("Oredayo_UI_Connection");
    }

    private void OnApplicationQuit()
    {
        server.ReceivedEvent -= Server_Received;
        server.Stop();
    }

    private async void Server_Received(object sender, DataReceivedEventArgs e)
    {
        /*
        if (e.CommandType == typeof(PipeCommands.LoadVRM))
        {
            var d = (PipeCommands.LoadVRM)e.Data;
            Debug.Log("LoadVRM: " + d.filepath);


            mainThreadInvoker.BeginInvoke(() => //別スレッドからGameObjectに触るときはメインスレッドで処理すること
            {
                externalReceiver.LoadVRM(d.filepath);
            });
        }
        if (e.CommandType == typeof(PipeCommands.BackgrounColor))
        {
            var d = (PipeCommands.BackgrounColor)e.Data;

            mainThreadInvoker.BeginInvoke(() => //別スレッドからGameObjectに触るときはメインスレッドで処理すること
            {
                windowManagerEx.SetWindowBackgroundTransparent(false, new Color(d.r / 255f, d.g / 255f, d.b / 255f));
            });
        }
        if (e.CommandType == typeof(PipeCommands.CameraPos))
        {
            var d = (PipeCommands.CameraPos)e.Data;

            mainThreadInvoker.BeginInvoke(() => //別スレッドからGameObjectに触るときはメインスレッドで処理すること
            {
                cameraArm.localRotation = Quaternion.Euler(0, d.rotate-180f, 0);
                camera.localPosition = new Vector3(0, 0, d.zoom);
                cameraArm.localPosition = new Vector3(0, d.height, 0);
            });
        }
        */
    }

    async void Update()
    {
        
    }
}
