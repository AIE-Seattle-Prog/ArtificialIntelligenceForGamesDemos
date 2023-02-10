using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class SteeringMethods
{
    /// <summary>
    /// Returns a force to apply to an object to steer it towards something.
    /// </summary>
    /// <param name="currentPos"></param>
    /// <param name="seekPos"></param>
    /// <param name="currentVel"></param>
    /// <param name="maxSpeed"></param>
    /// <returns></returns>
    public static Vector3 Seek(Vector3 currentPos, Vector3 seekPos, Vector3 currentVel, float maxSpeed)
    {
        Vector3 desiredVel = (seekPos - currentPos).normalized * maxSpeed;
        return desiredVel - currentVel;
    }

    public static Vector3 Flee(Vector3 currentPos, Vector3 fleePos, Vector3 currentVel, float maxSpeed)
    {
        Vector3 desiredVel = (currentPos - fleePos).normalized * maxSpeed;
        return desiredVel - currentVel;
    }

    public static Vector3 Wander(Vector3 currentPos, float radius, float jitter, Vector3 currentVel, float maxSpeed)
    {
        Vector3 spherePoint = Random.onUnitSphere * radius;
        Vector3 jitterOffset = new Vector3(Random.value, Random.value, Random.value).normalized * jitter;

        spherePoint = (spherePoint + jitterOffset).normalized * radius;

        return Seek(currentPos, currentPos + currentVel + spherePoint, currentVel, maxSpeed);
    }
}
