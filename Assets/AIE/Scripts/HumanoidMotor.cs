using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[SelectionBase]
public class HumanoidMotor : MonoBehaviour
{
    public Vector3 MoveWish;
    public bool CrouchWish;

    public float MoveSpeed = 8.0f;

    public float StandHeight = 2.0f;
    public float CrouchHeight = 1.25f;

    [field: SerializeField]
    public Rigidbody rbody { get; private set; }
    [field: SerializeField]
    public CapsuleCollider col { get; private set; }

    // TODO: add a way to retrieve dimensions (top, center, bot) for the capsule
    // at specific heights
    public static void GetCapsulePoints(float height, float radius, out Vector3 top, out Vector3 center, out Vector3 bottom)
    {
        center = new Vector3(0, height / 2, 0);

        Vector3 offset = new Vector3(0, height / 2 - radius, 0);

        top = center + offset;
        bottom = center - offset;
    }

    private void ResizeHeight(float newHeight)
    {
        col.height = newHeight;
        col.center = new Vector3(0, col.height / 2, 0);
    }

    private void Update()
    {
        MoveWish *= MoveSpeed;

        ResizeHeight(CrouchWish ? CrouchHeight : StandHeight);

        rbody.MovePosition(rbody.position + MoveWish * Time.deltaTime);
    }
}
