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

        // chasing
        SequenceBehavior chaseSequence = new();

        CheckHasTarget checkTarget = new ();
        checkTarget.agent = this;
        MoveTowardsTarget moveToTarget = new();
        moveToTarget.agent = this;

        chaseSequence.SetBehaviors(checkTarget, moveToTarget);

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
        rootSelector.SetBehaviors(chaseSequence, patrolSequence, moveToWp);

        // always setup the root
        behaviorTree = rootSelector;
    }

    protected virtual void Start()
    {
        if (patrolPoints.Length > 0)
        {
            SetDestination(patrolPoints[0].position);
        }
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
            if(agent.Destination == Vector3.zero) { return BehaviorResult.Failure; }

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

    private class CheckHasTarget : IBehavior
    {
        public BaseHumanoidAIController agent;

        public BehaviorResult DoBehavior()
        {
            return agent.followTarget != null ? BehaviorResult.Success : BehaviorResult.Failure;
        }
    }

    private class MoveTowardsTarget : IBehavior
    {
        public BaseHumanoidAIController agent;

        public BehaviorResult DoBehavior()
        {
            // early exit if nothing to chase
            if (agent.followTarget == null) { return BehaviorResult.Failure; }

            agent.motor.SprintWish = true;

            bool meOnNav = NavMesh.SamplePosition(agent.motor.transform.position, out var myHit, 1.0f, NavMesh.AllAreas);
            bool followOnNav = NavMesh.SamplePosition(agent.followTarget.transform.position, out var followHit, 1.0f, NavMesh.AllAreas);

            NavMeshHit navCastHit = new();
            bool hasClearPath = meOnNav && followOnNav && !NavMesh.Raycast(myHit.position, followHit.position, out navCastHit, NavMesh.AllAreas);
            if (hasClearPath)
            {
                agent.UseNavigation = false;

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
                    agent.UseNavigation = true;

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
                // failsafe - just try to move
                else
                {
                    agent.UseNavigation = false;

                    Vector3 chaseForce = SteeringMethods.Seek(agent.motor.transform.position, agent.followTarget.transform.position, agent.motor.MoveWish, 1.0f);
                    chaseForce.y = 0.0f;
                    chaseForce = chaseForce.normalized * (agent.chaseStrength * Time.deltaTime);
                    agent.motor.MoveWish += chaseForce;
                    agent.motor.MoveWish.Normalize();
                }
            }

            return BehaviorResult.Success;
        }
    }
}
