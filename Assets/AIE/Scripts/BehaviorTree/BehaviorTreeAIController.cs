using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BehaviorTreeAIController : BaseHumanoidAIController
{
    protected IBehavior behaviorTree;

    protected override void Awake()
    {
        base.Awake();

        SelectorBehavior rootSelector = new SelectorBehavior();

        // patrolling
        SequenceBehavior patrolSequence = new();

        CheckWaypoint checkWp = new();
        checkWp.agent = this;
        RenewWaypoint renewWp = new();
        renewWp.agent = this;

        patrolSequence.SetBehaviors(checkWp, renewWp);

        // moving towards
        MoveTowardsWaypoint moveToWp = new();
        moveToWp.agent = this;

        // final assembly
        rootSelector.SetBehaviors(patrolSequence, moveToWp);

        // always setup the root
        behaviorTree = rootSelector;
    }

    protected override void Update()
    {
        base.Update();

        behaviorTree.DoBehavior();
    }

    private class CheckWaypoint : IBehavior
    {
        public BaseHumanoidAIController agent;

        public BehaviorResult DoBehavior()
        {
            // edge-case - we don't have one yet!
            if(agent.Destination == Vector3.zero) { return BehaviorResult.Success; }

            // have we reached one?
            if((agent.motor.transform.position - agent.Destination).sqrMagnitude < agent.WaypointThresholdSqr)
            {
                return BehaviorResult.Success;
            }

            return BehaviorResult.Failure;
        }
    }

    private class RenewWaypoint : IBehavior
    {
        public BaseHumanoidAIController agent;

        public BehaviorResult DoBehavior()
        {
            agent.currentPatrolIndex = (agent.currentPatrolIndex + 1) % agent.patrolPoints.Length;
            agent.SetDestination(agent.patrolPoints[agent.currentPatrolIndex].position);

            return BehaviorResult.Success;
        }
    }

    private class MoveTowardsWaypoint : IBehavior
    {
        public BaseHumanoidAIController agent;

        public BehaviorResult DoBehavior()
        {
            agent.motor.SprintWish = false;

            return BehaviorResult.Success;
        }
    }
}
