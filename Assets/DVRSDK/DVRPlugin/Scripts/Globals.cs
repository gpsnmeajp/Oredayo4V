using DVRSDK.Avatar;
using DVRSDK.Plugins.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Globals
{
    public static DVRSDK.Avatar.VRMLoader vrmLoader = new DVRSDK.Avatar.VRMLoader();

    public static ButtonInputInterface buttonInputInterface;

    public static GameObject currentVrmModel;
}
