using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EasyDeviceDiscoveryProtocolClient
{
    [SerializeField]
    public class RequestJson {
        public const int protocolVersion = 1;
        //---------------------------------------------
        public int servicePort = 0;
        public string deviceName = "";
        public int version = 0; //初期値は無印版
    }
}
