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

    [Header("Steering")]
    public float seekStrength = 2.0f;
    [Space]
    public float wanderStrength = 3.0f;
    public float wanderRadius = 2.5f;
    public float wanderJitter = 0.5f;

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
    /// Distance at which a waypoint will be considered "reached" before moving
    /// to the next waypoint
    /// </summary>
    public float waypointThreshold = 0.3f;

    [Header("Sweep")]
    /// <summary>
    /// Mask applied when doing any physics queries relating to the motor
    /// </summary>
    public LayerMask worldMask;

    /// <summary>
    /// Intermediary buffer for retaining the results of various overlap tests.
    /// <br></br>
    /// Refer to 'overlapCount' to determine how many entries to check.
    /// </summary>
    private Collider[] overlapResults = new Collider[32];
    /// <summary>
    /// The number of colliders present in 'overlapResults'.
    /// </summary>
    private int overlapCount;

    private void Update()
    {
        bool crouchedThisFrame = false;

        // waypoint logic
        Vector3 curPos = motor.transform.position;
        Vector3 dstPos = waypoints[currentWaypoint].position;

        Vector3 offset = dstPos - curPos;

        // check if waypoint was reached
        if(offset.sqrMagnitude < waypointThreshold * waypointThreshold)
        {
            ++currentWaypoint;
            currentWaypoint %= waypoints.Length;

            dstPos = waypoints[currentWaypoint].position;
        }

        // apply desired input
        Vector3 seekForce = SteeringMethods.Seek(curPos, dstPos, motor.MoveWish, 1.0f);
        Vector3 wanderForce = SteeringMethods.Wander(curPos, wanderRadius, wanderJitter, motor.MoveWish, 1.0f);
        wanderForce.y = 0.0f;
        wanderForce.Normalize(); // HACK: wander assumes 3D, but ground movement only occurs in 2D (relative to ground plane)

        // apply forces
        motor.MoveWish += seekForce * (seekStrength * Time.deltaTime);
        motor.MoveWish += wanderForce * (wanderStrength * Time.deltaTime);

        // normalize (MoveWish expects a magnitude of 1)
        motor.MoveWish.Normalize();

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
