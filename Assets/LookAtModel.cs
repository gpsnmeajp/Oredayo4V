using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EVMC4U;

public class LookAtModel : MonoBehaviour
{
    public ExternalReceiver externalReceiver;    
    void Start()
    {
        
    }

    void Update()
    {
        if (externalReceiver.Model != null) {
            Vector3 t = externalReceiver.Model.transform.position;
            t.y = transform.position.y;
            transform.LookAt(t);
        }
    }
}
