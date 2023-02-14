using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[SelectionBase]
public class HumanoidMotor : MonoBehaviour
{
    [field: SerializeField]
    public Rigidbody rbody { get; private set; }
    [field: SerializeField]
    public CapsuleCollider col { get; private set; }

    [Header("State")]
    public bool isGrounded = false;
    
    /// <summary>
    /// Mask applied when doing any physics queries relating to the motor
    /// </summary>
    [Header("Sweep")]
    public LayerMask worldMask = ~1;

    public float maxGroundAngle = 60.0f;
    
    private int animSpeedHash;

    [Header("Input")]
    public Vector3 MoveWish;
    public bool CrouchWish;

    [Header("Motor Settings")]
    public float skinWidth = 0.05f;
    public float MoveSpeed = 8.0f;
    public float CrouchSpeed = 6.0f;
    [Space]
    public float StandHeight = 2.0f;
    public float CrouchHeight = 1.25f;
    [Space]
    public float groundSearchDistance = 0.01f;
    
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

        // rebuild matrix for new location - assuming top right
        var trsMatrix = Matrix4x4.TRS(posePosition, Quaternion.identity, transform.lossyScale);
        top = trsMatrix * new Vector4(top.x, top.y, top.z, 1);
        bottom = trsMatrix * new Vector4(bottom.x, bottom.y, bottom.z, 1);
        
        return Physics.CapsuleCast(top, bottom,
            radius - skinWidth,
            direction,
            out hit,
            distance,
            layerMask, triggerInteractionMode);
    }
    
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

    private bool CheckGround(Vector3 posePosition, out RaycastHit groundHit)
    {
        // cache height and radius
        float height = col.height;
        float radius = col.radius;
        
        // center of capsule
        Vector3 castOrigin = posePosition;
        castOrigin.y += height / 2.0f; // assumes bottom-center pivot
        
        float dist = height / 2.0f - radius + groundSearchDistance;
        
        // check if there's ground below us
        if (Physics.SphereCast(castOrigin, radius, Vector3.down, out var hit, dist, worldMask, QueryTriggerInteraction.Ignore))
        {
            if (Vector3.Angle(hit.normal, Vector3.up) < maxGroundAngle)
            {
                groundHit = hit;
                return true;
            }
        }

        // must write to 'groundHit' out param - write defaults when none available 
        groundHit = new RaycastHit();
        return false;
    }

    private void Awake()
    {
        animSpeedHash = Animator.StringToHash("Speed");
    }

    private void FixedUpdate()
    {
        // calculate movement
        Vector3 delta = MoveWish * (CrouchWish ? CrouchSpeed : MoveSpeed);
        
        // do ground check
        isGrounded = CheckGround(rbody.position, out var groundHit);

        if (isGrounded && delta.sqrMagnitude > 0.0f)
        {
            // ignore any vertical input while grounded
            delta.y = 0.0f;
            
            float oldMag = delta.magnitude;

            // project movement onto ground surface
            Vector3 projectedVelocity = Vector3.ProjectOnPlane(delta, groundHit.normal);
            // rescale translation on XZ axes to maintain speed
            // (otherwise we'll slow down after projection)
            float scalar = oldMag / projectedVelocity.magnitude;
            projectedVelocity *= scalar;
            
            delta = projectedVelocity;
        }
        else
        {
            // retain old Y-velocity while not grounded (aka falling)
            delta.y = rbody.velocity.y;
        }

        // apply standing/crouching
        ResizeHeight(CrouchWish ? CrouchHeight : StandHeight);

        // apply movement
        rbody.velocity = delta;

        // update rotation
        Vector3 lookDirection = new Vector3(rbody.velocity.x, 0.0f, rbody.velocity.z);
        if (rbody.velocity.sqrMagnitude > 0.0f)
        {
            rbody.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
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
        
        Vector3 castOrigin = transform.position;
        castOrigin.y += col.height / 2.0f;
        float dist = col.height / 2.0f - col.radius + groundSearchDistance;
        Gizmos.DrawRay(castOrigin,  Vector3.down * dist);
        Vector3 castDst = castOrigin + Vector3.down * dist;
        Gizmos.DrawWireSphere(castDst, col.radius);
    }

    private void Reset()
    {
        rbody = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        anim = GetComponentInChildren<Animator>();
    }
}
