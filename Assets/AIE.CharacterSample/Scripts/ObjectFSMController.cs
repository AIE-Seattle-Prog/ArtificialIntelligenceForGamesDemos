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

    private void Awake()
    {
        fsmRunner = new FiniteStateMachineRunner();
        fsmRunner.CurrentState = new PatrolState(this);
    }

    private void Update()
    {
        fsmRunner.Run();
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

public class PatrolState : IFiniteState
{
    public ObjectFSMController agent;

    private List<IStateTransition> transitions = new List<IStateTransition>();

    public PatrolState(ObjectFSMController agent)
    {
        this.agent = agent;
    }

    public void OnStateEnter() { /* left intentionally blank */ }

    public void OnStateRun()
    {
        agent.motor.SprintWish = false;
        agent.motor.MoveWish = (agent.patrolPoints[agent.currentPatrolIndex].position - agent.motor.transform.position).normalized;

        // check if wp reached
        if ((agent.motor.transform.position - agent.patrolPoints[agent.currentPatrolIndex].position).sqrMagnitude < agent.waypointThreshold * agent.waypointThreshold)
        {
            agent.currentPatrolIndex = (agent.currentPatrolIndex + 1) % agent.patrolPoints.Length;
        }
    }

    public void OnStateExit() { /* left intentionally blank */ }

    public void AddCondition(IStateTransition transition)
    {
        transitions.Add(transition);
    }

    public void RemoveCondition(IStateTransition transition)
    {
        transitions.Remove(transition);
    }

    public IFiniteState ChangeState()
    {
        foreach(var potentialTransition in transitions)
        {
            if(potentialTransition.ShouldTransition())
            {
                return potentialTransition.NextState;
            }
        }

        return this;
    }
}

public class HasFollowTarget : IStateTransition
{
    public IFiniteState NextState { get; set; }

    public Transform target;

    public bool ShouldTransition() => target != null;
}

public class CompoundTransition : IStateTransition
{
    public IFiniteState NextState { get; set; }

    public List<IStateTransition> orTransitions = new List<IStateTransition>();

    public bool ShouldTransition()
    {
        foreach(var transition in orTransitions)
        {
            if(transition.ShouldTransition())
            {
                return true;
            }
        }

        return false;
    }
}
