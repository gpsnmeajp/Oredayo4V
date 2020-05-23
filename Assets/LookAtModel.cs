using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EVMC4U;

public class LookAtModel : MonoBehaviour
{
    public ExternalReceiver externalReceiver;
    public float zaxis = 0f;
    public float height = 1.4f;
    void Start()
    {
        
    }

    void LateUpdate()
    {
        if (externalReceiver.Model != null) {
            Vector3 t = externalReceiver.Model.transform.position;
            t.y += height;
            transform.LookAt(t);
            transform.localRotation = Quaternion.Euler(transform.localRotation.eulerAngles.x, transform.localRotation.eulerAngles.y, transform.localRotation.eulerAngles.z + zaxis);
        }
    }
}
