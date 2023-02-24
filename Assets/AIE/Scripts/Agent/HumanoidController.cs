using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HumanoidController : MonoBehaviour
{
    public HumanoidMotor motor;

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        bool shouldCrouch = Input.GetButton("Crouch");

        motor.MoveWish.x = h;
        motor.MoveWish.z = v;

        motor.CrouchWish = shouldCrouch;
    }
}
