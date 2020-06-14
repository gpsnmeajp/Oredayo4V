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
        public int requestProtocolVersion = 0; //要求のプロトコルバージョン

        [Header("Request Data(Read only)")]
        public string requestDeviceName = ""; //要求に含まれるデバイス名
        public int requestServicePort = 0; //要求に含まれるポート

        UdpClient udpClient;
        UTF8Encoding utf8 = new UTF8Encoding(false);

        public Action OnRequested = () => { Debug.Log("[EDDP Responder]On Request"); };

        //UDP通信の準備を行います
        void TryOpen()
        {
            //GameObjectが有効なときだけ開始する
            if (isActiveAndEnabled)
            {
                if (udpClient == null)
                {
                    udpClient = new UdpClient(discoverPort);
                    udpClient.EnableBroadcast = true; //ブロードキャスト有効
                    udpClient.MulticastLoopback = true; //ループバック許可
                    Debug.Log("[EDDP Responder]UdpClient Open " + discoverPort);
                }
            }
        }

        //UDP通信の停止を行います。
        void Close()
        {
            if (udpClient != null)
            {
                try
                {
                    udpClient?.Close();
                    Debug.Log("[EDDP Responder]UdpClient Closed");
                }
                finally
                {
                    udpClient = null;
                }
            }
        }

        //GameObjectがEnableになったとき開く
        private void OnEnable()
        {
            TryOpen();
        }

        //GameObjectがDisableになったとき閉じる
        private void OnDisable()
        {
            Close();
        }

        //GameObjectが破棄されるとき閉じる
        private void OnDestroy()
        {
            Close();
        }

        //アプリケーションが終了するとき閉じる
        private void OnApplicationQuit()
        {
            Close();
        }

        //アプリケーションが中断・復帰したとき(モバイルでバックグラウンドになった・エディタで別のフォーカスを当てられたとき)
        private void OnApplicationPause(bool pause)
        {
            if (pause)
            {
                //アプリが閉じられたら止める
                Close();
            }
            else
            {
                //アプリが開かれたので開く
                TryOpen();
            }
        }

        void Update()
        {
            if (udpClient != null)
            {
                while (udpClient.Available > 0)
                {
                    //応答を受信
                    IPEndPoint point = new IPEndPoint(IPAddress.Any, discoverPort); //待受ポート兼応答先(変化後)
                    var r = udpClient.Receive(ref point);
                    var req = JsonUtility.FromJson<RequestJson>(utf8.GetString(r));

                    //要求内容を表示
                    requestIpAddress = point.Address.ToString();
                    requestPort = point.Port;
                    requestProtocolVersion = req.version;

                    requestDeviceName = req.deviceName;
                    requestServicePort = req.servicePort;

                    //応答を送信
                    string data = JsonUtility.ToJson(new RequestJson {
                        servicePort = servicePort,
                        deviceName = deivceName,
                        version = RequestJson.protocolVersion,
                    });
                    byte[] dat = utf8.GetBytes(data);
                    udpClient.Send(dat, dat.Length, point);

                    //コールバック送付
                    OnRequested?.Invoke();
                }
            }

        }
    }
}