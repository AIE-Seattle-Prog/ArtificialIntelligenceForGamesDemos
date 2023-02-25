using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseHumanoidController : MonoBehaviour
{
    [Header("Base Humanoid Controller")]
    public Transform headTransform;
    public CharacterMotor motor;

    [Space]
    public int factionId = -1;
}
