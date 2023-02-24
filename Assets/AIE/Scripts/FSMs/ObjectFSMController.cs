using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectFSMController : MonoBehaviour
{
    public CharacterMotor motor;
    public LayerMask walkableLayers = ~1;
    private NavMeshPath navMeshPath;
    private int curNavMeshPathIndex = -1;

    [SerializeField]
    public Transform[] patrolPoints;
    public int currentPatrolIndex;

    public float waypointThreshold = 0.5f;
    [Space]
    public float attackThreshold = 3.0f;
    public Transform followTarget;

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

            agent.motor.MoveWish = (agent.followTarget.position - agent.motor.transform.position).normalized;
        }
    }

    public void SetDestination(Vector3 destination)
    {
        curNavMeshPathIndex = -1;
        bool canReach = NavMesh.CalculatePath(motor.transform.position, destination, walkableLayers, navMeshPath);
        if(!canReach) { Debug.LogError($"Can't reach {destination} from this agent", this); return; }

        curNavMeshPathIndex = 0;
    }

    private void Awake()
    {
        navMeshPath = new();
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

            if (offset.sqrMagnitude < waypointThreshold * waypointThreshold)
            {
                ++curNavMeshPathIndex;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            followTarget = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            followTarget = null;
        }
    }
}