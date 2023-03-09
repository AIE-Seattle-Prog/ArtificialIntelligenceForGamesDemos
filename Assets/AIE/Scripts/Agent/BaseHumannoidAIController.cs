using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BaseHumanoidAIController : BaseHumanoidController
{
    [Header("AI Controller")]
    public LayerMask visibilityMask = ~1;

    [SerializeField]
    private int navAgentTypeID;
    private NavMeshPath navMeshPath;
    private int curNavMeshPathIndex = -1;
    private NavMeshQueryFilter navFilter;

    public Vector3 Destination
    {
        get
        {
            return navMeshPath != null && navMeshPath.status != NavMeshPathStatus.PathInvalid
                ? navMeshPath.corners[navMeshPath.corners.Length - 1]
                : Vector3.zero;
        }
    }
    public bool HasReachedDestination { get; private set; }

    [Space]
    [SerializeField]
    public Transform[] patrolPoints;
    [NonSerialized]
    public int currentPatrolIndex;

    public float waypointThreshold = 0.5f;
    public float WaypointThresholdSqr => waypointThreshold * waypointThreshold;
    [Space]
    public float attackThreshold = 3.0f;

    private HashSet<BaseHumanoidController> enemyCandidates = new();
    [NonSerialized]
    public BaseHumanoidController followTarget;

    [Header("Steering Forces")]
    public float patrolStrength = 5.0f;
    public float chaseStrength = 3.0f;

    public bool SetDestination(Vector3 destination)
    {
        curNavMeshPathIndex = -1;
        HasReachedDestination = false;
        bool canReach = NavMesh.CalculatePath(motor.transform.position, destination, navFilter, navMeshPath);
        if (!canReach) { return false; }

        curNavMeshPathIndex = 0;

        // skip first wp if it's too close
        Vector3 pathTarget = navMeshPath.corners[curNavMeshPathIndex];
        Vector3 offset = pathTarget - motor.transform.position;
        if (offset.sqrMagnitude < waypointThreshold * waypointThreshold)
        {
            ++curNavMeshPathIndex;
            HasReachedDestination = curNavMeshPathIndex >= navMeshPath.corners.Length;
        }

        return true;
    }

    protected virtual void Awake()
    {
        navMeshPath = new();
        navFilter = new NavMeshQueryFilter() { agentTypeID = navAgentTypeID, areaMask = NavMesh.AllAreas };
    }

    protected virtual void Update()
    {
        // target acquisition - only if we don't have one yet
        if (followTarget == null)
        {
            foreach (var curCandidate in enemyCandidates)
            {
                bool canSee = !Physics.Linecast(headTransform.position, curCandidate.headTransform.position, out RaycastHit losHit,
                    visibilityMask, QueryTriggerInteraction.Ignore);

                // we can still see them if the only thing we hit was them
                if (!canSee)
                {
                    canSee = losHit.collider.gameObject == curCandidate.gameObject;
                }

                // so, can we see them? if so, follow them!
                if (canSee)
                {
                    followTarget = curCandidate;
                    break;
                }
            }
        }
        else
        {
            bool canSee = !Physics.Linecast(headTransform.position, followTarget.headTransform.position, out RaycastHit losHit,
                visibilityMask, QueryTriggerInteraction.Ignore);

            // we can still see them if the only thing we hit was them
            if (!canSee)
            {
                canSee = losHit.collider.gameObject == followTarget.gameObject;
            }

            // if we still can't see them, drop it
            if (!canSee)
            {
                followTarget = null;
            }
        }

        // pathfinding
        if (navMeshPath.status != NavMeshPathStatus.PathInvalid &&
           curNavMeshPathIndex != -1 &&
           curNavMeshPathIndex != navMeshPath.corners.Length)
        {
            Vector3 pathTarget = navMeshPath.corners[curNavMeshPathIndex];
            Vector3 offset = pathTarget - motor.transform.position;
            motor.MoveWish = offset.normalized;

            float strength = followTarget == null ? patrolStrength : chaseStrength;

            Vector3 pathForce = SteeringMethods.Seek(motor.transform.position, pathTarget, motor.MoveWish, 1.0f);
            pathForce.y = 0.0f;
            pathForce = pathForce.normalized * (strength * Time.deltaTime);
            motor.MoveWish += pathForce;
            motor.MoveWish.Normalize();

            if (offset.sqrMagnitude < waypointThreshold * waypointThreshold)
            {
                ++curNavMeshPathIndex;
                HasReachedDestination = curNavMeshPathIndex >= navMeshPath.corners.Length;
            }
        }
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // always auto follow enemies
        if (other.TryGetComponent<BaseHumanoidController>(out var otherController))
        {
            if (otherController.factionId != factionId)
            {
                enemyCandidates.Add(otherController);
            }
        }
    }

    protected virtual void OnTriggerExit(Collider other)
    {
        // unfollow if target escapes detection radius
        if (other.TryGetComponent<BaseHumanoidController>(out var otherController))
        {
            // assuming that enemies can't change factionIDs at runtime
            if (otherController.factionId != factionId)
            {
                enemyCandidates.Remove(otherController);
            }
        }
    }

    protected virtual void OnDrawGizmosSelected()
    {
        if (navMeshPath != null &&
            navMeshPath.status != NavMeshPathStatus.PathInvalid &&
            curNavMeshPathIndex != -1 &&
            curNavMeshPathIndex != navMeshPath.corners.Length)
        {
            Gizmos.color = Color.white;

            Gizmos.DrawWireSphere(motor.transform.position, waypointThreshold);

            Gizmos.color = Color.green;

            Gizmos.DrawLine(motor.transform.position, navMeshPath.corners[curNavMeshPathIndex]);

            for (int i = curNavMeshPathIndex; i < navMeshPath.corners.Length - 1; ++i)
            {
                Gizmos.DrawLine(navMeshPath.corners[i], navMeshPath.corners[i + 1]);
            }
        }
    }
}
