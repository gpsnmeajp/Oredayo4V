using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EVMC4U;

//XZ追従機
public class RootLocker : MonoBehaviour
{
    public bool Lock = true;
    public ExternalReceiver externalReceiver;

    void Start()
    {

    }

    void LateUpdate()
    {
        if (externalReceiver.Model != null && Lock)
        {
            Vector3 t = externalReceiver.Model.transform.position;
            t.y = transform.position.y;
            transform.position = t;
        }
    }
}
