using UnityEngine;
using System.Collections.Generic;

public abstract class CompositeBehavior : IBehavior
{
    protected List<IBehavior> behaviors = new List<IBehavior>();

    public abstract BehaviorResult DoBehavior();

    public void AddBehavior(IBehavior behavior) => behaviors.Add(behavior);
    public void RemoveBehavior(IBehavior behavior) => behaviors.Remove(behavior);

    public void SetBehaviors(params IBehavior[] newBehaviors)
    {
        behaviors.Clear();
        foreach (var behavior in newBehaviors)
        {
            behaviors.Add(behavior);
        }
    }
}

/// <summary>
/// Run through all of its child behaviors until one fails or all are complete
/// </summary>
public class SequenceBehavior : CompositeBehavior
{
    public override BehaviorResult DoBehavior()
    {
        foreach(var behavior in behaviors)
        {
            if( BehaviorResult.Failure == behavior.DoBehavior()) { return BehaviorResult.Failure; }
        }

        return BehaviorResult.Success;
    }
}

/// <summary>
/// Run through all of its child behaviors until one succeeds or all fail
/// </summary>
public class SelectorBehavior : CompositeBehavior
{
    public override BehaviorResult DoBehavior()
    {
        foreach (var behavior in behaviors)
        {
            if (BehaviorResult.Success == behavior.DoBehavior()) { return BehaviorResult.Success; }
        }

        return BehaviorResult.Failure;
    }
}

public class PrintBehavior : IBehavior
{
    public string data = "";

    public PrintBehavior() { }
    public PrintBehavior(string userData)
    {
        data = userData;
    }

    public BehaviorResult DoBehavior()
    {
        Debug.Log(data);
        return BehaviorResult.Success;
    }
}