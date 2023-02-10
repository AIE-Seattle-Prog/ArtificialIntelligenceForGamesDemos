using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[DefaultExecutionOrder(100)]
public class ThirdPersonCameraController : MonoBehaviour
{
    public Transform target;
    public Vector3 targetDamping;
    // camera view target
    private Vector3 targetPosition;

    public float followDistance = 3.0f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        // do nothing if no target to follow
        if(target == null) { return; }
        
        Vector3 targetRealPosition = target.position;
        
        targetPosition.x = Mathf.SmoothDamp(targetPosition.x, targetRealPosition.x, ref velocity.x, targetDamping.x);
        targetPosition.y = Mathf.SmoothDamp(targetPosition.y, targetRealPosition.y, ref velocity.y, targetDamping.y);
        targetPosition.z = Mathf.SmoothDamp(targetPosition.z, targetRealPosition.z, ref velocity.z, targetDamping.z);
        
        // camera goal
        Vector3 goalPosition = targetRealPosition + -target.forward * followDistance;

        transform.position = goalPosition;
        transform.forward = (targetPosition - goalPosition).normalized;
    }
}
