using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace EasyDeviceDiscoveryProtocolClient
{
    public class Requester : MonoBehaviour
    {
        [Header("Settings")]
        public int discoverPort = 39500; //待受ポート

        [Header("Properties")]
        public string deivceName = "mydevice_client";//自分のデバイス名
        public int servicePort = 11111;//自分が使ってほしいと思っているポート

        [Header("Response Info(Read only)")]
        public string responseIpAddress = ""; //応答帰ってきたアドレス
        private int responsePort = 0; //応答帰ってきたポート
        public int foundDevices = 0; //見つかった台数

        [Header("Response Data(Read only)")]
        public string responseDeviceName = "";//データとして含まれるデバイス名
        public int responseServicePort = 0;//データとして含まれるポート

        [Header("Test")]
        public bool exec = false;//テスト実行


        public Action OnDeviceFound = null;

        UdpClient udpClient = new UdpClient();
        UTF8Encoding utf8 = new UTF8Encoding(false);

        public void StartDiscover(Action OnDeviceFound)
        {
            this.OnDeviceFound = OnDeviceFound;

            responseIpAddress = "";
            responsePort = 0;
            responseDeviceName = "";
            foundDevices = 0;

            string data = JsonUtility.ToJson(new RequestJson
            {
                servicePort = servicePort,
                deviceName = deivceName,
            });
            byte[] dat = utf8.GetBytes(data);

            udpClient.EnableBroadcast = true;
            udpClient.Send(dat, dat.Length, "255.255.255.255", discoverPort);
        }

        private void OnApplicationQuit()
        {
            udpClient.Close();
        }

        void Start()
        {

        }

        void Update()
        {
            if (exec) {
                exec = false;
                StartDiscover(() => { Debug.Log("Found"); });
            }

            if (udpClient != null)
            {
                while (udpClient.Available > 0)
                {
                    IPEndPoint point = new IPEndPoint(IPAddress.Any, discoverPort);
                    var r = udpClient.Receive(ref point);
                    var res = JsonUtility.FromJson<RequestJson>(utf8.GetString(r));

                    responseIpAddress = point.Address.ToString();
                    responsePort = point.Port;

                    responseDeviceName = res.deviceName;
                    responseServicePort = res.servicePort;

                    foundDevices++;
                    OnDeviceFound?.Invoke();
                }
            }
        }
    }
}