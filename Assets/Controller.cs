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

public class Controller : MonoBehaviour
{
    private MainThreadInvoker mainThreadInvoker;
    public ExternalReceiver externalReceiver;
    public WindowManagerEx windowManagerEx;
    MemoryMappedFileServer server;

    void Start()
    {
        windowManagerEx = GetComponent<WindowManagerEx>();
        externalReceiver = GetComponent<ExternalReceiver>();

        server = new MemoryMappedFileServer();
        server.ReceivedEvent += Server_Received;
        server.Start("SamplePipeName");
    }

    private void OnApplicationQuit()
    {
        server.ReceivedEvent -= Server_Received;
        server.Stop();
    }

    private async void Server_Received(object sender, DataReceivedEventArgs e)
    {
        if (e.CommandType == typeof(PipeCommands.SendMessage))
        {
            var d = (PipeCommands.MoveObject)e.Data;
            mainThreadInvoker.BeginInvoke(() => //別スレッドからGameObjectに触るときはメインスレッドで処理すること
            {

            });
            await server.SendCommandAsync(new PipeCommands.ReturnCurrentPosition { CurrentX = x }, e.RequestId);
        }
    }

    async void Update()
    {
        
    }
}
