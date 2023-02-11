using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidMotor : MonoBehaviour
{
    [field: HideInInspector]
    public Transform CachedTransform { get; private set; }
    public Rigidbody rbody;

    public float flySpeed = 20.0f;
    public float turnSpeed = 180.0f;

    private void Awake()
    {
        CachedTransform = transform;
    }

    private void FixedUpdate()
    {
        if (rbody.velocity.sqrMagnitude > 0.0f)
        {
            rbody.rotation = Quaternion.RotateTowards(rbody.rotation, Quaternion.LookRotation(rbody.velocity.normalized, Vector3.up), turnSpeed * Time.deltaTime);
        }
    }

    private void Reset()
    {
        CachedTransform = transform;
    }
}
