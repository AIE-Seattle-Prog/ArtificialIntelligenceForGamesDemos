using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicAIController : MonoBehaviour
{
    public HumanoidMotor motor;

    /// <summary>
    /// Will remain crouched for at least this long before standing back up
    /// </summary>
    public float crouchMinTime = 0.1f;
    /// <summary>
    /// Timer used to determine when standing back up is possible
    /// </summary>
    private float crouchTimer;

    [Header("Waypoints")]
    /// <summary>
    /// A series of waypoints that the character will walk through. Will loop back to
    /// beginning after reaching the last one.
    /// </summary>
    public Transform[] waypoints;
    /// <summary>
    /// An index denoting which waypoint we are currently pathing to.
    /// </summary>
    private int currentWaypoint;

    /// <summary>
    /// 
    /// </summary>
    public float waypointThreshold = 0.3f;

    [Header("Sweep")]
    public LayerMask worldMask;

    private Collider[] overlapResults = new Collider[32];
    private int overlapCount;

    private void Update()
    {
        bool crouchedThisFrame = false;

        // waypoint logic
        Vector3 curPos = motor.transform.position;
        Vector3 dstPos = waypoints[currentWaypoint].position;

        Vector3 offset = dstPos - curPos;

        if(offset.sqrMagnitude < waypointThreshold * waypointThreshold)
        {
            ++currentWaypoint;
            currentWaypoint %= waypoints.Length;

            dstPos = waypoints[currentWaypoint].position;
            offset = dstPos - curPos;
        }

        offset = Vector3.ClampMagnitude(offset, 1.0f);

        // apply desired input
        motor.MoveWish = offset;

        //
        // check if movement will be blocked
        //
        HumanoidMotor.GetCapsulePoints(motor.col.height, motor.col.radius,
            out var top, out _, out var bottom);
        top = motor.transform.TransformPoint(top);
        bottom = motor.transform.TransformPoint(bottom);

        bool isBlocked = Physics.CapsuleCast(top, bottom, motor.col.radius * 0.95f, offset.normalized, motor.MoveSpeed * Time.deltaTime);
        // don't evaluate crouch-bypass if already crouched and blocked
        if(isBlocked && !motor.CrouchWish)
        {
            HumanoidMotor.GetCapsulePoints(motor.CrouchHeight, motor.col.radius,
            out top, out _, out bottom);

            top = motor.transform.TransformPoint(top);
            bottom = motor.transform.TransformPoint(bottom);

            // determine if crouching will help bypass obstacle
            bool shouldCrouch = !Physics.CapsuleCast(top, bottom,
                motor.col.radius,
                offset.normalized,
                motor.MoveSpeed * Time.deltaTime);
            motor.CrouchWish = shouldCrouch;
            crouchedThisFrame = shouldCrouch;
            crouchTimer = crouchMinTime;
        }

        //
        // attempt uncrouch if crouching
        //
        if(!crouchedThisFrame && motor.CrouchWish)
        {
            crouchTimer -= Time.deltaTime;
            if (crouchTimer < 0)
            {
                HumanoidMotor.GetCapsulePoints(motor.StandHeight, motor.col.radius,
                out top, out _, out bottom);
                top = motor.transform.TransformPoint(top);
                bottom = motor.transform.TransformPoint(bottom);

                overlapCount = Physics.OverlapCapsuleNonAlloc(top, bottom, motor.col.radius * 0.95f, overlapResults, worldMask);
                // nothing in the way?
                if (overlapCount == 0)
                {
                    motor.CrouchWish = false;
                }
            }
        }
    }
}
