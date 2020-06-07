using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyDeviceDiscoveryProtocolClient
{
    public class Responder : MonoBehaviour
    {
        [Header("Settings")]
        public int discoverPort = 39500; //待受ポート

        [Header("Properties")]
        public string deivceName = "mydevice_server"; //自分のデバイス名
        public int servicePort = 22222; //自分が使ってほしいと思っているポート

        [Header("Request Info(Read only)")]
        public string requestIpAddress = ""; //要求来たアドレス
        private int requestPort = 0; //要求来たポート

        [Header("Request Data(Read only)")]
        public string requestDeviceName = ""; //要求に含まれるデバイス名
        public int requestServicePort = 0; //要求に含まれるポート

        UdpClient udpClient;
        UTF8Encoding utf8 = new UTF8Encoding(false);

        public Action OnRequested = () => { Debug.Log("Request got"); };

        void Start()
        {
            udpClient = new UdpClient(39500);
        }

        private void OnApplicationQuit()
        {
            udpClient.Close();
        }

        void Update()
        {
            if (udpClient != null)
            {
                while (udpClient.Available > 0)
                {
                    IPEndPoint point = new IPEndPoint(IPAddress.Any, discoverPort); //待受ポート兼応答先(変化後)
                    var r = udpClient.Receive(ref point);
                    var req = JsonUtility.FromJson<RequestJson>(utf8.GetString(r));

                    //要求内容を表示
                    requestIpAddress = point.Address.ToString();
                    requestPort = point.Port;

                    requestDeviceName = req.deviceName;
                    requestServicePort = req.servicePort;

                    //応答を送信
                    string data = JsonUtility.ToJson(new RequestJson {
                        servicePort = servicePort,
                        deviceName = deivceName,
                    });
                    byte[] dat = utf8.GetBytes(data);
                    udpClient.Send(dat, dat.Length, point);

                    OnRequested?.Invoke();
                }
            }

        }
    }
}