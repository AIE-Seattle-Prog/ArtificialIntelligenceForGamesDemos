using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class SwitchFSMController : MonoBehaviour
{
    public CharacterMotor motor;
    public NavMeshAgent navAgent;

    [SerializeField]
    private Transform[] patrolPoints;
    private int currentPatrolIndex;
    public float waypointThreshold = 0.5f;
    private NavMeshPath currentPath;
    [Space]
    public float attackThreshold = 3.0f;
    private Transform followTarget;

    public enum States
    {
        Patrol,
        Chase,
        Attack
    }
    private States currentState;
    private States nextState;

    private void OnPatrolEnter() { Debug.Log("I guess it was just the wind..."); }
    private void Patrol()
    {
        motor.SprintWish = false;
        motor.MoveWish = (patrolPoints[currentPatrolIndex].position - motor.transform.position).normalized;

        // check if wp reached
        if ((motor.transform.position - patrolPoints[currentPatrolIndex].position).sqrMagnitude < waypointThreshold * waypointThreshold)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
        }

        // TRANSITION: player is in range, begin chase
        //             (TODO: ensure that we have line of sight)
        if (followTarget != null) { nextState = States.Chase; }
    }
    private void OnPatrolExit() { }

    private void OnChaseEnter() { Debug.Log("STOP! You've violated the law!"); }
    private void Chase()
    {
        if(followTarget == null) { currentPatrolIndex = 0; nextState = States.Patrol; return; }

        motor.SprintWish = true;
        motor.MoveWish = (followTarget.position - motor.transform.position).normalized;

        // TRANSITION: if player is close enough, ATTACK! (TODO: test if we have line of sight)
        if ((motor.transform.position - followTarget.position).sqrMagnitude < attackThreshold * attackThreshold)
        {
            nextState = States.Attack;
        }
    }
    private void OnChaseExit() { }

    private void OnAttackEnter() { Debug.Log("ROAR!"); }
    private void Attack()
    {
        if (followTarget == null) { currentPatrolIndex = 0; nextState = States.Patrol; return; }

        Debug.Log("Pow!");

        motor.MoveWish = Vector3.zero;
        motor.transform.forward = (followTarget.position - motor.transform.position).normalized;

        // TRANSITION: if player is too far, chase instead.
        if ((motor.transform.position - followTarget.position).sqrMagnitude > attackThreshold * attackThreshold)
        {
            nextState = States.Chase;
        }
    }
    private void OnAttackExit() { }

    private void ChangeState(States newState)
    {
        // exit from the current
        switch (currentState)
        {
            case States.Patrol:
                OnPatrolExit();
                break;
            case States.Chase:
                OnChaseExit();
                break;
            case States.Attack:
                OnAttackExit();
                break;
        }

        // update my variables
        currentState = newState;

        // enter the next
        switch (newState)
        {
            case States.Patrol:
                OnPatrolEnter();
                break;
            case States.Chase:
                OnChaseEnter();
                break;
            case States.Attack:
                OnAttackEnter();
                break;
        }
    }

    private void Awake()
    {
        currentPath = new NavMeshPath();
    }

    private void Update()
    {
        switch (currentState)
        {
            case States.Patrol:
                Patrol();
                break;
            case States.Chase:
                Chase();
                break;
            case States.Attack:
                Attack();
                break;
        }

        // does a transition need to occur?
        if(currentState != nextState)
        {
            ChangeState(nextState);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
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
