using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ObjectFSMController : MonoBehaviour
{
    public CharacterMotor motor;
    public NavMeshAgent navAgent;

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

        public override void OnStateRun()
        {
            agent.motor.SprintWish = false;
            agent.motor.MoveWish = (agent.patrolPoints[agent.currentPatrolIndex].position - agent.motor.transform.position).normalized;

            // check if wp reached
            if ((agent.motor.transform.position - agent.patrolPoints[agent.currentPatrolIndex].position).sqrMagnitude < agent.waypointThreshold * agent.waypointThreshold)
            {
                agent.currentPatrolIndex = (agent.currentPatrolIndex + 1) % agent.patrolPoints.Length;
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

        public override void OnStateRun()
        {
            // early exit if nothing to chase
            if (agent.followTarget == null) { return; }

            agent.motor.SprintWish = true;
            agent.motor.MoveWish = (agent.followTarget.position - agent.motor.transform.position).normalized;
        }
    }

    private void Awake()
    {
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