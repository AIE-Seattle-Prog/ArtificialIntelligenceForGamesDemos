using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasicAIController : MonoBehaviour
{
    public HumanoidMotor motor;

    public Transform[] waypoints;
    private int currentWaypoint;

    public float threshold = 0.3f;

    public LayerMask worldMask;

    private Collider[] overlapResults = new Collider[32];
    private int overlapCount;

    private void Update()
    {
        // waypoint logic
        Vector3 curPos = motor.transform.position;
        Vector3 dstPos = waypoints[currentWaypoint].position;

        Vector3 offset = dstPos - curPos;

        if(offset.sqrMagnitude < threshold * threshold)
        {
            ++currentWaypoint;
            currentWaypoint %= waypoints.Length;

            dstPos = waypoints[currentWaypoint].position;
            offset = dstPos - curPos;
        }

        offset = Vector3.ClampMagnitude(offset, 1.0f);

        motor.MoveWish = offset;

        //
        // check if crouch needed
        //

        HumanoidMotor.GetCapsulePoints(motor.col.height, motor.col.radius,
            out var top, out _, out var bottom);
        top = motor.transform.TransformPoint(top);
        bottom = motor.transform.TransformPoint(bottom);

        bool isBlocked = Physics.CapsuleCast(top, bottom, motor.col.radius * 0.95f, offset.normalized, motor.MoveSpeed * Time.deltaTime);
        bool crouchedThisFrame = false;
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
        }

        //
        // attempt uncrouch if crouching
        //
        if(!crouchedThisFrame && motor.CrouchWish)
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
