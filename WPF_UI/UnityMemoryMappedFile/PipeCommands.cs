using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace UnityMemoryMappedFile
{
    public class PipeCommands
    {
        public static Type GetCommandType(string commandStr)
        {
            var commands = typeof(PipeCommands).GetNestedTypes(System.Reflection.BindingFlags.Public);
            foreach (var command in commands)
            {
                if (command.Name == commandStr) return command;
            }
            return null;
        }

        public enum LightType{ 
            Directional,
            Point,
            Spot,
        };
        public enum SEDSS_RequestType
        {
            Upload,
            Downdload,
        };
        public enum LogType
        {
            Error,
            Warning,
            Debug,
        };

        //Each
        public class Hello{ 
            public DateTime startTime { get; set; }
        }
        public class Bye{ }

        //To Unity
        //★基本設定画面
        public class LoadVRM
        {
            public string filepath { get; set; }
        }
        public class LoadBackground
        {
            public string filepath { get; set; }
        }
        public class RemoveBackground
        {
        }

        public class CameraControl
        {
            public float Rx { get; set; }
            public float Ry { get; set; }
            public float Rz { get; set; }
            public float Zoom { get; set; }
            public float Height { get; set; }
            public float Fov { get; set; }
        }

        public class LightControl
        {
            public float Rx { get; set; }
            public float Ry { get; set; }
            public float Rz { get; set; }
            public float Distance { get; set; }
            public float Range { get; set; }
            public float SpotAngle { get; set; }
            public LightType Type { get; set; }
        }
        public class BackgroundObjectControl
        {
            public float Rx { get; set; }
            public float Ry { get; set; }
            public float Rz { get; set; }
            public float Px { get; set; }
            public float Py { get; set; }
            public float Pz { get; set; }
            public float scale { get; set; }
            public bool cameraTaget { get; set; }
        }

        //★詳細設定
        public class EVMC4UControl
        {
            public bool Enable { get; set; }
            public int Port { get; set; }
            public bool Freeze { get; set; }
            public bool BoneFilterEnable { get; set; }
            public float BoneFilterValue { get; set; }
            public bool BlendShapeFilterEnable { get; set; }
            public float BlendShapeFilterValue { get; set; }
        }

        public class EVMC4UTakePhotoCommand
        {
            //Command
        }

        public class WindowControl
        {
            public bool ForceForeground { get; set; }
            public bool Transparent { get; set; }
            public bool NoBorder { get; set; }
        }

        public class RootPositionControl
        {
            public bool CameraLock { get; set; }
            public bool LightLock { get; set; }
            public bool BackgroundLock { get; set; }
        }
        public class ExternalControl
        {
            public bool OBS { get; set; }
        }
        public class VirtualWebCamera
        {
            public bool Enable { get; set; }
        }
        public class SEDSSServerControl
        {
            public bool Enable { get; set; }
            public string Password { get; set; }
            public string ExchangeFilePath { get; set; }
        }
        public class SEDSSClientRequestCommand
        {
            public SEDSS_RequestType RequestType { get; set; }
            public string Address { get; set; }
            public string Port { get; set; }
            public string ID { get; set; }
            public string Password { get; set; }
            public string UploadFilePath { get; set; }
        }

        //★色設定

        public class BackgrounColor
        {
            public float r { get; set; }
            public float g { get; set; }
            public float b { get; set; }
        }
        public class LightColor
        {
            public float r { get; set; }
            public float g { get; set; }
            public float b { get; set; }
        }
        public class EnvironmentColor
        {
            public float r { get; set; }
            public float g { get; set; }
            public float b { get; set; }
        }

        //★画質設定
        /*
        public class UnityGraphicsControl
        {

        }
        */
        public class PostProcessingControl
        {
            public bool AntiAliasingEnable { get; set; }
            public bool BloomEnable { get; set; }
            public float BloomIntensity { get; set; }
            public float BloomThreshold { get; set; }
            public bool DoFEnable { get; set; }
            public float DoFFocusDistance { get; set; }
            public float DoFAperture { get; set; }
            public float DoFFocusLength { get; set; }
            public int DoFMaxBlurSize { get; set; }
            public bool CGEnable { get; set; }
            public float CGTemperature { get; set; }
            public float CGSaturation { get; set; }
            public float CGContrast { get; set; }
            public bool VEnable { get; set; }
            public float VIntensity { get; set; }
            public float VSmoothness { get; set; }
            public float VRounded { get; set; }
            public bool CAEnable { get; set; }
            public float CAIntensity { get; set; }
        }

        public class VRMLicenceCheck{}


        //From Unity
        public class LogMessage
        {
            public LogType Type { get; set; }
            public string Message { get; set; }
            public string Detail { get; set; }

        }
        public class SendMessage
        {
            public string Message { get; set; }
        }
        public class CyclicStatus
        {
            public bool EVMC4U { get; set; }
            public float HeadHeight { get; set; }
        }
        public class VRMLicenceAnser
        {
            public bool Agree { get; set; }
        }
    }
}
