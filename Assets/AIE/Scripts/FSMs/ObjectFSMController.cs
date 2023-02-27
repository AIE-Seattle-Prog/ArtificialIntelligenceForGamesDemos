using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectFSMController : BaseHumanoidController
{
    [Header("AI Controller")]
    public LayerMask visibilityMask = ~1;
    public int navAgentTypeID;
    private NavMeshQueryFilter navFilter;

    private NavMeshPath navMeshPath;

    public Vector3 Destination
    {
        get
        {
            return navMeshPath != null && navMeshPath.status != NavMeshPathStatus.PathInvalid
                ? navMeshPath.corners[navMeshPath.corners.Length - 1]
                : Vector3.zero;
        }
    }

    private int curNavMeshPathIndex = -1;

    [SerializeField]
    public Transform[] patrolPoints;
    [NonSerialized]
    public int currentPatrolIndex;

    public float waypointThreshold = 0.5f;
    [Space]
    public float attackThreshold = 3.0f;
    [NonSerialized]
    public Transform followTarget;

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
            bool followOnNav = NavMesh.SamplePosition(agent.followTarget.position, out var followHit, 1.0f, NavMesh.AllAreas);

            bool hasClearPath = meOnNav && followOnNav && !NavMesh.Raycast(myHit.position, followHit.position, out var navCastHit, NavMesh.AllAreas);
            if (hasClearPath)
            {
                Vector3 chaseForce = SteeringMethods.Seek(agent.motor.transform.position, agent.followTarget.position, agent.motor.MoveWish, 1.0f);
                chaseForce.y = 0.0f;
                chaseForce = chaseForce.normalized * (agent.chaseStrength * Time.deltaTime);
                agent.motor.MoveWish += chaseForce;
                agent.motor.MoveWish.Normalize();
            }
            else
            {
                if (followOnNav)
                {
                    bool isPathStale = NavMesh.Raycast(agent.Destination, 
                        followHit.position,
                        out var staleHit, 
                        NavMesh.AllAreas);
                    if (isPathStale)
                    {
                        agent.SetDestination(followHit.position);
                    }
                }
                else
                {
                    Vector3 chaseForce = SteeringMethods.Seek(agent.motor.transform.position, agent.followTarget.position, agent.motor.MoveWish, 1.0f);
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
        bool canReach = NavMesh.CalculatePath(motor.transform.position, destination, navFilter,  navMeshPath);
        if(!canReach) { return false; }

        curNavMeshPathIndex = 0;
        
        // skip first wp if it's too close
        Vector3 pathTarget = navMeshPath.corners[curNavMeshPathIndex];
        Vector3 offset = pathTarget - motor.transform.position;
        if (offset.sqrMagnitude < waypointThreshold * waypointThreshold)
        {
            ++curNavMeshPathIndex;
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
        fsmRunner.Run();

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
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // always auto follow the player
        if (other.TryGetComponent<BaseHumanoidController>(out var otherController))
        {
            if(otherController.factionId != factionId)
            {
                followTarget = other.transform;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // unfollow if target escapes detection radius
        if(other.transform == followTarget)
        {
            followTarget = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (navMeshPath != null &&
            navMeshPath.status != NavMeshPathStatus.PathInvalid &&
            curNavMeshPathIndex != -1 &&
            curNavMeshPathIndex != navMeshPath.corners.Length)
        {
            Gizmos.color = Color.green;
            
            Gizmos.DrawLine(motor.transform.position, navMeshPath.corners[curNavMeshPathIndex]);
            
            for (int i = curNavMeshPathIndex; i < navMeshPath.corners.Length - 1; ++i)
            {
                Gizmos.DrawLine(navMeshPath.corners[i], navMeshPath.corners[i+1]);
            }
        }
    }
}