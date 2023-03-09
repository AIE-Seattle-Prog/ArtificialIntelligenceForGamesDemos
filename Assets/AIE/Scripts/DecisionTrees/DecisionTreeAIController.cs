using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DecisionTreeAIController : BaseHumanoidController
{
    IDecision decisionTreeRoot;

    [Header("Decision Tree Settings")]
    public bool decisionValue;

    private void Start()
    {
        PrintDecision printYeah = new PrintDecision();
        printYeah.text = "Yeah!";

        PrintDecision printNah = new PrintDecision();
        printNah.text = "Nah.";

        BooleanDecision vibeCheck = new BooleanDecision();
        vibeCheck.boolean = decisionValue;
        vibeCheck.trueDecision = printYeah;
        vibeCheck.falseDecision = printNah;

        decisionTreeRoot = vibeCheck;
    }

    private void Update()
    {
        IDecision currentDecision = decisionTreeRoot;

        while(currentDecision != null)
        {
            currentDecision = currentDecision.MakeDecision();
        }
    }
}
