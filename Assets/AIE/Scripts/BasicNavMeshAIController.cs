using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class BasicNavMeshAIController : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent navAgent;
    
    void Update()
    {
        // agent orders
        if (Input.GetMouseButtonDown(0))
        {
            var pickerRay = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(pickerRay, out var hit, Mathf.Infinity))
            {
                navAgent.destination = hit.point;
            }
        }
    }
}
