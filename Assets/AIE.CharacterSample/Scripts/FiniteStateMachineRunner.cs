using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FiniteStateMachineRunner
{
    public IFiniteState PreviousState { get; private set; }
    private IFiniteState currentState;
    public IFiniteState CurrentState
    {
        get => currentState;
        set
        {
            ChangeState(value);
        }
    }

    public void ChangeState(IFiniteState newState)
    {
        if (CurrentState != null)
        {
            CurrentState.OnStateExit();
        }

        PreviousState = currentState;
        currentState = newState;
        Debug.Assert(CurrentState != null, "New state is null! Agent will be stuck.");

        if (newState != null)
        {
            newState.OnStateEnter();
        }
    }

    public void Run()
    {
        if (CurrentState != null)
        {
            // execute the current state
            CurrentState.OnStateRun();

            // check for transitions
            IFiniteState nextState = CurrentState.ChangeState();

            if (CurrentState != nextState)
            {
                ChangeState(nextState);

            }
        }
    }
}
