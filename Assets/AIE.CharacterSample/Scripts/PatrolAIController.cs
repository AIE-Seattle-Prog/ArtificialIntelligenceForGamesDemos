using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PatrolAIController : MonoBehaviour
{
    public CharacterMotor motor;

    public float waypointThreshold = 0.1f;

    public Transform[] waypoints;
    private int curWaypointIndex;

    private void Update()
    {
        Vector3 curPosition = motor.transform.position;
        Vector3 dstPosition = waypoints[curWaypointIndex].position;

        Vector3 offset = dstPosition - curPosition;

        if (offset.sqrMagnitude < waypointThreshold * waypointThreshold)
        {
            curWaypointIndex = (curWaypointIndex + 1) % waypoints.Length;

            // refresh destination
            dstPosition = waypoints[curWaypointIndex].position;
            offset = dstPosition - curPosition;
        }

        motor.MoveWish = offset.normalized;
    }
}
