using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectFSMController : BaseHumanoidController
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
    [Space]
    public float attackThreshold = 3.0f;

    private HashSet<BaseHumanoidController> enemyCandidates = new();
    [NonSerialized]
    public BaseHumanoidController followTarget;

    [Header("Steering Forces")]
    public float patrolStrength = 5.0f;
    public float chaseStrength = 3.0f;

    private FiniteStateMachineRunner fsmRunner;

    private class PatrolState : BaseState
    {
        public ObjectFSMController agent;

        public PatrolState(ObjectFSMController agent)
        {
            this.agent = agent;
        }

        public override void OnStateEnter()
        {
            agent.SetDestination(agent.patrolPoints[agent.currentPatrolIndex].position);
            agent.motor.SprintWish = false;
        }

        public override void OnStateRun()
        {
            // check if wp reached
            if ((agent.motor.transform.position - agent.patrolPoints[agent.currentPatrolIndex].position).sqrMagnitude < agent.waypointThreshold * agent.waypointThreshold)
            {
                agent.currentPatrolIndex = (agent.currentPatrolIndex + 1) % agent.patrolPoints.Length;
                agent.SetDestination(agent.patrolPoints[agent.currentPatrolIndex].position);
            }
        }
    }

    private class ChaseState : BaseState
    {
        public ObjectFSMController agent;

        public ChaseState(ObjectFSMController agent)
        {
            this.agent = agent;
        }

        public override void OnStateEnter()
        {
            agent.curNavMeshPathIndex = -1;
            agent.motor.SprintWish = true;
        }

        public override void OnStateRun()
        {
            // early exit if nothing to chase
            if (agent.followTarget == null) { return; }

            bool meOnNav = NavMesh.SamplePosition(agent.motor.transform.position, out var myHit, 1.0f, NavMesh.AllAreas);
            bool followOnNav = NavMesh.SamplePosition(agent.followTarget.transform.position, out var followHit, 1.0f, NavMesh.AllAreas);

            NavMeshHit navCastHit = new ();
            bool hasClearPath = meOnNav && followOnNav && !NavMesh.Raycast(myHit.position, followHit.position, out navCastHit, NavMesh.AllAreas);
            if (hasClearPath)
            {
                Vector3 chaseForce = SteeringMethods.Seek(agent.motor.transform.position, agent.followTarget.transform.position, agent.motor.MoveWish, 1.0f);
                chaseForce.y = 0.0f;
                chaseForce = chaseForce.normalized * (agent.chaseStrength * Time.deltaTime);
                agent.motor.MoveWish += chaseForce;
                agent.motor.MoveWish.Normalize();
            }
            else
            {
                // draw obstruction
                Debug.DrawRay(navCastHit.position, Vector3.up * 3.0f, Color.red, 0.1f);
                if (followOnNav)
                {
                    bool isPathStale = NavMesh.Raycast(agent.Destination, 
                        followHit.position,
                        out var staleHit, 
                        NavMesh.AllAreas) || agent.HasReachedDestination;
                    
                    Debug.DrawRay(agent.Destination, Vector3.up * 3.0f, Color.green, 0.1f);
                    if (isPathStale)
                    {
                        agent.SetDestination(followHit.position);
                    }
                }
                else
                {
                    Vector3 chaseForce = SteeringMethods.Seek(agent.motor.transform.position, agent.followTarget.transform.position, agent.motor.MoveWish, 1.0f);
                    chaseForce.y = 0.0f;
                    chaseForce = chaseForce.normalized * (agent.chaseStrength * Time.deltaTime);
                    agent.motor.MoveWish += chaseForce;
                    agent.motor.MoveWish.Normalize();
                }
            }
        }
    }

    public bool SetDestination(Vector3 destination)
    {
        curNavMeshPathIndex = -1;
        HasReachedDestination = false;
        bool canReach = NavMesh.CalculatePath(motor.transform.position, destination, navFilter,  navMeshPath);
        if(!canReach) { return false; }

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

    private void Awake()
    {
        navMeshPath = new();
        navFilter = new NavMeshQueryFilter() { agentTypeID = navAgentTypeID, areaMask = NavMesh.AllAreas };
        fsmRunner = new FiniteStateMachineRunner();
        PatrolState patrol = new PatrolState(this);
        ChaseState chase = new ChaseState(this);

        // patrol => chase
        patrol.AddCondition(new BooleanTransition(chase, true, () => { return followTarget != null; }));
        // chase => patrol
        chase.AddCondition(new BooleanTransition(patrol, true, () => { return followTarget == null; }));

        fsmRunner.CurrentState = patrol;
    }

    private void Update()
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

        // fsm
        fsmRunner.Run();

        // pathfinding
        if(navMeshPath.status != NavMeshPathStatus.PathInvalid &&
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

    private void OnTriggerEnter(Collider other)
    {
        // always auto follow enemies
        if (other.TryGetComponent<BaseHumanoidController>(out var otherController))
        {
            if(otherController.factionId != factionId)
            {
                enemyCandidates.Add(otherController);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // unfollow if target escapes detection radius
        if (other.TryGetComponent<BaseHumanoidController>(out var otherController))
        {
            // assuming that enemies can't change factionIDs at runtime
            if(otherController.factionId != factionId)
            {
                enemyCandidates.Remove(otherController);
            }
        }
    }

    private void OnDrawGizmosSelected()
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
                Gizmos.DrawLine(navMeshPath.corners[i], navMeshPath.corners[i+1]);
            }
        }
    }
}