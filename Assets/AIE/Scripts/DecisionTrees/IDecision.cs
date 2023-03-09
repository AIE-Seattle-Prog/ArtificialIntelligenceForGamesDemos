using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDecision
{
    IDecision MakeDecision();
}

public class PrintDecision : IDecision
{
    public string text;

    public IDecision MakeDecision()
    {
        Debug.Log(text);

        return null;
    }
}

public class BooleanDecision : IDecision
{
    public bool boolean;

    public IDecision trueDecision;
    public IDecision falseDecision;

    public IDecision MakeDecision()
    {
        return (boolean ? trueDecision : falseDecision);
    }
}