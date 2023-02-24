using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseState : IFiniteState
{
    private List<IStateTransition> transitions = new List<IStateTransition>();

    public IFiniteState ChangeState()
    {
        foreach (var potentialTransition in transitions)
        {
            if (potentialTransition.ShouldTransition())
            {
                return potentialTransition.NextState;
            }
        }

        return this;
    }

    public virtual void OnStateEnter() { }

    public virtual void OnStateExit() { }

    public virtual void OnStateRun() { }

    public void AddCondition(IStateTransition transition)
    {
        transitions.Add(transition);
    }

    public void RemoveCondition(IStateTransition transition)
    {
        transitions.Remove(transition);
    }
}

public class FloatTransition : IStateTransition
{
    public IFiniteState NextState { get; set; }

    public float threshold = 3.0f;

    public Func<float> floatFunction;
    public enum Operator
    {
        LessThan,
        LessThanOrEqualTo,
        GreaterThan,
        GreaterThanOrEqualTo,
        EqualTo
    }
    public Operator comparisonOperation;

    public FloatTransition() { }
    public FloatTransition(IFiniteState next, float exp, Func<float> func, Operator comp)
    {
        NextState = next;
        threshold = exp;
        floatFunction = func;
        comparisonOperation = comp;
    }

    public bool ShouldTransition()
    {
        float dist2 = floatFunction();
        float thres2 = threshold * threshold;

        switch (comparisonOperation)
        {
            case Operator.LessThan:
                return dist2 < thres2;
            case Operator.LessThanOrEqualTo:
                return dist2 <= thres2;
            case Operator.GreaterThan:
                return dist2 > thres2;
            case Operator.GreaterThanOrEqualTo:
                return dist2 >= thres2;
            case Operator.EqualTo:
                return Mathf.Approximately(dist2, thres2);
        }

        return false;
    }
}

public class BooleanTransition : IStateTransition
{
    public IFiniteState NextState { get; set; }

    public bool expected;

    public Func<bool> booleanFunction;

    public BooleanTransition() { }
    public BooleanTransition(IFiniteState next, bool exp, Func<bool> func)
    {
        NextState = next;
        expected = exp;
        booleanFunction = func;
    }

    public bool ShouldTransition()
    {
        return expected == booleanFunction();
    }
}

public class CompoundTransition : IStateTransition
{
    public IFiniteState NextState { get; set; }

    public List<IStateTransition> orTransitions = new List<IStateTransition>();

    public bool ShouldTransition()
    {
        foreach (var transition in orTransitions)
        {
            if (transition.ShouldTransition())
            {
                return true;
            }
        }

        return false;
    }
}
