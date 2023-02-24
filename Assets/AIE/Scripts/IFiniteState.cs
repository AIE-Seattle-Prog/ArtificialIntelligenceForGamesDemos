using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IFiniteState
{
    void OnStateEnter();

    void OnStateRun();

    void OnStateExit();

    void AddCondition(IStateTransition transition);

    void RemoveCondition(IStateTransition transition);

    IFiniteState ChangeState();
}

public interface IStateTransition
{
    bool ShouldTransition();

    IFiniteState NextState { get; }
}
