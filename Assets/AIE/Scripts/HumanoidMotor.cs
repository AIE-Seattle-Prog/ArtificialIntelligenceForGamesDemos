using System;
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

    private int animSpeedHash;

    [Header("Input")]
    public Vector3 MoveWish;
    public bool CrouchWish;

    [Header("Motor Settings")]
    public float MoveSpeed = 8.0f;
    public float CrouchSpeed = 6.0f;
    [Space]
    public float StandHeight = 2.0f;
    public float CrouchHeight = 1.25f;
    
    [field: Header("Animation Settings")]
    [field: SerializeField]
    public Animator anim { get; private set; }

    public struct CapsuleDimensions
    {
        public Vector3 top;
        public Vector3 center;
        public Vector3 bottom;
    }

    public bool Cast(Vector3 direction, float distance, out RaycastHit hit, int layerMask=~1, QueryTriggerInteraction triggerInteractionMode= QueryTriggerInteraction.UseGlobal)
    {
        return Cast(transform.position, col.height, col.radius, direction, distance, out hit, layerMask, triggerInteractionMode);
    }

    public bool Cast(Vector3 posePosition, float height, float radius, Vector3 direction, float distance,out RaycastHit hit, int layerMask=~1, QueryTriggerInteraction triggerInteractionMode= QueryTriggerInteraction.UseGlobal)
    {
        GetCapsulePoints(height, radius, out var top, out var center, out var bottom);

        var trsMatrix = Matrix4x4.TRS(posePosition, Quaternion.identity, transform.lossyScale);
        top = trsMatrix * top;
        bottom = trsMatrix * bottom;
        
        return Physics.CapsuleCast(top, bottom,
            radius,
            direction,
            out hit,
            distance,
            layerMask, triggerInteractionMode);
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

    private void UpdateAnimator(Animator animTarget)
    {
        animTarget.SetFloat(animSpeedHash, rbody.velocity.magnitude);
        animTarget.SetLayerWeight(1, CrouchWish ? 1.0f : 0.0f);
    }

    private void Awake()
    {
        animSpeedHash = Animator.StringToHash("Speed");
    }

    private void FixedUpdate()
    {
        // calculate movement
        Vector3 delta = MoveWish * (CrouchWish ? CrouchSpeed : MoveSpeed);

        // apply standing/crouching
        ResizeHeight(CrouchWish ? CrouchHeight : StandHeight);

        // apply movement
        rbody.velocity = delta;

        // update rotation
        if (rbody.velocity.sqrMagnitude > 0.0f)
        {
            rbody.rotation = Quaternion.LookRotation(rbody.velocity, Vector3.up);
        }

        // update animator
        if (anim != null)
        {
            UpdateAnimator(anim);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, rbody.velocity);
    }

    private void Reset()
    {
        rbody = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        anim = GetComponentInChildren<Animator>();
    }
}
