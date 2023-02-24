using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileAIController : MonoBehaviour
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

    /// <summary>
    /// A series of waypoints that the character will walk through. Will loop back to
    /// beginning after reaching the last one.
    /// </summary>
    [field: Header("Waypoints")]
    public List<Vector3> Waypoints { get; private set; } = new();
    /// <summary>
    /// An index denoting which waypoint we are currently pathing to.
    /// </summary>
    private int currentWaypoint;

    /// <summary>
    /// Distance at which a waypoint will be considered "reached" before moving
    /// to the next waypoint
    /// </summary>
    public float waypointThreshold = 0.3f;

    /// <summary>
    /// Intermediary buffer for retaining the results of various overlap tests.
    /// <br></br>
    /// Refer to 'overlapCount' to determine how many entries to check.
    /// </summary>
    private Collider[] cacheOverlapResults = new Collider[32];
    /// <summary>
    /// The number of colliders present in 'overlapResults'.
    /// </summary>
    private int cacheOverlapCount;

    public void SetPath(List<Vector3> path)
    {
        Waypoints.Clear();

        foreach(var point in path)
        {
            Waypoints.Add(point);
        }

        currentWaypoint = 0;
    }

    private void Update()
    {
        bool crouchedThisFrame = false;

        // waypoint logic
        Vector3 curPos = motor.transform.position;
        Vector3 dstPos = curPos;

        if (Waypoints != null && Waypoints.Count > 0)
        {
            dstPos = Waypoints[currentWaypoint];
            Vector3 offset = dstPos - curPos;

            // check if waypoint was reached
            if (offset.sqrMagnitude < waypointThreshold * waypointThreshold)
            {
                ++currentWaypoint;
                if (currentWaypoint == Waypoints.Count)
                {
                    Waypoints.Clear();
                }
                else
                {
                    dstPos = Waypoints[currentWaypoint];
                }
            }
        }
        else
        {
            motor.MoveWish = Vector3.zero;
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
        motor.MoveWish = Vector3.ClampMagnitude(motor.MoveWish, 1);

        //
        // check if movement will be blocked
        //
        bool isBlocked = motor.Cast(motor.MoveWish, motor.MoveSpeed * Time.deltaTime, out var hit, motor.worldMask);
        
        // don't evaluate crouch-bypass if already crouched and blocked
        if(isBlocked &&
           hit.normal.y < 0.65f &&  // assumes that anything w/ a high Y-value is not ground
           !motor.CrouchWish)
        {
            // determine if crouching will help bypass obstacle
            bool shouldCrouch = !motor.Cast(curPos, motor.CrouchHeight,
                motor.col.radius,
                motor.MoveWish,
                motor.MoveSpeed * Time.deltaTime,
                out _);
            
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
                out var top, out _, out var bottom);
                top = motor.transform.TransformPoint(top);
                bottom = motor.transform.TransformPoint(bottom);

                cacheOverlapCount = Physics.OverlapCapsuleNonAlloc(top, bottom, motor.col.radius - motor.skinWidth, cacheOverlapResults, motor.worldMask);
                // nothing in the way?
                if (cacheOverlapCount == 0)
                {
                    motor.CrouchWish = false;
                }
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        if (Waypoints != null && Waypoints.Count > 0)
        {
            HumanoidMotor.GetCapsulePoints(motor.col.height, motor.col.radius, out var top, out _, out _);
            Vector3 topWorld = transform.TransformPoint(top);
            Vector3 offset = topWorld - transform.position;

            for(int i = 0; i < Waypoints.Count-1; ++i)
            {
                Gizmos.DrawLine(Waypoints[i] + offset, Waypoints[i+1] + offset);
            }
            Gizmos.DrawLine(topWorld, Waypoints[currentWaypoint] + offset);
        }
    }
}
