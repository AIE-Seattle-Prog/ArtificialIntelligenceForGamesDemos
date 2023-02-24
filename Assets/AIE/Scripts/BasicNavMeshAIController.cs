using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;

public class BasicNavMeshAIController : MonoBehaviour
{
    public Camera cam;
    public NavMeshAgent navAgent;

    public LayerMask pickerMask = ~1;

    private NavMeshPath path;


    private void Start()
    {
        path = new NavMeshPath();
    }

    void Update()
    {
        // agent orders
        if (Input.GetMouseButtonDown(0))
        {
            var pickerRay = cam.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(pickerRay, out var hit, Mathf.Infinity, pickerMask, QueryTriggerInteraction.Ignore))
            {
                navAgent.CalculatePath(hit.point, path);
                navAgent.path = path;
            }
        }
    }

    private void OnDrawGizmos()
    {
        // early exit if null
        if(path == null) { return; }

        Gizmos.color = Color.green;

        for(int i = 0; i < path.corners.Length-1; ++i)
        {
            Gizmos.DrawLine(path.corners[i], path.corners[i + 1]);
        }
    }
}
