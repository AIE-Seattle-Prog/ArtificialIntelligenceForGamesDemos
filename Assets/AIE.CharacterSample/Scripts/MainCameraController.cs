using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(100)]
public class MainCameraController : MonoBehaviour
{
    public Transform CameraToShadow;

    private void LateUpdate()
    {
        if(CameraToShadow == null) {return;}
        transform.position = CameraToShadow.position;
        transform.rotation = CameraToShadow.rotation;
    }
}
