using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class HumanoidMotor : MonoBehaviour
{
    [field: SerializeField]
    public Rigidbody rbody { get; private set; }
    [field: SerializeField]
    public CapsuleCollider col { get; private set; }

    [Header("Input")]
    public Vector3 MoveWish;
    public bool CrouchWish;

    [Header("Motor Settings")]
    public float MoveSpeed = 8.0f;
    public float CrouchSpeed = 6.0f;
    [Space]
    public float StandHeight = 2.0f;
    public float CrouchHeight = 1.25f;

    public struct CapsuleDimensions
    {
        public Vector3 top;
        public Vector3 center;
        public Vector3 bottom;
    }

    // TODO: add a way to retrieve dimensions (top, center, bot) for the capsule
    // at specific heights
    public static void GetCapsulePoints(float height, float radius,
        out Vector3 top, out Vector3 center, out Vector3 bottom)
    {
        // assumes that the pivot is bottom-center

        center = new Vector3(0, height / 2, 0);

        Vector3 offset = new Vector3(0, height / 2 - radius, 0);

        top = center + offset;
        bottom = center - offset;
    }

    public static CapsuleDimensions GetCapsulePoints(float height, float radius)
    {
        // assumes that the pivot is bottom-center

        CapsuleDimensions dims = new CapsuleDimensions();
        dims.center = new Vector3(0, height / 2, 0);

        Vector3 offset = new Vector3(0, height / 2 - radius, 0);

        dims.top = dims.center + offset;
        dims.bottom = dims.center - offset;

        return dims;
    }

    private void ResizeHeight(float newHeight)
    {
        // assumes that the pivot is bottom-center

        col.height = newHeight;
        col.center = new Vector3(0, col.height / 2, 0);
    }

    private void FixedUpdate()
    {
        // calculate movement
        Vector3 delta = MoveWish * (CrouchWish ? CrouchSpeed : MoveSpeed);

        // apply standing/crouching
        ResizeHeight(CrouchWish ? CrouchHeight : StandHeight);

        // apply movement
        rbody.MovePosition(rbody.position + delta * Time.deltaTime);
    }
}
